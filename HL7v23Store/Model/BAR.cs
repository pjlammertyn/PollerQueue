using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7v23Store.Model
{
    class BAR
    {
        public string MessageControlId { get; set; }
        public string PatientId { get; set; }
        public string VisitNumber { get; set; }
        public string PlanId { get; set; }
        public string InsuranceCompanyNumber { get; set; }
    }
}
