using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Invoice_Parameter.Models
{
    [Table("Device", Schema = "setting")]
    public partial class Device
    {
        public int Id { get; set; }

        public int Article { get; set; }

        public string MachineName { get; set; } = null!;

        public string? Description { get; set; }

        public int? ConnectionType { get; set; }

        public int? SystemModel { get; set; }

        public int Type { get; set; }

        public int? Host { get; set; }

        public string? DeviceValue { get; set; }

        public int? Preference { get; set; }

        public string? IpAddress { get; set; }

        public string? MacAddress { get; set; }

        public int? IpPort { get; set; }

        public int? SerialPort { get; set; }

        public int? ConsigneeUnit { get; set; }

        public bool? IsEvenParity { get; set; }

        public int? BaudRate { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? LastModified { get; set; }

        public bool IsActive { get; set; }

        public string? Remark { get; set; }
        //public virtual SystemConstants? SystemConstantNavigation { get; set; }
    }

}