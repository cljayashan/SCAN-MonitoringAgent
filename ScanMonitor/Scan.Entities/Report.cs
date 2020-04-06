using Scan.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scan.Entities
{
    public class Report
    {
        public Report() {}

        public Report(DateTime roundTime, EnumTestCriteria criteria, EnumTestResults result)
        {
            this.RoundTime = roundTime;
            this.Criteria = criteria;
            this.Result = result;
        }

        public DateTime RoundTime { get; set; }
        public EnumTestCriteria Criteria { get; set; }
        public EnumTestResults Result { get; set; }
        public string Remarks { get; set; }
    }
}
