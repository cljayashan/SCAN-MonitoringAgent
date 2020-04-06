using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scan.Entities
{
    public class Server
    {
        public int PingTimeOut { get; set; }
        public int TimerInterval { get; set; }
        public string Gateway { get; set; }
        public string ServerName { get; set; }
        public string TestPingDomain{ get; set; }
    }
}
