using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Keyless]
[Table("course_has_student")]
public partial class CourseHasStudent
{
    [Column("course_Id")]
    public int CourseId { get; set; }

    [Column("student_Id")]
    public int StudentId { get; set; }

    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;
}
