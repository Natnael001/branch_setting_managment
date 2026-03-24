using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Configuration", Schema = "setting")]
public class Configuration
{
    [Key]
    public int Id { get; set; }

    public int Pointer { get; set; }

    public string Reference { get; set; } = string.Empty; // Initialized

    [Column("consigneeUnit")]
    public int? ConsigneeUnitId { get; set; }

    public string Attribute { get; set; } = string.Empty; // Initialized

    public string CurrentValue { get; set; } = string.Empty; // Initialized

    public string? PreviousValue { get; set; }

    public string? Remark { get; set; }
}