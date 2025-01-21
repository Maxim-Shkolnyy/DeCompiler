using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace DeCompiler
{
    public static class GetInfoViaIlSpy
    {
        private static int attempts = 0;
        public static void ProcessAssemblies(string assemblyFile, string assembliesFolder)
        {
            try
            {
                List<string> files = new List<string>();

                if (string.IsNullOrEmpty(assemblyFile))
                {
                    files = Directory.GetFiles(assembliesFolder, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(file => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                                    file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else
                {
                    files.Add(assemblyFile);
                }

                foreach (var file in files)
                {
                    using (var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        Console.WriteLine($"Processing: {file}");
                        string assemblyInfo = GetAssemblyInfo(file);
                        foreach (var line in assemblyInfo.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                        {
                            Console.WriteLine(line);
                        }
                        Console.WriteLine($"____________________________________________________________________________________________DONE");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                attempts++;
                Console.WriteLine($"Access to the path '{assemblyFile}' is denied: {ex.Message}");
                AddAccessPremission(assemblyFile);
                if (attempts < 3)
                {
                    ProcessAssemblies(assemblyFile, assembliesFolder);
                }
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
                    FileName = "ilspycmd",
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

        public static void AddAccessPremission(string filePath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"if ((Get-ExecutionPolicy) -eq 'Restricted') {{ Set-ExecutionPolicy RemoteSigned -Scope CurrentUser -Force }}; " +
                                $"$acl = Get-Acl '{filePath}'; " +
                                "$rule = New-Object System.Security.AccessControl.FileSystemAccessRule('Everyone', 'FullControl', 'Allow'); " +
                                "$acl.SetAccessRule($rule); " +
                                $"Set-Acl '{filePath}' $acl\"",
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
                    throw new Exception($"Failed to set access permissions: {error}");
                }

                Console.WriteLine(output);
            }
        }

        public static void AddAccessPremissionViaSecurityIdentifier(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            fileSecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileSecurity);
        }
    }
}
