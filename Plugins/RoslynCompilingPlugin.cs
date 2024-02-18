using CSharpInterpriterSemanticKernel.Options;
using CSharpInterpriterSemanticKernel.Services;
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
        private readonly DataRetrievalService _dataRetrievalService;
        public RoslynCompilingPlugin(DependenciesOptions dependenciesOptions, DataRetrievalService dataRetrievalService)
        {
            _dependenciesOptions = dependenciesOptions;
            _dataRetrievalService = dataRetrievalService;
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

                    var errorsText = string.Join("\n", errors);
                    Console.WriteLine("\nCode Interpreter: Compilation errors detected.\n" + errorsText);

                    
                    return "Compilation Errors: " + errorsText + ".\n";
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

                                resultText = "Console output: " + sw.ToString();

                                var resultTextLowwer = resultText.ToLower();

                                if (resultTextLowwer.Contains("error") || resultTextLowwer.Contains("failed") || resultTextLowwer.Contains("exception"))
                                {
                                    resultText += "\n" + await _dataRetrievalService.RetrieveDataFromQDrantAsync(resultText, 0.8);
                                }
                                else
                                {
                                    resultText = "Preserve all useful information from the console output.\n" + resultText;
                                }
                            }
                            catch (Exception ex)
                            {
                                resultText = "Execution Error: " + ex.Message;
                                resultText += "\n" + await _dataRetrievalService.RetrieveDataFromQDrantAsync(ex.Message, 0.8);
                            }
                            finally
                            {
                                Console.SetOut(originalOut);
                            }

                            Console.WriteLine("Interpriter C#: " + resultText);
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
