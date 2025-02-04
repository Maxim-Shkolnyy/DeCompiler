using System.Runtime.InteropServices;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace DeCompiler;

static class Program
{
    public static void Main(string[] args)
    {
        string assemblyFile = "D:\\Work\\everwood5\\Bin\\Echoes.dll";
        assemblyFile = "";

        string assembliesFolder = "D:\\Work\\everwood5\\Bin";

        GetInfoViaIlSpy.ProcessAssemblies(assemblyFile, assembliesFolder);


        //ExecutePowerShellScript.ExecuteScript(assemblyFile);
        //DecompileViaCSharpAndCmd.Decompile(assemblyFile, assembliesFolder, "C#");
        //ProcessAssembly(assemblyFile);
    }
}


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
                //GetInfoViaIlSpyOld.AddAccessPremissionViaCmd(assemblyFile);
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
}
