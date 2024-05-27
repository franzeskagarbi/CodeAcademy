using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("questions")]
public partial class Question
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("quiz_id")]
    public int QuizId { get; set; }

    [Column("question_text")]
    [StringLength(200)]
    [Unicode(false)]
    public string QuestionText { get; set; } = null!;

    [InverseProperty("Question")]
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    [ForeignKey("QuizId")]
    [InverseProperty("Questions")]
    public virtual Quiz Quiz { get; set; } = null!;
}
