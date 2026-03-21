using System.ComponentModel.DataAnnotations.Schema;

[Table("ConsigneeUnit", Schema = "consignee")]
public class ConsigneeUnit
{
    public int id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("consignee")]
    public int ConsigneeId { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; }

    [Column("type")]
    public int Type { get; set; }

    [ForeignKey("ConsigneeId")]
    public Consignee Consignee { get; set; }
}