using AsyncPoco;
using HL7toXDocumentParser;
using HL7v23Store.Model;
using log4net;
using PollerQueue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HL7v23Store
{
    class HL7FilePoller : FilePoller
    {
        #region Fields

        Parser hl7Parser;
        static readonly ILog log = LogManager.GetLogger(typeof(HL7FilePoller));

        #endregion

        #region Constructor

        public HL7FilePoller()
        {
            hl7Parser = new Parser();
        }

        #endregion

        #region FilePoller implementation

        protected override void LogException(string message, Exception ex, string currentItem)
        {
            if (log.IsErrorEnabled)
                log.Error(message, ex);

            if (!string.IsNullOrEmpty(currentItem) && File.Exists(currentItem))
                File.Delete(currentItem);
        }

        protected override async Task<bool> ProcessCurrentItem(string currentItem)
        { 
            Func<Task> action = async () =>
            {
                if (!File.Exists(currentItem))
                    return;

                using (var fs = new FileStream(currentItem, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete))
                {
                    using (var sr = new StreamReader(fs, Encoding.GetEncoding("iso-8859-1"))) //latin1
                    {
                        var hl7 = await sr.ReadToEndAsync();
                        var xDoc = hl7Parser.Parse(hl7);

                        using (var db = new Database("HL7v23"))
                        {
                            try
                            {
                                await db.BeginTransactionAsync();

                                await StoreHL7(db, xDoc, hl7, Path.GetFileName(currentItem));

                                db.CompleteTransaction();
                            }
                            catch (Exception)
                            {
                                db.AbortTransaction();
                                throw;
                            }
                        }

                        File.Delete(currentItem);
                    }
                }
            };

            await action.WrapSharingViolations(retryCount: 100);

            return true;
        }

        #endregion

        #region Methods

        async Task StoreHL7(Database db, XDocument xDoc, string hl7, string fileName)
        {
            var messageControlId = (from elem in xDoc.Descendants("MSH.10") select elem.Value).FirstOrDefault().ToDecimal();

            var exists = await db.ExistsAsync<HL7>("MessageControlId = @0", messageControlId);

            var record = new HL7();
            record.MessageControlId = messageControlId;
            record.MessageTimeStamp = (from elem in xDoc.Descendants("MSH.7") select elem.Value).FirstOrDefault().ToDatetime("yyyyMMddHHmmss", new DateTime(3000, 1, 1));
            record.MessageType = (from elem in xDoc.Descendants("MSH.9.1") select elem.Value).FirstOrDefault();
            record.EventType = (from elem in xDoc.Descendants("MSH.9.2") select elem.Value).FirstOrDefault();
            record.Message = hl7;
            record.FileName = fileName;

            if (record.MessageControlId == decimal.Zero || record.MessageType.IsNullOrEmpty() || record.EventType.IsNullOrEmpty())
                return;

            if (exists)
                await db.UpdateAsync("HL7v23.dbo.HL7", "MessageControlId", record);
            else
                await db.InsertAsync("HL7v23.dbo.HL7", "MessageControlId", false, record);

            if (record.MessageType == "MFN")
                await StoreMFN(db, xDoc, exists);
            else if (record.MessageType == "ADT")
                await StoreADT(db, xDoc, exists);
            else if (record.MessageType == "BAR")
                await StoreBAR(db, xDoc, exists);
            else
            {
                var message = string.Format("Need to store unsupported Message with MessageType {0}: {1}", record.MessageType, hl7);
                LogException(message, new Exception(message), null);   
            }

            await db.ExecuteAsync(@"update ZISv21.dbo.LastProcessedMessage
set MessageControlId = @0 
where [application] = @1 and MessageControlId < @0", messageControlId, "HL7v23");
        }

        async Task StoreADT(Database db, XDocument xDoc, bool exists)
        {
            var messageControlId = (from elem in xDoc.Descendants("MSH.10") select elem.Value).FirstOrDefault().ToDecimal();

            var record = new ADT();
            record.MessageControlId = messageControlId;
            record.EventTimeStamp = (from elem in xDoc.Descendants("EVN.6") select elem.Value).FirstOrDefault().ToDatetime("yyyyMMddHHmm",
                (from elem in xDoc.Descendants("PV1.44") select elem.Value).FirstOrDefault().ToDatetime("yyyyMMddHHmm",
                    new DateTime(3000, 1, 1)));
            record.PatientId = (from elem in xDoc.Descendants("PID.3") select elem.Value).FirstOrDefault().ToDecimal();
            record.VisitNumber = (from elem in xDoc.Descendants("PV1.19.1") select elem.Value).FirstOrDefault().ToDecimal();
            record.PreadmitNumber = (from elem in xDoc.Descendants("PV1.5") select elem.Value).FirstOrDefault();
            record.AdmissionUnitNumber = (from elem in xDoc.Descendants("PV1.10") select elem.Value).FirstOrDefault().ToDecimal();
            record.CampusCode = (from elem in xDoc.Descendants("PV1.3.4") select elem.Value).FirstOrDefault();
            record.NursingUnitNumber = (from elem in xDoc.Descendants("PV1.3.1") select elem.Value).FirstOrDefault().ToDecimal();
            record.RoomNumber = (from elem in xDoc.Descendants("PV1.3.2") select elem.Value).FirstOrDefault().ToDecimal();
            record.BedNumber = (from elem in xDoc.Descendants("PV1.3.3") select elem.Value).FirstOrDefault().Maybe(s => s.PadLeft(2, '0'));
            record.DoctorNumber = (from elem in xDoc.Descendants("PV1.17.1") select elem.Value).FirstOrDefault().ToDecimal();
            record.FamilyName = (from elem in xDoc.Descendants("PID.5.1") select elem.Value).FirstOrDefault();
            record.FirstName = (from elem in xDoc.Descendants("PID.5.2") select elem.Value).FirstOrDefault();
            record.PreviousCampusCode = (from elem in xDoc.Descendants("PV1.6.4") select elem.Value).FirstOrDefault();
            record.PreviousNursingUnitNumber = (from elem in xDoc.Descendants("PV1.6.1") select elem.Value).FirstOrDefault().ToDecimal();
            record.PreviousRoomNumber = (from elem in xDoc.Descendants("PV1.6.2") select elem.Value).FirstOrDefault().ToDecimal();
            record.PreviousBedNumber = (from elem in xDoc.Descendants("PV1.6.3") select elem.Value).FirstOrDefault().Maybe(s => s.PadLeft(2, '0'));

            if (exists)
                await db.UpdateAsync("HL7v23.dbo.ADT", "MessageControlId", record);
            else
                await db.InsertAsync("HL7v23.dbo.ADT", "MessageControlId", false, record);
        }

        async Task StoreMFN(Database db, XDocument xDoc, bool exists)
        {
            var messageControlId = (from elem in xDoc.Descendants("MSH.10") select elem.Value).FirstOrDefault().ToDecimal();

            var record = new MFN();
            record.MessageControlId = messageControlId;

            record.DoctorNumber = (from elem in xDoc.Descendants("STF.1") select elem.Value).FirstOrDefault().ToDecimal();

            var insuranceCompanyId = (from elem in xDoc.Descendants("IN1.3") select elem.Value).FirstOrDefault();
            if (!insuranceCompanyId.IsNullOrEmpty())
            {
                if (insuranceCompanyId.Length == 7)
                {
                    record.PlanId = insuranceCompanyId.ToCharArray(0, 1)[0];
                    record.InsuranceCompanyNumber = insuranceCompanyId.Substring(1, 6).ToDecimal();
                }
                else //mutualities
                {
                    record.PlanId = '2'; //for mutualities set planId to 2
                    record.InsuranceCompanyNumber = insuranceCompanyId.ToDecimal();
                }
            }

            if (exists)
                await db.UpdateAsync("HL7v23.dbo.MFN", "MessageControlId", record);
            else
                await db.InsertAsync("HL7v23.dbo.MFN", "MessageControlId", false, record);
        }

        async Task StoreBAR(Database db, XDocument xDoc, bool exists)
        {
            var messageControlId = (from elem in xDoc.Descendants("MSH.10") select elem.Value).FirstOrDefault();

            var record = new BAR();
            record.MessageControlId = messageControlId;
            record.PatientId = (from elem in xDoc.Descendants("PID.3") select elem.Value).FirstOrDefault();
            record.VisitNumber = (from elem in xDoc.Descendants("PV1.19.1") select elem.Value).FirstOrDefault();
            var insuranceCompanyId = (from elem in xDoc.Descendants("IN1.3") select elem.Value).FirstOrDefault();
            if (!insuranceCompanyId.IsNullOrEmpty())
            {
                if (insuranceCompanyId.Length == 7)
                {
                    record.PlanId = insuranceCompanyId.Substring(0, 1);
                    record.InsuranceCompanyNumber = insuranceCompanyId.Substring(1, 6);
                }
                else //mutualities
                {
                    record.PlanId = "2"; //for mutualities set planId to 2
                    record.InsuranceCompanyNumber = insuranceCompanyId;
                }
            }

            if (exists)
                await db.UpdateAsync("HL7v23.dbo.BAR", "MessageControlId", record);
            else
                await db.InsertAsync("HL7v23.dbo.BAR", "MessageControlId", false, record);
        }

        #endregion
    }
}
