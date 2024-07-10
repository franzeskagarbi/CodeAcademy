using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("quiz")]
public partial class Quiz
{
    [Key]
    [Column("quiz_Id")]
    public int QuizId { get; set; }

    [Column("section_Id")]
    public int SectionId { get; set; }

    [Column("quiz_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string QuizName { get; set; } = null!;

    [Column("total_points")]
    public int TotalPoints { get; set; }

    [Column("isFinal")]
    public bool IsFinal { get; set; } = false; //default to false for regular quizzes

    [InverseProperty("Quiz")]
    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    [InverseProperty("Quiz")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    [ForeignKey("SectionId")]
    [InverseProperty("Quizzes")]
    public virtual CourseSection Section { get; set; } = null!;
    public ICollection<StudentAnswer> StudentAnswers { get; set; }
}
