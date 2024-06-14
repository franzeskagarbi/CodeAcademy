using CodeAcademy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

            var questionViewModels = questions.Select(q => new QuestionViewModel
            {
                QuestionText = q.QuestionText,
            }).ToList();

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
                // Εδώ μπορείτε να κάνετε τις απαραίτητες ενέργειες για να αποθηκεύσετε την ερώτηση

                // Παράδειγμα αποθήκευσης στη βάση δεδομένων (υποθέτοντας ότι έχετε σχετικό context _context)
                var question = new Question
                {
                    QuizId = model.QuizId,
                    QuestionText = model.QuestionText
                    // Προσθέστε άλλα πεδία αν χρειάζεται
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // Μετά την επιτυχή αποθήκευση, μπορείτε να κάνετε redirect σε άλλη προβολή
                return RedirectToAction("ViewQuestions", new { quizId = model.QuizId });
            }

            // Αν το ModelState δεν είναι έγκυρο, επιστροφή στην ίδια προβολή με τα σφάλματα του ModelState
            return View(model);
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
    }
}
