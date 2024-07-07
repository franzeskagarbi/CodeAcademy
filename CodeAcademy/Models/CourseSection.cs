using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("courseSections")]
public partial class CourseSection
{
    [Key]
    [Column("section_Id")]
    public int SectionId { get; set; }

    [Column("course_Id")]
    public int CourseId { get; set; }

    [Column("section_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string SectionName { get; set; } = null!;

    [Column("description")]
    [StringLength(5000)]
    [Unicode(false)]
    public string Description { get; set; } = null!;

    [Column("section_level")]
    [StringLength(50)]
    [Unicode(false)]
    public string SectionLevel { get; set; } = null!;

    [ForeignKey("CourseId")]
    [InverseProperty("CourseSections")]
    public virtual Course Course { get; set; } = null!;

    [InverseProperty("Section")]
    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
