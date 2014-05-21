using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace HL7v23Store
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.UseLog4Net();

                x.Service<HL7FilePoller>(s =>
                {
                    s.ConstructUsing(() =>
                    {
                        return new HL7FilePoller()
                            {
                                PollerPath = ConfigurationManager.AppSettings["PollerPath"],
                                MaxDegreeOfParallelism = ConfigurationManager.AppSettings["MaxDegreeOfParallelism"].ToInt32(),
                                SearchPattern = ConfigurationManager.AppSettings["SearchPattern"]
                            };
                    });
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                });

                x.SetDescription("HL7v23Store");
                x.SetDisplayName("HL7v23Store");
                x.SetServiceName("HL7v23Store");
            });
        }
    }
}
