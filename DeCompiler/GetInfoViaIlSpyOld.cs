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
using ICSharpCode.Decompiler.TypeSystem;
using System.Reflection.Metadata;

namespace DeCompiler
{
    public static class GetInfoViaIlSpyOld
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


























        static void ProcessAssembly(string assemblyPath)
        {
            using (var file = new PEFile(assemblyPath))
            {
                //PrintHeaders(file);
                PrintAssemblyAttributes(file);
                //PrintAssemblyDependencies(file);

                var resolver = new UniversalAssemblyResolver(assemblyPath, false, null);
                var typeSystem = new DecompilerTypeSystem(file, resolver);

                foreach (var type in typeSystem.MainModule.TypeDefinitions)
                {
                    //GenerateHeaderForType(type);
                }

                //DecompileClassesAndMethods(assemblyFile);
            }
        }

        static void PrintHeaders(PEFile file)
        {
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine("Assembly Headers:");
            Console.WriteLine($"File Name: {file.FileName}");
            Console.WriteLine($"Metadata Version: {file.Metadata.MetadataVersion}");
            Console.WriteLine($"Machine: {file.Reader.PEHeaders.CoffHeader.Machine}");
            //Console.WriteLine($"Time Stamp: {file.Reader.PEHeaders.PEHeader.TimDateStamp}");
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine("Sections:");
            foreach (var section in file.Reader.PEHeaders.SectionHeaders)
            {
                Console.WriteLine($"  Section: {section.Name}");
                Console.WriteLine($"    Virtual Address: {section.VirtualAddress}");
                Console.WriteLine($"    Size of Raw Data: {section.SizeOfRawData}");
            }
        }

        //static void PrintAssemblyAttributes(PEFile file)
        //{
        //    Console.WriteLine("Assembly Attributes:-----------------------------------_______________________________-----------------------");

        //    foreach (var attributeHandle in file.Metadata.CustomAttributes)
        //    {
        //        try
        //        {
        //            var attribute = file.Metadata.GetCustomAttribute(attributeHandle);

        //            // Отримуємо конструктор атрибута
        //            var constructorHandle = attribute.Constructor;
        //            string attributeName;

        //            if (constructorHandle.Kind == HandleKind.MemberReference)
        //            {
        //                // Якщо це MemberReference, отримуємо ім'я типу через MemberReference
        //                var constructor = file.Metadata.GetMemberReference((MemberReferenceHandle)constructorHandle);
        //                var declaringTypeHandle = constructor.Parent;

        //                if (declaringTypeHandle.Kind == HandleKind.TypeReference)
        //                {
        //                    var typeReference = file.Metadata.GetTypeReference((TypeReferenceHandle)declaringTypeHandle);
        //                    attributeName = file.Metadata.GetString(typeReference.Name);
        //                }
        //                else
        //                {
        //                    attributeName = "UnknownAttribute";
        //                }
        //            }
        //            else if (constructorHandle.Kind == HandleKind.MethodDefinition)
        //            {
        //                // Якщо це MethodDefinition, отримуємо ім'я через MethodDefinition
        //                var methodDefinition = file.Metadata.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
        //                var declaringTypeHandle = methodDefinition.GetDeclaringType();

        //                var typeDefinition = file.Metadata.GetTypeDefinition(declaringTypeHandle);
        //                attributeName = file.Metadata.GetString(typeDefinition.Name);
        //            }
        //            else
        //            {
        //                attributeName = "UnknownAttribute";
        //            }

        //            Console.WriteLine($"[{attributeName}]");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error reading attribute: {ex.Message}");
        //        }
        //    }
        //}

        static void PrintAssemblyAttributes(PEFile file)
        {
            Console.WriteLine("Assembly Attributes:");
            foreach (var attributeHandle in file.Metadata.CustomAttributes)
            {
                try
                {
                    var attribute = file.Metadata.GetCustomAttribute(attributeHandle);

                    string attributeName = GetAttributeName(file, attribute.Constructor);

                    Console.Write($"[{attributeName}");

                    if (!attribute.Value.IsNil)
                    {
                        var reader = file.Metadata.GetBlobReader(attribute.Value);
                        var arguments = ReadAttributeArguments(reader);
                        if (!string.IsNullOrEmpty(arguments))
                        {
                            Console.Write($"({arguments})");
                        }
                    }

                    Console.WriteLine("]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading attribute: {ex.Message}");
                }
            }
        }

        static string GetAttributeName(PEFile file, EntityHandle constructorHandle)
        {
            if (constructorHandle.Kind == HandleKind.MemberReference)
            {
                var constructor = file.Metadata.GetMemberReference((MemberReferenceHandle)constructorHandle);
                var declaringTypeHandle = constructor.Parent;

                if (declaringTypeHandle.Kind == HandleKind.TypeReference)
                {
                    var typeReference = file.Metadata.GetTypeReference((TypeReferenceHandle)declaringTypeHandle);
                    return file.Metadata.GetString(typeReference.Name);
                }
            }
            else if (constructorHandle.Kind == HandleKind.MethodDefinition)
            {
                var methodDefinition = file.Metadata.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
                var declaringTypeHandle = methodDefinition.GetDeclaringType();

                var typeDefinition = file.Metadata.GetTypeDefinition(declaringTypeHandle);
                return file.Metadata.GetString(typeDefinition.Name);
            }

            return "UnknownAttribute";
        }

