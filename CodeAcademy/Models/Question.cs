﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

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
    [StringLength(500)]
    [Unicode(false)]
    public string QuestionText { get; set; } = null!;

    public List<Answer> Answers { get; set; }


    [ForeignKey("QuizId")]
    [InverseProperty("Questions")]
    public virtual Quiz Quiz { get; set; } = null!;
    [Column("points")]
    public int Points { get; set; }
    public ICollection<StudentAnswer> StudentAnswers { get; set; }

}
