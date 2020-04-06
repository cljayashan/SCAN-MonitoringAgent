using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scan.Entities
{
    public class Client
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string IpAddress { get; set; }
        public string WinLogin { get; set; }
        public string DisplayName { get; set; }
    }
}
