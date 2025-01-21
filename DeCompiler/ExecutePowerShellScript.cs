using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DeCompiler
{
    public static class ExecutePowerShellScript
    {
        public static void ExecuteScript(string targetPath)
        {
            string scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\ProcessAssembly.ps1").Replace("\\", "/");

            string powerShellPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = powerShellPath,
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFilePath}\" -TargetPath {targetPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();

            process.WaitForExit();

            Console.WriteLine("PowerShell Output:");
            Console.WriteLine(output);

            if (!string.IsNullOrWhiteSpace(errors))
            {
                Console.WriteLine("PowerShell Errors:");
                Console.WriteLine(errors);
            }
        }
    }
}
