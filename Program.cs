using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace JobMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var logFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), $"JobMonitorAuditLog_{DateTime.Now.ToString("MM/dd/yyyy")}.txt");
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new JobMonitorService(args)
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
