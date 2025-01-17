using System;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace DeCompiler
{
    static class Program
    {
        public static void Main(string[] args)
        {
            string assemblyPath = "D:\\Work\\Elements\\Bin\\Elements.SQLite.dll";

            ProcessAssembly(assemblyPath);
            Console.ReadLine();
        }

        static void ProcessAssembly(string assemblyPath)
        {
            using (var file = new PEFile(assemblyPath))
            {
                PrintHeaders(file);

                var resolver = new UniversalAssemblyResolver(assemblyPath, false, null);
                var typeSystem = new DecompilerTypeSystem(file, resolver);

                foreach (var type in typeSystem.MainModule.TypeDefinitions)
                {
                    GenerateHeaderForType(type);
                }

                Console.WriteLine(GetAssemblyContentsAsString(typeSystem));
            }
        }

        static void PrintHeaders(PEFile file)
        {
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine("Assembly Headers:");
            Console.WriteLine($"File Name: {file.FileName}");
            Console.WriteLine($"Metadata Version: {file.Metadata.MetadataVersion}{file.}");
            Console.WriteLine($"Machine: {file.Reader.PEHeaders.CoffHeader.Machine}");
            Console.WriteLine($"Time Stamp: {file.Reader.PEHeaders.PEHeader.BaseOfData}");
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine("Sections:");
            foreach (var section in file.Reader.PEHeaders.SectionHeaders)
            {
                Console.WriteLine($"  Section: {section.Name}");
                Console.WriteLine($"    Virtual Address: {section.VirtualAddress}");
                Console.WriteLine($"    Size of Raw Data: {section.SizeOfRawData}");
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

        static string GetAssemblyContentsAsString(DecompilerTypeSystem typeSystem)
        {
            var sb = new StringBuilder();
            foreach (var type in typeSystem.MainModule.TypeDefinitions)
            {
                sb.AppendLine($"// Тип: {type.FullName}");
                sb.AppendLine($"{type.Accessibility.ToString().ToLower()} class {type.Name}");

                foreach (var field in type.Fields)
                {
                    sb.AppendLine($"    {field.Accessibility.ToString().ToLower()} {field.Type.FullName} {field.Name};");
                }

                foreach (var property in type.Properties)
                {
                    sb.AppendLine($"    {property.Accessibility.ToString().ToLower()} {property.ReturnType.FullName} {property.Name} {{ get; set; }}");
                }

                foreach (var method in type.Methods)
                {
                    sb.Append($"    {method.Accessibility.ToString().ToLower()} {method.ReturnType.FullName} {method.Name}(");
                    for (int i = 0; i < method.Parameters.Count; i++)
                    {
                        var parameter = method.Parameters[i];
                        sb.Append($"{parameter.Type.FullName} {parameter.Name}");
                        if (i < method.Parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.AppendLine(");");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
