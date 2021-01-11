using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;

namespace JobMonitor
{
    public partial class JobMonitorService : ServiceBase
    {
        private Timer timer;
        string batchFilePath = string.Empty;
        string programName = string.Empty;
        string logFilePath = string.Empty;
        public JobMonitorService(string[] args)
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var appRunFrequency = Convert.ToInt32(ConfigurationManager.AppSettings["RunFrequencyInMins"]);
            //AuditLog FilePath
            logFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), $"JobMonitorAuditLog_{DateTime.Now.ToString("MM_dd_yyyy")}.txt");
            string localMachineName = Environment.MachineName;
            Process p = Process.GetProcesses(localMachineName).Where(c => c.ProcessName == "JobMonitor").FirstOrDefault();
            using (StreamWriter log = new StreamWriter(logFilePath, append: true))
            {
                log.Write("********************Job Monitor*******************" + Environment.NewLine);
                log.Write($"Info: Job Monitor Service Started at {DateTime.Now}, and runs at every {appRunFrequency}mins interval." + Environment.NewLine);

                if (args.Length == 2)
                {
                    batchFilePath = args[0];
                    EventLog.WriteEntry($"First Parameter Received BatchFilePath: {batchFilePath}.");
                    programName = args[1];
                    EventLog.WriteEntry($"Second Parameter Received Monitoring ProgramName: {programName}.");
                    if (File.Exists(batchFilePath))
                    {
                        log.Write($"Info: BatchFile Location: {batchFilePath}." + Environment.NewLine);
                        log.Write($"Info: Monitoring Program Name: {programName}." + Environment.NewLine);
                    }
                    else
                    {
                        log.Write($"Warning: BatchFile not exists in the given location: {batchFilePath} ." + Environment.NewLine);
                        p.Kill();
                    }
                    timer = new Timer(appRunFrequency * 60 * 1000);
                    timer.Elapsed += new ElapsedEventHandler(MonitorApps);
                    timer.Start();
                }
                else
                {
                    log.Write($"Error: Expected BatchFilePath and ProgramName not passed with Application start. Stopping the Job Monitor service." + Environment.NewLine);
                    p.Kill();
                }
            }
        }

        private void MonitorApps(object sender, ElapsedEventArgs e)
        {
            using (StreamWriter log = new StreamWriter(logFilePath, append: true))
            {
                try
                {
                    string localMachineName = Environment.MachineName;
                    var isProgramRunning = Process.GetProcesses(localMachineName).Where(c => c.ProcessName == programName).Any();
                    if (isProgramRunning)
                    {
                        log.Write($"Info: {DateTime.Now}; {programName} is Up and Running." + Environment.NewLine);
                    }
                    else
                    {
                        log.Write($"Info: {DateTime.Now}; {programName} is not running, Triggering Bat file at location {batchFilePath}. " + Environment.NewLine);
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/C " + batchFilePath,
                            CreateNoWindow = true,
                            ErrorDialog = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        startInfo.EnvironmentVariables.Add("CATALINA_HOME", Path.GetDirectoryName(batchFilePath));
                        var process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                        log.Write($"Info: {DateTime.Now} Bat File Triggered." + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    log.Write($"Error: Exception Occured. Error Message: {ex.Message}; InnerException: {ex.InnerException}." + Environment.NewLine);
                }
            }
        }

        protected override void OnStop()
        {
            using (StreamWriter log = new StreamWriter(logFilePath, append: true))
            {
                log.Write($"Warning: Service Stopped at {DateTime.Now}." + Environment.NewLine);
            }
            timer.Stop();
            timer.Dispose();
        }
    }
}
