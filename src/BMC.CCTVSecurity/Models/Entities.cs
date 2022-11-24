using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMC.CCTVSecurity.Models
{
    public class CCTVImage
    {
        public string CctvName { get; set; }
        public byte[] ImageBytes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    internal class Entities
    {
    }
}
