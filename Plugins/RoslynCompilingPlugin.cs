using CSharpInterpriterSemanticKernel.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpInterpriterSemanticKernel.Plugins
{
    internal class RoslynCompilingPlugin
    {
        private readonly DependenciesOptions _dependenciesOptions;
        public RoslynCompilingPlugin(DependenciesOptions dependenciesOptions)
        {
            _dependenciesOptions = dependenciesOptions;
        }

        [KernelFunction]
        [Description("Executes the provided C# code and returns the output. The code is compiled and executed dynamically. This method is capable of handling any valid C# code, including asynchronous operations and HTTP requests. Ensure the code includes a 'Main' method or an entry point for execution.\"")]
        public async Task<string> ExecuteCodeAsync(
            [Description("The C# code to be executed. The code should be a complete snippet that can be compiled and executed, including necessary class and method definitions.")] string code
            )
        {
            Console.WriteLine("Code Interpreter: Starting execution of code.\n" + code);

            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            List<MetadataReference> references = new List<MetadataReference>();

            foreach(var dependencie in _dependenciesOptions.Referense)
                references.AddRange(LoadReferencesFromDirectory(dependencie));

            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    // Сбор и возврат сообщений об ошибках
                    var errors = result.Diagnostics
                        .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(diag => $"{diag.Id}: {diag.GetMessage()}");
                    Console.WriteLine("\nCode Interpreter: Compilation errors detected.");
                    return "Compilation Errors: " + string.Join("\n", errors);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                foreach (var type in assembly.GetTypes())
                {
                    var method = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        using (var sw = new StringWriter())
                        {
                            var originalOut = Console.Out;

                            Console.SetOut(sw);

                            string resultText = "";

                            try
                            {
                                var parameters = method.GetParameters();

                                object[] parametersArray = new object[parameters.Length];

                                object methodResult = method.Invoke(null, parametersArray);

                                if (methodResult is System.Threading.Tasks.Task)
                                {
                                    await (methodResult as Task);
                                }

                                resultText = "Console result: " + sw.ToString();

                                Console.WriteLine("\nCode Interpreter: Execution completed. " + resultText);
                            }
                            catch (Exception ex)
                            {
                                resultText = "Execution Error: " + ex.Message;
                                Console.WriteLine("\nCode Interpreter: " + resultText);
                            }
                            finally
                            {
                                Console.SetOut(originalOut);
                            }

                            return resultText;
                        }
                    }
                }

                Console.WriteLine("\nCode Interpreter: Main method not found.");
                return "Main method not found";
            }
        }


        private List<MetadataReference> LoadReferencesFromDirectory(string path)
        {
            return Directory.GetFiles(path, "*.dll")
                            .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath) as MetadataReference)
                            .ToList();
        }
    }
}
