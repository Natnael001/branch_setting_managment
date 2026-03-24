// Models/Article.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Invoice_Parameter.Models
{
    [Table("Article", Schema = "article")]
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LocalCode { get; set; } // Random Fixed Asset Code

        [Required]
        public int GslType { get; set; } = 15; // 15 for devices

        [Required]
        public string Name { get; set; } // Device name

        [Required]
        public int Uom { get; set; } = 1400; // Default unit of measure

        [Required]
        public int Preference { get; set; } = 1;

        [Required]
        public bool NeedsUOMConversion { get; set; } = true;

        [Required]
        public DateTime CreatedOn { get; set; }

        [Required]
        public DateTime LastModified { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool Locked { get; set; } = false;

        // Optional fields (Checked in schema)
        public string? Description { get; set; }
        public int? DefaultSupplier { get; set; }
        public string? Model { get; set; }
        public int? User { get; set; }
        public string? Remark { get; set; }
    }
}