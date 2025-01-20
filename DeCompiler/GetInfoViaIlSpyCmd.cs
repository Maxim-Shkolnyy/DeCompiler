using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DeCompiler
{
    public static class GetInfoViaIlSpyCmd
    {
        public static void ProcessAssemblies(string assemblyFile, string assembliesFolder)
        {
            try
            {
                IEnumerable<string> files;

                if (string.IsNullOrEmpty(assembliesFolder))
                {
                    // Process all files in the directory
                    files = Directory.GetFiles(assemblyFile, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(file => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                                    file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    // Process the specific file
                    files = new List<string> { assembliesFolder };
                }

                foreach (var file in files)
                {
                    using (var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        Console.WriteLine($"Processing: {file}");
                        string assemblyInfo = GetAssemblyInfo(file);
                        Console.WriteLine(assemblyInfo);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access to the path '{assemblyFile}' is denied: {ex.Message}");
                AddAccessPremission(assemblyFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing files in directory '{assemblyFile}': {ex.Message}");
            }
        }

        public static string GetAssemblyInfo(string filePath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ilspycmd", // Ensure ilspycmd is available in PATH
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"ilspycmd failed with error: {error}");
                }

                return output;
            }
        }

        // Add this method to the GetInfoViaIlSpyCmd class
        public static void AddAccessPremission(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            fileSecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileSecurity);
        }
    }
}
