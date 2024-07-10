using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("course")]
public partial class Course
{
    [Key]
    [Column("course_Id")]
    public int CourseId { get; set; }

    [Column("title")]
    [StringLength(50)]
    [Unicode(false)]
    public string Title { get; set; } = null!;

    [Column("teacher_Id")]
    public int TeacherId { get; set; }

    [Column("description")]
    [StringLength(1000)]
    [Unicode(false)]
    public string Description { get; set; } = null!;

    public string? ImageUrl { get; set; }


    [InverseProperty("Course")]
    public virtual ICollection<CourseSection> CourseSections { get; set; } = new List<CourseSection>();

    [ForeignKey("TeacherId")]
    [InverseProperty("Courses")]
    public virtual Teacher Teacher { get; set; } = null!;

}
