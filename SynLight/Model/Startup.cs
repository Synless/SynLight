using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SynLight.Model
{
    public static class Startup
    {
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
                    p.Kill();
                }
            }
            string procName = Process.GetCurrentProcess().ProcessName;
            System.Collections.Generic.List<Process> processes = Process.GetProcessesByName(procName).ToList();
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
            //Do not clean if not on /bin/Debug
            var tmp = Directory.GetParent("./").FullName;
            string tmp2 = tmp.Split('\\')[tmp.Split('\\').Length-1].ToLower();
            if(tmp2 != "debug")
            {
                return;
            }
            //.vs
            try
            {
                string[] vsPathSplits = Directory.GetCurrentDirectory().Split('\\');
                string vsPath = "";
                for(int n = 0; n < vsPathSplits.Length-3;n++)
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
                string process = Process.GetCurrentProcess().ToString().Split('(')[1];
                process = process.Remove(process.Length - 2) + ".exe";
                string[] fileList = Directory.GetFiles(Directory.GetCurrentDirectory());
                foreach (string file in fileList)
                {
                    string[] fileSplit = file.Split('\\');
                    string split = fileSplit[fileSplit.Length - 1];
                    
                    if (split != "param.txt" && split != process)
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