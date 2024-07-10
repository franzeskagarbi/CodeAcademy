using CodeAcademy.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class StudentAnswer
{
    [Key]
    [Column("answerId")]
    public int AnswerId { get; set; }

    [Column("studentId")]
    public int? StudentId { get; set; }

    [Column("quizId")]
    public int? QuizId { get; set; }

    [Column("questionId")]
    public int? QuestionId { get; set; }

    [Column("chosen_answer_id")]
    public int? ChosenAnswerId { get; set; }

    [Column("is_correct")]
    public bool? IsCorrect { get; set; }

    // Navigation properties (if needed)
    [ForeignKey("ChosenAnswerId")]
    public Answer ChosenAnswer { get; set; }

    [ForeignKey("QuestionId")]
    public Question Question { get; set; }

    [ForeignKey("QuizId")]
    public Quiz Quiz { get; set; }

    [ForeignKey("StudentId")]
    public Student Student { get; set; }
}
