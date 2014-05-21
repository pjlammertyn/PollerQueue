using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7v23Store.Model
{
    class MFN
    {
        public decimal MessageControlId { get; set; }
        public decimal DoctorNumber { get; set; }
        public char PlanId { get; set; }
        public decimal InsuranceCompanyNumber { get; set; }
    }
}
