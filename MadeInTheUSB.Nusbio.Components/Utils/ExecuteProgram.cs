using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MadeInTheUSB
{
    public class ExecuteProgram
    {
        public static bool ExecProgram(string strProgram, string strParameter, bool booWait, ref int intExitCode, bool booSameProcess, bool booHidden)
        {
            System.Diagnostics.Process proc;
            try
            {
                if (booSameProcess)
                {
                    proc = System.Diagnostics.Process.GetCurrentProcess();
                }
                else
                {
                    proc = new System.Diagnostics.Process();
                }
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = strProgram;
                proc.StartInfo.Arguments = strParameter;

                if (booHidden)
                {
                    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                }
                var started = proc.Start();
                if (!started)
                {
                    System.Diagnostics.Debug.WriteLine("Not started");
                }

                if (booWait)
                {
                    proc.WaitForExit();
                    intExitCode = proc.ExitCode;
                }
                return true;
            }
            catch(System.Exception ex)
            {
                return false;
            }
        }

        public class ProgramExecutionCapture {

                public string Output;
                public string ErrorOutput;
                public int Time;
                public string CommandLine;
                public int ExitCode;

                public ProgramExecutionCapture(){

                    Output      = "";
                    ErrorOutput = "";
                    Time        = -1;
                    CommandLine = "";
                    ExitCode  = -1;
                }
        }

        public static ProgramExecutionCapture ExecProgramAndCaptureOutput(string strProgram, string strParameter, bool booSameProcess, bool booHidden)
        {
            var e                     = new ProgramExecutionCapture();
            e.Time                    = Environment.TickCount;
            StreamReader outputReader = null;
            StreamReader errorReader  = null;
            try {
                
                ProcessStartInfo processStartInfo = new ProcessStartInfo(strProgram, strParameter);
                processStartInfo.ErrorDialog = false;
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardInput = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = booHidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal;
                //processStartInfo.WorkingDirectory = this.NodePath;

                Process process = new Process();
                process.StartInfo = processStartInfo;
                bool processStarted = process.Start();

                if (processStarted)
                {
                    outputReader = process.StandardOutput;
                    errorReader = process.StandardError;
                    process.WaitForExit();

                    e.ExitCode = process.ExitCode;
                    e.Output = outputReader.ReadToEnd();
                    e.ErrorOutput = errorReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                e.ErrorOutput += "Error lanching the nodejs.exe = "+ ex.ToString();
            }
            finally {
                if (outputReader != null)
                    outputReader.Close();
                if (errorReader != null)
                    errorReader.Close();

                e.Output = e.Output.Replace("\n", "\r\n");
                e.ErrorOutput = e.ErrorOutput.Replace("\n", "\r\n");
            }
            e.Time = Environment.TickCount - e.Time;
            return e;   
        }
        
        public static bool OpenTextFile(string fileName)
        {
            int exitCode = -1;
            return ExecProgram("notepad.exe", $@"""{fileName}""", false, ref exitCode, false, false);
        }

        public static bool OpenFileSystemFolder(string fileSytemFolder)
        {
            int exitCode = -1;
            return ExecProgram("explorer.exe", $@"""{fileSytemFolder}""", false, ref exitCode, false, false);
        }

        public class ExecutionInfo
        {
            public string Program;
            public string CommandLine;
            public bool Hidden = false;
            public bool Wait = true;
            public int ExitCode = -1;
            public bool ExecCode;

            public override string ToString()
            {
                return $@"Execute ""{Program}"" {CommandLine}, Wait:{Wait}, Hidden:{Hidden}";
            }
            public string ShortInfo()
            {
                return $@"Execute ""{Program}"" {CommandLine}";
            }
        }

        public static bool ExecProgram(List<ExecutionInfo> list, bool stopOnFirstError, bool toConsole)
        {
            for(var i=0; i<list.Count; i++)
            {
                var ei = list[i];

                ei.ExecCode = ExecProgram(ei.Program, ei.CommandLine, ei.Wait, ref ei.ExitCode, false, ei.Hidden);
                
                if(ei.ExitCode!=0 && stopOnFirstError)
                {
                    return false;
                }
            }
            return list.All(e => e.ExitCode == 0);
        }

        public static bool OpenURL(string url)
        {
            try
            {
                System.Diagnostics.Process proc;
                proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = url;
                proc.Start();
                return true;
            }
            catch(System.Exception ex)
            {
                return false;
            }
        }
    }
}
