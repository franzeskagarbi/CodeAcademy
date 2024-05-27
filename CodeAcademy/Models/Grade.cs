using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("grades")]
public partial class Grade
{
    [Key]
    [Column("grade_Id")]
    public int GradeId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("quiz_id")]
    public int QuizId { get; set; }

    [Column("score")]
    public int Score { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [ForeignKey("QuizId")]
    [InverseProperty("Grades")]
    public virtual Quiz Quiz { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("Grades")]
    public virtual Student Student { get; set; } = null!;
}
