using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SynLight.Model
{
    public static class Startup
    {
        /// <summary>
        /// Starting Mobile hotstop using Powershell
        /// </summary>
        public static void MobileHotstop()
        {
            
        }


        /// <summary>
        /// Start anew or Kill the old process
        /// </summary>
        public static void StartOrKill()
        {
            Process[] xaml = Process.GetProcesses().OrderBy(m => m.ProcessName).ToArray();

            foreach (Process p in xaml)
            {
                if(p.ProcessName.Contains("XAML Designer") || p.ProcessName.Contains("XDesProc"))
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

            foreach(Process p in processes)
            {
                if(p.Id != Process.GetCurrentProcess().Id)
                    p.Kill();
            }
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

                        if((!split.ToLower().Contains("param")) && (split != process) && (split.Contains(".ps1")))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
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
                        if(subLine[0] == "SHOW")
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
            }

            return true;
        }
    }
}