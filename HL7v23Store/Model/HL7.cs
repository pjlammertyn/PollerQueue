using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7v23Store.Model
{
    class HL7
    {
        public decimal MessageControlId { get; set; }
        public DateTime MessageTimeStamp { get; set; }
        public string MessageType { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public string Creator { get; set; }
    }
}
