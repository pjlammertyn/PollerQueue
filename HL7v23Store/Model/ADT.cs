using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7v23Store.Model
{
    class ADT
    {
        public decimal MessageControlId { get; set; }
        public DateTime EventTimeStamp { get; set; }
        public decimal PatientId { get; set; }
        public decimal VisitNumber { get; set; }
        public string PreadmitNumber { get; set; }
        public decimal AdmissionUnitNumber { get; set; }
        public string CampusCode { get; set; }
        public decimal NursingUnitNumber { get; set; }
        public decimal RoomNumber { get; set; }
        public string BedNumber { get; set; }
        public decimal DoctorNumber { get; set; }
        public string FamilyName { get; set; }
        public string FirstName { get; set; }
        public string PreviousCampusCode { get; set; }
        public decimal PreviousNursingUnitNumber { get; set; }
        public decimal PreviousRoomNumber { get; set; }
        public string PreviousBedNumber { get; set; }
    }
}
