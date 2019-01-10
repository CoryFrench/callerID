using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallerID_v2
{
    class Call
    {
        public DateTime _start { get; set; }
        public DateTime _end { get; set; }
        public string _callNumber { get; set; }
        public bool _callLive { get; set; }

        public Call(DateTime Start, DateTime End, string CallNumber, bool CallLive)
        {
            _start = Start;
            _end = End;
            _callNumber = CallNumber;
            _callLive = CallLive;
        }
    }
}
