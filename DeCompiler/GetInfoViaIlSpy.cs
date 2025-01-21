using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                        //string assemblyInfo = GetAssemblyInfoViaCmd(file);

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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    AddAccessPremissionViaSecurityIdentifier(assemblyFile);
                }
                else
                {
                    AddAccessPremissionViaCmd(assemblyFile);
                }
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
            try
            {
                var module = new PEFile(filePath);
                var decompiler = new CSharpDecompiler(filePath, new DecompilerSettings());
                var syntaxTree = decompiler.DecompileWholeModuleAsSingleFile();
                return syntaxTree.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decompile assembly: {ex.Message}");
            }
        }


        

        public static void AddAccessPremissionViaSecurityIdentifier(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
                System.Security.Principal.SecurityIdentifier everyone = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
                fileSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(everyone, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                fileInfo.SetAccessControl(fileSecurity);
            }
            else
            {
                throw new PlatformNotSupportedException("This method is only supported on Windows.");
            }
        }



        public static string GetAssemblyInfoViaCmd(string filePath)
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

        public static void AddAccessPremissionViaCmd(string filePath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"777 \"{filePath}\"",
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
    }
}
