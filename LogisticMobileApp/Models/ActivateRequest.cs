using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticMobileApp.Models
{
    public class ActivateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Device_name { get; set; } = string.Empty;
        public string Device_identifier { get; set; } = string.Empty;
    }
}
