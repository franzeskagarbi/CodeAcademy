using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("teacher")]
[Index("UserId", Name = "IX_teacher", IsUnique = true)]
public partial class Teacher
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

    [Column("phone_number")]
    public int PhoneNumber { get; set; }

    [InverseProperty("Teacher")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    [ForeignKey("UserId")]
    [InverseProperty("Teacher")]
    public virtual User User { get; set; } = null!;
}
