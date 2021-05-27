using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace SynLight.Model
{
    public static class Startup
    {
        /// <summary>
        /// Starting Mobile hotstop using Powershell
        /// </summary>
        public static void StartMobileHotstop()
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                string psScript = "$connectionProfile = [Windows.Networking.Connectivity.NetworkInformation,Windows.Networking.Connectivity,ContentType=WindowsRuntime]::GetInternetConnectionProfile()\n$tetheringManager = [Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager,Windows.Networking.NetworkOperators,ContentType=WindowsRuntime]::CreateFromConnectionProfile($connectionProfile)\n$v1 = 25\n$v2 = 0\nwhile($tetheringManager.TetheringOperationalState -eq \"Off\")\n{\n$a = [\nWindows.Networking.NetworkOperators.NetworkOperatorTetheringManager, Windows.Networking.NetworkOperators, ContentType = WindowsRuntime]::CreateFromConnectionProfile([Windows.Networking.Connectivity.NetworkInformation, Windows.Networking.Connectivity, ContentType = WindowsRuntime]::GetInternetConnectionProfile())\n$a.StartTetheringAsync()\nStart-Sleep -Seconds 0.5\necho $v2\n    $v2 = $v2 + 1\nif($v2 -le $v1)\n{\nbreak\n}\n}";
                //PowerShellInstance.AddScript("$a = [Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager, Windows.Networking.NetworkOperators, ContentType = WindowsRuntime]::CreateFromConnectionProfile([Windows.Networking.Connectivity.NetworkInformation, Windows.Networking.Connectivity, ContentType = WindowsRuntime]::GetInternetConnectionProfile())\n$a.StartTetheringAsync()");
                PowerShellInstance.AddScript(psScript);
                IAsyncResult result = PowerShellInstance.BeginInvoke();
                while (result.IsCompleted == false)
                {
                    Console.WriteLine("Waiting for pipeline to finish...");
                    Thread.Sleep(5);
                }
                Console.WriteLine("Modile Hotstop started!");
            }
        }
        
        public static void StopMobileHotstop()
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                string psScript = "$connectionProfile = [Windows.Networking.Connectivity.NetworkInformation,Windows.Networking.Connectivity,ContentType=WindowsRuntime]::GetInternetConnectionProfile()\n$tetheringManager = [Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager,Windows.Networking.NetworkOperators,ContentType=WindowsRuntime]::CreateFromConnectionProfile($connectionProfile)\n$v1 = 25\n$v2 = 0\nwhile($tetheringManager.TetheringOperationalState -eq \"On\")\n{\n$a = [\nWindows.Networking.NetworkOperators.NetworkOperatorTetheringManager, Windows.Networking.NetworkOperators, ContentType = WindowsRuntime]::CreateFromConnectionProfile([Windows.Networking.Connectivity.NetworkInformation, Windows.Networking.Connectivity, ContentType = WindowsRuntime]::GetInternetConnectionProfile())\n$a.StopTetheringAsync()\nStart-Sleep -Seconds 0.5\necho $v2\n    $v2 = $v2 + 1\nif($v2 -le $v1)\n{\nbreak\n}\n}";
                //PowerShellInstance.AddScript("$a = [Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager, Windows.Networking.NetworkOperators, ContentType = WindowsRuntime]::CreateFromConnectionProfile([Windows.Networking.Connectivity.NetworkInformation, Windows.Networking.Connectivity, ContentType = WindowsRuntime]::GetInternetConnectionProfile())\n$a.StartTetheringAsync()");
                PowerShellInstance.AddScript(psScript);
                IAsyncResult result = PowerShellInstance.BeginInvoke();
                while (result.IsCompleted == false)
                {
                    Console.WriteLine("Waiting for pipeline to finish...");
                    Thread.Sleep(5);
                }
                Console.WriteLine("Modile Hotstop started!");
            }
        }


        /// <summary>
        /// Start anew or Kill the old process
        /// </summary>
        public static void StartOrKill()
        {
            Process[] xaml = Process.GetProcesses();
            foreach (Process p in xaml)
            {
                if (p.ProcessName.Contains("XAML Designer") || p.ProcessName.Contains("XDesProc"))
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }
                }
            }
            string procName = Process.GetCurrentProcess().ProcessName;
            List<Process> processes = Process.GetProcessesByName(procName).ToList();
            while (processes.Count > 1)
            {
                if (processes[0].StartTime > processes[1].StartTime)
                {
                    processes[1].Kill();
                    processes[0].Kill();
                }
                else
                {
                    processes[0].Kill();
                    processes[1].Kill();
                }
                processes = Process.GetProcessesByName(procName).ToList();
            }
            processes[0].PriorityClass = ProcessPriorityClass.Idle;
        }
        /// <summary>
        /// Removing some files from the project
        /// </summary>
        public static void CleanFiles()
        {
            //Do not clean if not on /bin
            var currentDir = Directory.GetCurrentDirectory().Split('\\').Last().ToLower();
            if(currentDir == "debug" || currentDir == "release")
            {
                //.vs
                try
                {
                    string[] vsPathSplits = Directory.GetCurrentDirectory().Split('\\');
                    string vsPath = "";
                    for (int n = 0; n < vsPathSplits.Length - 3; n++)
                    {
                        vsPath += vsPathSplits[n] + '\\';
                    }
                    Directory.Delete(vsPath + ".vs", true);
                    File.Delete(vsPath + "README.md");
                }
                catch { }

                //obj
                try
                {
                    string[] objPathSplits = Directory.GetCurrentDirectory().Split('\\');
                    string objPath = "";
                    for (int n = 0; n < objPathSplits.Length - 2; n++)
                    {
                        objPath += objPathSplits[n] + '\\';
                    }
                    objPath += "obj";
                    Directory.Delete(objPath, true);
                }
                catch { }

                //bin
                try
                {
                    string process = AppDomain.CurrentDomain.FriendlyName;
                    string[] fileList = Directory.GetFiles(Directory.GetCurrentDirectory());

                    foreach (string file in fileList)
                    {
                        string split = file.Split('\\').Last();

                        if ((!split.ToLower().Contains("param")) && (split != process) && (split.Contains(".ps1")))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }
        /// <summary>
        /// Check if the MainForm is to be shown or not
        /// </summary>
        /// <returns></returns>
        public static bool ShowOrHide()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Param_SynLight.param))
                {
                    string[] lines = sr.ReadToEnd().Split('\n');
                    foreach (string line in lines)
                    {
                        string[] subLine = line.Trim('\r').Split('=');
                        if (subLine[0] == "SHOW")
                        {
                            return false;
                        }
                    }
                }
            }
            catch { }
            return true;
        }
    }
}
