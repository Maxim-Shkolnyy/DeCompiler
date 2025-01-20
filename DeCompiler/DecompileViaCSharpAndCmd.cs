using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeCompiler
{
    public static class DecompileViaCSharpAndCmd
    {
        public static void Decompile(string assemblyFile, string assembliesFolder, string targetLanguage)
        {
            try
            {
                IEnumerable<string> files;
                if (string.IsNullOrEmpty(assembliesFolder))
                {
                    files = new List<string> { assemblyFile };
                }
                else
                {
                    files = new List<string> { assembliesFolder };
                }

                foreach (var file in files)
                {
                    if (DllCanBeDecompiled(file, targetLanguage = "C#")) //Remove ="C#"!
                    {
                        Console.WriteLine($"Decompiling: {file}");
                        string decompiledCode = GetDecompiledCode(file);
                        Console.WriteLine(decompiledCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decompiling files in directory '{assemblyFile}': {ex.Message}");
            }
        }

        // ...

        private static bool DllCanBeDecompiled(string dllPath, string targetLanguage)
        {
             if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath) )
            {
                return false;
            }

            string fileExtension = Path.GetExtension(dllPath).ToLower();

            switch (targetLanguage)
            {
                case "C#":
                    return true;
                case "IL":
                    return true;
                case "VB":
                    return true;
                default:
                    return false;
                    throw new NotSupportedException($"Language {targetLanguage} is not supported by ILSpy.");
            }
        }

        private static string GetDecompiledCode(string dllPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ilspycmd",
                    Arguments = $"-p {dllPath}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }        

    }
}

