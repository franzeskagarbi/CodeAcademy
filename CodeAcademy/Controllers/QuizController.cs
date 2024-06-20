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

        // POST: Quiz/CreateQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(int sectionId, string quizName, int totalPoints)
        {
            try
            {
                // Check if the quiz already exists for the section
                var existingQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.SectionId == sectionId );
                if (existingQuiz == null)
                {
                    // Generate a unique quizId
                    var quizId = GenerateUniqueQuizId();

                // Create the new quiz
                var quiz = new Quiz
                {
                    QuizId = quizId,
                    SectionId = sectionId,
                    QuizName = quizName,
                    TotalPoints = totalPoints
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quiz created successfully.";
                return RedirectToAction("ViewQuestions", new { quizId = quizId });

                }       

            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error creating quiz: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while creating the quiz. Please try again later.";
                return RedirectToAction("CreateQuiz", new { sectionId = sectionId });
            }
            return RedirectToAction("CreateQuiz", new { sectionId = sectionId });
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

                return RedirectToAction("ViewQuestions", new { quizId = model.QuizId });
            }
            return View(model);
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
        // GET: Quiz/DoQuiz/{sectionId}
        public IActionResult DoQuiz(int sectionId)
        {
            var quiz = _context.Quizzes
                               .Include(q => q.Questions)
                               .ThenInclude(q => q.Answers)
                               .FirstOrDefault(q => q.SectionId == sectionId);

            if (quiz == null)
            {
                return NotFound();
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

            // Ensure QuizId is valid
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuizId == QuizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // Calculate total score
            int totalScore = 0;
            foreach (var answer in answers)
            {
                // Find the question in the quiz
                var question = quiz.Questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (question != null)
                {

                    // Find the correct answer for the question
                    var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect == 1);
                    if (correctAnswer != null && correctAnswer.AnswerId == answer.SelectedAnswerId)
                    {
                        totalScore += question.Points;
                    }
                }
            }

            // Save or update grade
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

            await _context.SaveChangesAsync();

            return RedirectToAction("CourseMainPage", "Courses", new { id = 0 });
        }


    }
}
