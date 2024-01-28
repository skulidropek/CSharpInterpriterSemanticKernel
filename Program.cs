using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using CSharpInterpriterSemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using CSharpInterpriterSemanticKernel.Options;

var builder = Kernel.CreateBuilder();

// Alternative using OpenAI
builder.AddOpenAIChatCompletion(
      "gpt-3.5-turbo",//"gpt-4-1106-preview", // Youre model
         Environment.GetEnvironmentVariable("openai-api-key"));

builder.Services.AddSingleton(s => new DependenciesOptions()
{
    Referense = new List<string>()
    {
        "C:\\Program Files\\dotnet\\packs\\Microsoft.NETCore.App.Ref\\8.0.1\\ref\\net8.0",
        "C:\\Users\\legov\\.nuget\\packages\\newtonsoft.json\\13.0.3\\lib\\net6.0",
        "C:\\Users\\legov\\.nuget\\packages\\htmlagilitypack\\1.11.57\\lib\\NetCore45",
        "C:\\Users\\legov\\.nuget\\packages\\selenium.webdriver\\4.15.0\\lib\\netstandard2.0",
    }
});

builder.Plugins.AddFromType<RoslynCompilingPlugin>();

var kernel = builder.Build();

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Create the chat history
ChatHistory chatMessages = new ChatHistory();
chatMessages.AddUserMessage($@"
    Hello! You CSharpGPT, your virtual assistant specializing in C#.
    I automatically compile and execute C# code, enhancing our interaction with real-time code solutions.
    Alongside C# expertise, You equipped with Selenium and Google Chrome's WebDriver for advanced browser automation tasks.
    You here to assist with C# coding, debugging, learning, or automating web interactions.
    Thanks to the RoslynCompilingPlugin, You seamlessly compile and execute C# code during our conversation, always using a C# interpreter.");

while (true)
{
    // Get user input
    System.Console.Write("User > ");

    var query = Console.ReadLine()!;

    chatMessages.AddUserMessage(query);

    // Get the chat completions
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        chatMessages,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Stream the results
    string fullMessage = "";

    await foreach (var content in result)
    {
        if (content.Role.HasValue)
        {
            System.Console.Write("\nAssistant > ");
        }
        System.Console.Write(content.Content);
        fullMessage += content.Content;
    }

    System.Console.WriteLine();
    chatMessages.AddAssistantMessage(fullMessage);
}