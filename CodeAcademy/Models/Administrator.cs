using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

[Table("administrator")]
public partial class Administrator
{
    [Key]
    [Column("user_Id")]
    public int UserId { get; set; }

    [Column("name")]
    [StringLength(50)]
    [Unicode(false)]
    public string Name { get; set; } = "tba";

    [Column("surname")]
    [StringLength(50)]
    [Unicode(false)]
    public string Surname { get; set; } = "tba"!;

    [ForeignKey("UserId")]
    [InverseProperty("Administrator")]
    public virtual User User { get; set; } = null!;
}