        static string ReadAttributeArguments(BlobReader reader)
        {
            var sb = new StringBuilder();
            try
            {
                reader.ReadCompressedInteger(); // Пропускаємо префікс (тип даних)

                while (reader.RemainingBytes > 0)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    // Читаємо тип аргументу
                    var value = ReadArgumentValue(reader);
                    sb.Append(value);
                }
            }
            catch
            {
                return string.Empty;
            }
            return sb.ToString();
        }

        static object ReadArgumentValue(BlobReader reader)
        {
            try
            {
                // Підтримка чисел, рядків, булевих значень та інших простих типів
                switch (reader.ReadByte())
                {
                    case 0x02: // Boolean
                        return reader.ReadBoolean();
                    case 0x04: // Int32
                        return reader.ReadInt32();
                    case 0x08: // Int64
                        return reader.ReadInt64();
                    case 0x0E: // String
                        return $"\"{reader.ReadSerializedString()}\"";
                    default:
                        return "UnknownType";
                }
            }
            catch
            {
                return "InvalidValue";
            }
        }

        // Метод для читання значень аргументів
        static string ReadFixedArgument(BlobReader reader, PEFile file)
        {
            try
            {
                // Приклад читання рядків
                if (reader.RemainingBytes > 0)
                {
                    return reader.ReadSerializedString();
                }
            }
            catch
            {
                // Якщо значення неможливо прочитати
            }
            return "Unknown";
        }


        static void PrintAssemblyDependencies(PEFile file)
        {
            var references = file.Metadata.AssemblyReferences;

            Console.WriteLine("Assembly Dependencies:");
            foreach (var reference in references)
            {
                var hk = reference.ToString();
                //Console.WriteLine($"{reference.Name} (Version: {reference.Version})");
            }
        }

        static void GenerateHeaderForType(ITypeDefinition type)
        {
            Console.WriteLine($"// Тип: {type.FullName}");
            Console.WriteLine($"{type.Accessibility.ToString().ToLower()} class {type.Name}");

            foreach (var field in type.Fields)
            {
                Console.WriteLine($"    {field.Accessibility.ToString().ToLower()} {field.Type.FullName} {field.Name};");
            }

            foreach (var property in type.Properties)
            {
                Console.WriteLine($"    {property.Accessibility.ToString().ToLower()} {property.ReturnType.FullName} {property.Name} {{ get; set; }}");
            }

            foreach (var method in type.Methods)
            {
                Console.Write($"    {method.Accessibility.ToString().ToLower()} {method.ReturnType.FullName} {method.Name}(");
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var parameter = method.Parameters[i];
                    Console.Write($"{parameter.Type.FullName} {parameter.Name}");
                    if (i < method.Parameters.Count - 1)
                        Console.Write(", ");
                }
                Console.WriteLine(");");
            }
            Console.WriteLine();
        }

        static void DecompileClassesAndMethods(string assemblyPath)
        {
            using (var peFile = new PEFile(assemblyPath))
            {
                var resolver = new UniversalAssemblyResolver(assemblyPath, false, null);
                var typeSystem = new DecompilerTypeSystem(peFile, resolver);
                var settings = new DecompilerSettings { ThrowOnAssemblyResolveErrors = false };
                var decompiler = new CSharpDecompiler(peFile, resolver, settings);

                foreach (var type in typeSystem.MainModule.TypeDefinitions)
                {
                    Console.WriteLine($"// Decompiled Type: {type.FullName}");
                    var typeCode = decompiler.DecompileType(type.FullTypeName);
                    //Console.WriteLine(typeCode);
                }
            }
        }
    }
    //public static class GetInfoViaIlSpy
    //{
    //    public static void ProcessAssemblies(string targetPath, string assemblyPath)
    //    {
    //        try
    //        {
    //            IEnumerable<string> files;

    //            if (string.IsNullOrEmpty(assemblyPath))
    //            {
    //                // Process all files in the directory
    //                files = Directory.GetFiles(targetPath, "*.*", SearchOption.TopDirectoryOnly)
    //                                 .Where(file => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
    //                                                file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
    //            }
    //            else
    //            {
    //                // Process the specific file
    //                files = new List<string> { assemblyPath };
    //            }

    //            foreach (var file in files)
    //            {
    //                using (var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
    //                {
    //                    Console.WriteLine($"Processing: {file}");
    //                    string assemblyInfo = GetAssemblyInfoViaCmd(file);
    //                    Console.WriteLine(assemblyInfo);
    //                }
    //            }
    //        }
    //        catch (UnauthorizedAccessException ex)
    //        {
    //            Console.WriteLine($"Access to the path '{targetPath}' is denied: {ex.Message}");
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error processing files in directory '{targetPath}': {ex.Message}");
    //        }
    //    }

    //    public static string GetAssemblyInfoViaCmd(string filePath)
    //    {
    //        using (var process = new Process())
    //        {
    //            process.StartInfo = new ProcessStartInfo
    //            {
    //                FileName = "ilspycmd", // Ensure ilspycmd is available in PATH
    //                Arguments = $"\"{filePath}\"",
    //                RedirectStandardOutput = true,
    //                RedirectStandardError = true,
    //                UseShellExecute = false,
    //                CreateNoWindow = true
    //            };

    //            process.Start();

    //            string output = process.StandardOutput.ReadToEnd();
    //            string error = process.StandardError.ReadToEnd();

    //            process.WaitForExit();

    //            if (process.ExitCode != 0)
    //            {
    //                throw new Exception($"ilspycmd failed with error: {error}");
    //            }

    //            return output;
    //        }
    //    }
    //}
//}
}
