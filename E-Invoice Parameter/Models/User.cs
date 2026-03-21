using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("User", Schema = "security")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Column("person")]
    public int PersonId { get; set; }  // REQUIRED (Unchecked)

    [Column("userName")]
    [Required]
    [StringLength(50)] // Matches your DB nvarchar(50)
    public string UserName { get; set; }

    [Column("password")]
    [Required]
    [StringLength(128)] // Matches your DB nvarchar(128)
    public string PasswordHash { get; set; }

    [Column("salt")]
    [Required]
    [StringLength(128)] // Matches your DB nvarchar(128)
    public string Salt { get; set; }

    [Column("loggedInStatus")]
    public int? LoggedInStatus { get; set; } // Matches DB 'int' (Checked/Nullable)

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("remark")]
    public string? Remark { get; set; } // Nullable

    [Column("createdAt")]
    public DateTime? CreatedAt { get; set; } // Nullable in your DB

    [Column("firstLoginAt")]
    public DateTime? FirstLoginAt { get; set; }

    [Column("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [Column("modifiedAt")]
    public DateTime? ModifiedAt { get; set; }

    [ForeignKey("PersonId")]
    public virtual Consignee? Person { get; set; }
}