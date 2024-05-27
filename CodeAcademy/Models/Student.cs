using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("student")]
[Index("UserId", Name = "IX_student", IsUnique = true)]
public partial class Student
{
    [Key]
    [Column("user_Id")]
    public int UserId { get; set; }

    [Column("name")]
    [StringLength(50)]
    [Unicode(false)]
    public string Name { get; set; } = null!;

    [Column("surname")]
    [StringLength(50)]
    [Unicode(false)]
    public string Surname { get; set; } = null!;

    [InverseProperty("Student")]
    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    [ForeignKey("UserId")]
    [InverseProperty("Student")]
    public virtual User User { get; set; } = null!;
}
