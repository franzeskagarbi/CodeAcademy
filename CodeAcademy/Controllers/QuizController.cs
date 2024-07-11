using CodeAcademy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace CodeAcademy.Controllers
{
    public class QuizController : Controller
    {
        private readonly AcademyContext _context;

        public QuizController(AcademyContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CreateQuizAsync(int sectionId)
        {
            var quiz = await _context.Quizzes
                                    .FirstOrDefaultAsync(q => q.SectionId == sectionId);

            if (quiz != null)
            {
                var createQuizModel = new CreateQuizModel
                {
                    QuizName = quiz.QuizName,
                    // Add other properties you want to populate in CreateQuizModel
                };

                return RedirectToAction("ViewQuestions", new { quizId = quiz.QuizId });
            }
            else
            {
                var viewModel = new CreateQuizModel { SectionId = sectionId };
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(int sectionId, string quizName, int totalPoints)
        {
            try
            {
                // Check if the section name is "Final"
                var sectionName = await _context.CourseSections
                    .Where(s => s.SectionId == sectionId)
                    .Select(s => s.SectionName)
                    .FirstOrDefaultAsync();

                if (sectionName != null && sectionName.Equals("Final", StringComparison.OrdinalIgnoreCase))
                {
                    // Generate a unique quizId
                    var quizId = GenerateUniqueQuizId();

                    // Create the final quiz
                    var finalQuiz = new Quiz
                    {
                        QuizId = quizId,
                        SectionId = sectionId,
                        QuizName = "Final Quiz",
                        TotalPoints = 0,
                        IsFinal = true
                    };

                    // Add final quiz to context
                    _context.Quizzes.Add(finalQuiz);
                    await _context.SaveChangesAsync();

                    // Generate or update questions for the final quiz
                    var courseId = GetCourseIdForSection(sectionId);
                    await GenerateOrUpdateFinalQuiz(courseId);

                    TempData["SuccessMessage"] = "Final Quiz created successfully.";
                    return RedirectToAction("ViewQuestions", new { quizId = quizId });
                }
                else
                {
                    // Create a regular quiz
                    var existingQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.SectionId == sectionId);
                    if (existingQuiz == null)
                    {
                        var quizId = GenerateUniqueQuizId();
                        var quiz = new Quiz
                        {
                            QuizId = quizId,
                            SectionId = sectionId,
                            QuizName = quizName,
                            TotalPoints = 0,
                            IsFinal = false
                        };

                        _context.Quizzes.Add(quiz);
                        await _context.SaveChangesAsync();

                        await UpdateQuizTotalPoints(quizId);

                        // Update the final quiz if the section level is "basic"
                        var sectionLevel = await _context.CourseSections
                            .Where(s => s.SectionId == sectionId)
                            .Select(s => s.SectionLevel)
                            .FirstOrDefaultAsync();

                        if (sectionLevel == "basic")
                        {
                            var courseId = GetCourseIdForSection(sectionId);
                            await GenerateOrUpdateFinalQuiz(courseId);
                        }

                        TempData["SuccessMessage"] = "Quiz created successfully.";
                        return RedirectToAction("ViewQuestions", new { quizId = quizId });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating quiz: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while creating the quiz. Please try again later.";
            }

            return RedirectToAction("CreateQuiz", new { sectionId = sectionId });
        }




        //helper method
        private async Task UpdateQuizTotalPoints(int quizId)
        {
            var totalPoints = await _context.Questions
                                           .Where(q => q.QuizId == quizId)
                                           .SumAsync(q => q.Points);

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz != null)
            {
                quiz.TotalPoints = totalPoints;
                await _context.SaveChangesAsync();
            }
        }

        private async Task GenerateOrUpdateFinalQuiz(int courseId)
        {
            // Get the sectionId for the section named "Final"
            var finalSectionId = await _context.CourseSections
                .Where(s => s.CourseId == courseId && s.SectionName == "Final")
                .Select(s => s.SectionId)
                .FirstOrDefaultAsync();

            if (finalSectionId == 0)
            {
                Console.WriteLine("Section 'Final' not found for courseId: " + courseId);
                return;
            }

            // Check if a final quiz already exists for the course and section
            var existingFinalQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.SectionId == finalSectionId && q.IsFinal);

            if (existingFinalQuiz == null)
            {
                // Generate the final quiz
                var finalQuiz = new Quiz
                {
                    QuizId = GenerateUniqueQuizId(),
                    SectionId = finalSectionId,
                    QuizName = "Final Quiz",
                    IsFinal = true
                };

                await AddQuestionsFromRegularQuizzesToFinalQuiz(courseId, finalQuiz.QuizId);

                var totalPoints = await CalculateTotalPointsForFinalQuiz(courseId);
                finalQuiz.TotalPoints = totalPoints;

                _context.Quizzes.Add(finalQuiz);
                await _context.SaveChangesAsync();
            }
            else
            {
                await UpdateQuestionsInFinalQuiz(existingFinalQuiz.QuizId, courseId);

                var totalPoints = await CalculateTotalPointsForFinalQuiz(courseId);
                existingFinalQuiz.TotalPoints = totalPoints;

                await _context.SaveChangesAsync();
            }
        }


        private async Task AddQuestionsFromRegularQuizzesToFinalQuiz(int courseId, int finalQuizId)
        {
            // Query all regular quizzes for the course
            var regularQuizzes = await _context.Quizzes
                .Where(q => q.Section.CourseId == courseId && !q.IsFinal && q.Section.SectionLevel == "basic")
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers) // Include answers for each question
                .ToListAsync();

            foreach (var regularQuiz in regularQuizzes)
            {
                // Add each question from the regular quiz to the final quiz
                foreach (var question in regularQuiz.Questions)
                {
                    var newQuestion = new Question
                    {
                        // Assign a new questionId for the final quiz
                        QuestionId = GenerateUniqueQuizId(), // Adjust as needed
                        QuizId = finalQuizId,
                        QuestionText = question.QuestionText,
                        Points = question.Points
                    };

                    _context.Questions.Add(newQuestion);

                    // Add answers for the current question to the final quiz question
                    foreach (var answer in question.Answers)
                    {
                        var newAnswer = new Answer
                        {
                            // Assign a new answerId for the final quiz
                            AnswerId = GenerateUniqueAnswerId(), // Generate unique answerId
                            QuestionId = newQuestion.QuestionId, // Assign the new questionId
                            Answer1 = answer.Answer1,
                            IsCorrect = answer.IsCorrect
                        };

                        _context.Answers.Add(newAnswer);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        // Helper method to generate unique answerId
        private int GenerateUniqueAnswerId()
        {
            // Implement your logic to generate a unique answerId here
            // Example: You can use GUIDs or a sequence generator
            // For simplicity, let's assume it generates unique integer IDs
            return Guid.NewGuid().GetHashCode(); // Example; adjust as needed
        }


        private async Task UpdateQuestionsInFinalQuiz(int finalQuizId, int courseId)
        {
            // Get the existing questions in the final quiz
            var existingQuestions = await _context.Questions
                    .Where(q => q.QuizId == finalQuizId)
                    .Include(q => q.Answers)
                    .ToListAsync();

            foreach (var question in existingQuestions)
            {
                _context.Answers.RemoveRange(question.Answers);
            }
            _context.Questions.RemoveRange(existingQuestions);

            await _context.SaveChangesAsync();

            // Add questions from all regular quizzes for the course to the final quiz
            await AddQuestionsFromRegularQuizzesToFinalQuiz(courseId, finalQuizId);
        }


        private async Task<int> CalculateTotalPointsForFinalQuiz(int courseId)
        {
            // Query all regular quizzes for the course
            var regularQuizzes = await _context.Quizzes
                 .Where(q => q.Section.CourseId == courseId && !q.IsFinal && q.Section.SectionLevel == "basic")
                .ToListAsync();

            // Calculate total points by summing up points from all regular quizzes
            var totalPoints = regularQuizzes.Sum(q => q.TotalPoints);

            return totalPoints;
        }

        [HttpGet]
        public async Task<IActionResult> ViewQuestions(int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);

            if (quiz == null)
            {
                return NotFound();
            }

            ViewData["QuizName"] = quiz.QuizName;
            ViewData["QuizId"] = quizId;

            var questions = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .ToListAsync();

            var questionViewModels = new List<QuestionViewModel>();

            foreach (var question in questions)
            {
                var answers = await _context.Answers
                    .Where(a => a.QuestionId == question.QuestionId)
                    .Select(a => new AnswerViewModel
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.Answer1,
                    })
                    .ToListAsync();

                var questionViewModel = new QuestionViewModel
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    Answers = answers
                };

                questionViewModels.Add(questionViewModel);
            }

            return View(questionViewModels);
        }
        // GET: Quiz/CreateQuestion
        [HttpGet]
        public IActionResult CreateQuestion(int quizId)
        {

            ViewData["QuizId"] = quizId; 

            return View();
        }

        // POST: Quiz/CreateQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(CreateQuestionModel model)
        {
            if (ModelState.IsValid)
            {
                var question = new Question
                {
                    QuestionId = GenerateUniqueQuizId(),
                    QuizId = model.QuizId,
                    QuestionText = model.QuestionText,
                    Points = model.Points,
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
                // Get the courseId from the quiz
                var courseId = await GetCourseIdForQuiz(model.QuizId);
                await GenerateOrUpdateFinalQuiz(courseId);

                // Update TotalPoints for the quiz after adding a question
                await UpdateQuizTotalPoints(model.QuizId);

                // Update TotalPoints for the final quiz
                var finalQuizId = await GetFinalQuizId(courseId);
                await UpdateQuizTotalPoints(finalQuizId);

                return RedirectToAction("ViewQuestions", new { quizId = model.QuizId });
            }
            return View(model);
        }

        private async Task<int> GetCourseIdForQuiz(int quizId)
        {
            return await _context.Quizzes
                .Where(q => q.QuizId == quizId)
                .Select(q => q.Section.CourseId)
                .FirstOrDefaultAsync();
        }

        private async Task<int> GetFinalQuizId(int courseId)
        {
            return await _context.Quizzes
                .Where(q => q.Section.CourseId == courseId && q.IsFinal)
                .Select(q => q.QuizId)
                .FirstOrDefaultAsync();
        }

        // GET: Answer/Create
        [HttpGet]
        public IActionResult CreateAnswer(int questionId)
        {
            ViewData["QuestionId"] = questionId;

            return View();
        }

        // POST: Answer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnswer(CreateAnswerModel model)
        {
            if (ModelState.IsValid)
            {
                var answer = new Answer
                {
                    AnswerId = GenerateUniqueQuizId(),
                    QuestionId = model.QuestionId,
                    Answer1 = model.AnswerText,
                    IsCorrect = model.IsCorrect
                };

                _context.Answers.Add(answer);
                await _context.SaveChangesAsync();
                var question = await _context.Questions.FindAsync(model.QuestionId);
                if (question == null)
                {
                    return NotFound();
                }
                var quizId = question.QuizId;
                return RedirectToAction("ViewQuestions", new { quizId });
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> EditQuestion(int questionId)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                return NotFound();
            }

            var model = new CreateQuestionModel
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(CreateQuestionModel model)
        {
            if (ModelState.IsValid)
            {
                var question = await _context.Questions.FindAsync(model.QuestionId);
                if (question == null)
                {
                    return NotFound();
                }

                question.QuestionText = model.QuestionText;

                _context.Questions.Update(question);
                await _context.SaveChangesAsync();

                var quizId = question.QuizId;

                return RedirectToAction("ViewQuestions", new { quizId });
            }

            return View(model);
        }

        // POST: Answer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnswer(int answerId)
        {
            var answer = await _context.Answers.FindAsync(answerId);
            if (answer == null)
            {
                return NotFound();
            }

            try
            {
                _context.Answers.Remove(answer);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }

            var question = await _context.Questions.FindAsync(answer.QuestionId);
            if (question == null)
            {
                return NotFound();
            }

            var quizId = question.QuizId;

            // Calculate total points for the quiz after deleting the answer
            await UpdateQuizTotalPoints(quizId);
           

            // Get the courseId from the quiz
            var courseId = await GetCourseIdForQuiz(quizId);
            // Update TotalPoints for the final quiz
            var finalQuizId = await GetFinalQuizId(courseId);
            await UpdateQuizTotalPoints(finalQuizId);

            // Update the final quiz
            await GenerateOrUpdateFinalQuiz(courseId);

            return RedirectToAction("ViewQuestions", new { quizId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                return NotFound();
            }

            var answers = _context.Answers.Where(a => a.QuestionId == questionId);
            _context.Answers.RemoveRange(answers);

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
                        

            var quizId = question.QuizId;
            // Calculate total points for the quiz after deleting the question
            await UpdateQuizTotalPoints(quizId);

            // Get the courseId from the quiz
            var courseId = await GetCourseIdForQuiz(quizId);
            // Update TotalPoints for the final quiz
            var finalQuizId = await GetFinalQuizId(courseId);
            await CalculateTotalPointsForFinalQuiz(courseId);

            // Update the final quiz
            await GenerateOrUpdateFinalQuiz(courseId);
            return RedirectToAction("ViewQuestions", new { quizId});
        }

        private int GenerateUniqueQuizId()
        {
            int newId;
            do
            {
                newId = new Random().Next(1, int.MaxValue);
            } while (_context.Quizzes.Any(q => q.QuizId == newId));

            return newId;
        }

        private int GetCourseIdForSection(int sectionId)
        {
            var course = _context.CourseSections
                                .Where(s => s.SectionId == sectionId)
                                .Select(s => s.CourseId)
                                .FirstOrDefault();
            return course;
        }

        private async Task UpdateFinalQuizAfterChange(int courseId, int pointsAdjustment)
        {
            // Get the sectionId for the section named "Final"
            var finalSectionId = await _context.CourseSections
                .Where(s => s.CourseId == courseId && s.SectionName == "Final")
                .Select(s => s.SectionId)
                .FirstOrDefaultAsync();

            if (finalSectionId == 0)
            {
                Console.WriteLine("Section 'Final' not found for courseId: " + courseId);
                return;
            }

            // Get the final quiz for the course
            var finalQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.SectionId == finalSectionId && q.IsFinal);
            if (finalQuiz == null)
            {
                Console.WriteLine("Final quiz not found for section: " + finalSectionId);
                return;
            }

            // Adjust the total points for the final quiz
            finalQuiz.TotalPoints += pointsAdjustment;

            await _context.SaveChangesAsync();
        }


        // GET: Quiz/DoQuiz/{sectionId}
        public IActionResult DoQuiz(int sectionId, int totalScore)
        {
            var quiz = _context.Quizzes
                               .Include(q => q.Questions)
                               .ThenInclude(q => q.Answers)
                               .FirstOrDefault(q => q.SectionId == sectionId);

            ViewBag.TotalScore = totalScore;
            if (quiz == null)
            {
                TempData["ErrorMessage"] = "For now, no quiz is set for this section.";
                //return RedirectToAction("CourseMainPage", "Courses", new { id = GetCourseIdForSection(sectionId), error = true });
                var sectionLevel = GetSectionLevel(sectionId);
                switch (sectionLevel.ToLower())
                {
                    case "basic":
                        return RedirectToAction("CourseMainPage", "Courses", new { id = GetCourseIdForSection(sectionId), error = true });
                    case "-basic":
                        return RedirectToAction("BasicMinusLearningPath", "Learning", new { id = GetCourseIdForSection(sectionId), totalScore = totalScore });
                    case "medium":
                        return RedirectToAction("MediumLearningPath", "Learning", new { id = GetCourseIdForSection(sectionId), totalScore = totalScore });
                    case "medium+":
                        return RedirectToAction("MediumPlusLearningPath", "Learning", new { id = GetCourseIdForSection(sectionId), totalScore = totalScore });
                    case "advanced":
                        return RedirectToAction("AdvancedLearningPath", "Learning", new { id = GetCourseIdForSection(sectionId), totalScore = totalScore });
                    default:
                        return RedirectToAction("CourseMainPage", "Courses", new { id = GetCourseIdForSection(sectionId), error = true });
                }
            }

            var viewModel = new QuizSubmissionViewModel
            {
                QuizId = quiz.QuizId,
                Questions = quiz.Questions.ToList(),
                StudentAnswers = new List<AnswerSubmission>()
            };

            foreach (var question in quiz.Questions)
            {
                viewModel.StudentAnswers.Add(new AnswerSubmission { QuestionId = question.QuestionId });
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DoQuiz(int QuizId, List<AnswerSubmission> answers)
        {
            if (answers == null || !answers.Any())
            {
                // Handle case where no answers were submitted
                return RedirectToAction("Error");
            }
            // Fetch section level for the quiz
            var quizSection = await _context.Quizzes
                .Include(q => q.Section)
                .FirstOrDefaultAsync(q => q.QuizId == QuizId);

            if (quizSection == null)
            {
                return NotFound();
            }


            // Ensure QuizId is valid
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuizId == QuizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // check if the quiz is a final quiz
            bool isFinalQuiz = quiz.IsFinal;

            // Calculate total score
            int totalScore = 0;
            string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserId, out int userIdInt))
            {
                return RedirectToAction("Error");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userIdInt);
            if (student == null)
            {
                return RedirectToAction("Error");
            }

            // Delete existing student answers for the current quiz and student
            var existingStudentAnswers = await _context.StudentAnswers
                .Where(sa => sa.QuizId == QuizId && sa.StudentId == student.UserId)
                .ToListAsync();

            if (existingStudentAnswers.Any())
            {
                _context.StudentAnswers.RemoveRange(existingStudentAnswers);
                await _context.SaveChangesAsync();
            } 

            List<StudentAnswer> studentAnswers = new List<StudentAnswer>();


            foreach (var answer in answers)
            {
                // Find the question in the quiz
                var question = quiz.Questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);

                if (question != null)
                {

                    // Find the correct answer for the question
                    var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect == 1);
                    bool isCorrect = correctAnswer != null && correctAnswer.AnswerId == answer.SelectedAnswerId;

                    if (isCorrect)
                    {
                        totalScore += question.Points;
                    }
                    
                    // Add to studentAnswers list only if it's the final quiz
                    if (isFinalQuiz)
                    {
                        studentAnswers.Add(new StudentAnswer
                        {
                            AnswerId = GenerateStudentAnswerId(),
                            StudentId = student.UserId,
                            QuizId = QuizId,
                            QuestionId = answer.QuestionId,
                            ChosenAnswerId = answer.SelectedAnswerId,
                            IsCorrect = isCorrect
                        });
                    }
                }
            }

            // Save or update grade
            //string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            /*if (!int.TryParse(currentUserId, out int userIdInt))
            {
                return RedirectToAction("Error");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userIdInt);
            if (student == null)
            {
                return RedirectToAction("Error");
            }*/

            var existingGrade = await _context.Grades.FirstOrDefaultAsync(g => g.StudentId == student.UserId && g.QuizId == QuizId);

            if (existingGrade != null)
            {
                existingGrade.Score = totalScore;
            }
            else
            {
                Grade newGrade = new Grade
                {
                    GradeId = GenerateUniqueQuizId(), // Replace with your method to generate a unique grade ID
                    StudentId = student.UserId,
                    QuizId = QuizId,
                    Score = totalScore,
                };

                _context.Grades.Add(newGrade);
            }

            ViewBag.TotalScore = totalScore;
            ViewBag.StudentId = student.UserId;

            
            await _context.SaveChangesAsync();
            // Fetch course and section IDs based on QuizId
            var quizInfo = await _context.Quizzes
                .Include(q => q.Section) // Assuming Quiz has a navigation property to Section
                .ThenInclude(s => s.Course) // Assuming Section has a navigation property to Course
                .Where(q => q.QuizId == QuizId)
                .Select(q => new { CourseId = q.Section.Course.CourseId, SectionId = q.Section.SectionId })
                .FirstOrDefaultAsync();

            if (quizInfo == null)
            {
                TempData["ErrorMessage"] = "No quiz is set for this course."; // Handle case where quizId doesn't exist or isn't associated correctly
            }

            // Add student answers to the database
            if (isFinalQuiz)
            {
                _context.StudentAnswers.AddRange(studentAnswers);
                await _context.SaveChangesAsync();

                // Redirect to view showing incorrect answers
                return RedirectToAction("DisplayIncorrectAnswers", new { quizId = QuizId });
            }


            int courseId = quizInfo.CourseId;
            int sectionId = quizInfo.SectionId;
            var sectionLevel = GetSectionLevel(quizSection.SectionId);

            //return RedirectToAction("CourseMainPage", "Courses", new { id = courseId });
            switch (sectionLevel.ToLower())
            {
                case "medium":
                    return RedirectToAction("MediumLearningPath", "Learning", new { id = GetCourseIdForSection(quizSection.SectionId), totalScore = totalScore });
                case "basic":
                    return RedirectToAction("CourseMainPage", "Courses", new { id = GetCourseIdForSection(quizSection.SectionId), error = true });
                case "-basic":
                    return RedirectToAction("BasicMinusLearningPath", "Learning", new { id = GetCourseIdForSection(quizSection.SectionId), totalScore = totalScore });
                case "medium+":
                    return RedirectToAction("MediumPlusLearningPath", "Learning", new { id = GetCourseIdForSection(quizSection.SectionId), totalScore = totalScore });
                case "advanced":
                    return RedirectToAction("AdvancedLearningPath", "Learning", new { id = GetCourseIdForSection(quizSection.SectionId), totalScore = totalScore });
                default:
                    return RedirectToAction("CourseMainPage", "Courses", new { id = GetCourseIdForSection(quizSection.SectionId), error = true });
            }
        }

        private string GetSectionLevel(int sectionId)
        {
            var section = _context.CourseSections.FirstOrDefault(s => s.SectionId == sectionId);
            return section?.SectionLevel ?? "unknown"; // Default to "unknown" or handle appropriately
        }

        private int GenerateStudentAnswerId()
        {
            int newId;
            do
            {
                newId = new Random().Next(1, int.MaxValue);
            } while (_context.StudentAnswers.Any(s => s.AnswerId == newId));

            return newId;
        }

        public async Task<IActionResult> DisplayIncorrectAnswers(int quizId)
        {
            var studentId = GetStudentId(); // Replace with your method to get the current student's ID

            // Fetch the grade for the current student and quiz
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.StudentId == studentId && g.QuizId == quizId);

            var totalScore = grade?.Score ?? 0;

            var incorrectAnswers = await _context.StudentAnswers
                .Where(sa => sa.QuizId == quizId && sa.StudentId == studentId && sa.IsCorrect == false)
                .Include(sa => sa.Question)
                .ThenInclude(q => q.Answers)
                .Select(sa => new
                {
                    sa.QuestionId,
                    sa.Question.QuestionText,
                    sa.ChosenAnswer.Answer1,
                    sa.Question.Answers
                })
                .ToListAsync();

            var incorrectAnswerViewModels = incorrectAnswers.Select(sa => new IncorrectAnswerViewModel
            {
                QuestionId = sa.QuestionId ?? 0,
                QuestionText = sa.QuestionText,
                SelectedAnswerText = sa.Answer1,
                CorrectAnswerText = sa.Answers.FirstOrDefault(a => a.IsCorrect == 1)?.Answer1,
                AnswerOptions = sa.Answers.Select(a => new AnswerViewModel
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.Answer1,
                    IsCorrect = a.IsCorrect == 1
                }).ToList()
            }).ToList();

            var quizInfo = await _context.Quizzes
                .Include(q => q.Section) // Assuming Quiz has a navigation property to Section
                .ThenInclude(s => s.Course) // Assuming Section has a navigation property to Course
                .Where(q => q.QuizId == quizId)
                .Select(q => new {
                    q.QuizId,
                    q.TotalPoints,
                    CourseId = q.Section.Course.CourseId,
                    SectionId = q.Section.SectionId
                })
                .FirstOrDefaultAsync();

            int totalPoints = quizInfo.TotalPoints;

            // Calculate percentage of total score
            double percentage = (totalScore * 100.0) / totalPoints;

            if (quizInfo == null)
            {
                TempData["ErrorMessage"] = "No quiz is set for this course."; // Handle case where quizId doesn't exist or isn't associated correctly
                return RedirectToAction("Index", "Home"); // Or handle it as appropriate
            }

            var viewModel = new DisplayIncorrectAnswersViewModel
            {
                IncorrectAnswers = incorrectAnswerViewModels,
                TotalScore = totalScore,
                Percentage = percentage,
                CourseId = quizInfo.CourseId
            };
            Console.WriteLine(percentage + "%");
            return View(viewModel);
            // Redirect based on the percentage
            /*if (percentage >= 90) // advanced
            {
                return RedirectToAction("AdvancedLearningPath", "Learning", new { id = quizInfo.CourseId, totalScore = totalScore });
            }
            else if (percentage >= 50 && percentage < 90) // medium+
            {
                return RedirectToAction("MediumPlusLearningPath", "Learning", new { id = quizInfo.CourseId, totalScore = totalScore });
            }
            else if (percentage >= 10 && percentage < 50) // medium
            {
                return RedirectToAction("MediumLearningPath", "Learning", new { id = quizInfo.CourseId, totalScore = totalScore });
            }
            else if (percentage < 10) // medium
            {
                return RedirectToAction("BasicMinusLearningPath", "Learning", new { id = quizInfo.CourseId, totalScore = totalScore });
            }
            else // basic
            {
                return RedirectToAction("BasicMinusLearningPath", "Learning", new { id = quizInfo.CourseId, totalScore = totalScore });
            } */
        }


        private int GetStudentId()
        {
            // Get the current user's identity
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                // You can further query the database or use the ID directly based on your context
                // Example: Fetch the student ID from your database based on the userId
                var student = _context.Students.FirstOrDefault(s => s.UserId == userId);

                if (student != null)
                {
                    return student.UserId; // Assuming StudentId is the primary key for Student
                }
            }

            // Handle cases where user ID claim is not found or student is not found
            // You might want to throw an exception or handle this scenario based on your application's requirements
            throw new ApplicationException("Student ID not found for the current user.");
        }


    }




}
