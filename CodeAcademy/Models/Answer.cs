using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("q_answers")]
public partial class Answer
{
    [Key]
    [Column("answer_id")]
    public int AnswerId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("answer")]
    [StringLength(500)]
    [Unicode(false)]
    public string Answer1 { get; set; } = null!;

    [Column("is_correct")]
    public int IsCorrect { get; set; } 

    [ForeignKey("QuestionId")]
    [InverseProperty("Answers")]
    public virtual Question Question { get; set; } = null!;
}
