using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("UserRoleMapper", Schema = "security")]
public class UserRoleMapper
{
    [Key]
    public int Id { get; set; }

    [Column("role")]
    public int RoleId { get; set; }

    [Column("user")]
    public int UserId { get; set; }

    [Column("expiryDate")]
    public DateTime? ExpiryDate { get; set; }

    [Column("remark")]
    public string Remark { get; set; }

    [ForeignKey("RoleId")]
    public virtual ConsigneeUnit Role { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}