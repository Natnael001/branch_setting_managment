using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Invoice_Parameter.Models
{
    [Table("SystemConstant", Schema = "setting")]
    public class SystemConstants
    {
        public int Id { get; set; }

        public string Type { get; set; }

        public int? Index { get; set; }

        public string Description { get; set; }

        public string? Category { get; set; }

        public string? Value { get; set; }

        public int? ParentId { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public int? NavType { get; set; }

        public string? Remark { get; set; }

    }
}
