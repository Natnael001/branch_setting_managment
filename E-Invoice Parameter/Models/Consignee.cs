using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Consignee", Schema = "consignee")]
public class Consignee
{
    [Key]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } // Database usually expects a unique string or number

    [Column("gslType")]
    public int GslType { get; set; }

    [Column("tin")]
    public string? Tin { get; set; }

    [Column("isPerson")]
    public bool IsPerson { get; set; } = true;

    [Column("firstName")]
    public string? FirstName { get; set; }

    [Column("secondName")]
    public string? SecondName { get; set; }

    [Column("preference")]
    public int Preference { get; set; } // Changed to int (non-nullable) based on your DB list

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("locked")]
    public bool Islocked { get; set; } = true;

    [Column("createdOn")]
    public DateTime CreatedOn { get; set; }

    [Column("lastModified")]
    public DateTime LastModified { get; set; }

    public virtual ICollection<ConsigneeUnit>? ConsigneeUnits { get; set; }
}