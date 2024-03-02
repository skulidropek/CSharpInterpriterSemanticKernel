using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using CSharpInterpriterSemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using CSharpInterpriterSemanticKernel.Options;
using Microsoft.KernelMemory;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage;
using CSharpInterpriterSemanticKernel.Services;
using HtmlAgilityPack;
using System.Globalization;

var builder = Kernel.CreateBuilder();

// Alternative using OpenAI
builder.AddOpenAIChatCompletion(
      "gpt-4-1106-preview",//"gpt-4-1106-preview", // Youre model
         Environment.GetEnvironmentVariable("openai-api-key"));

var openAIConfig = new OpenAIConfig
{
    TextModel = "gpt-4-1106-preview",
    EmbeddingModel = "text-embedding-ada-002",
    APIKey = Environment.GetEnvironmentVariable("openai-api-key"),
};

// Валидация конфигурации
openAIConfig.Validate();

builder.Services.AddSingleton<DataRetrievalService>();

// Регистрация OpenAITextEmbeddingGenerator с использованием конфигурации
builder.Services.AddScoped<ITextEmbeddingGenerator>((serviceProvider) =>
{
    var httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
    return new OpenAITextEmbeddingGenerator(openAIConfig, null, serviceProvider.GetService<ILogger<OpenAITextEmbeddingGenerator>>(), httpClient);
});

builder.Services.AddQdrantAsMemoryDb(new QdrantConfig()
{
    Endpoint = "http://localhost:6333/", // Используйте актуальный адрес вашего сервера Qdrant
    APIKey = "" // Оставьте пустым, если аутентификация не требуется
});

builder.Services.AddSingleton(s => new DependenciesOptions()
{
    Referense = new List<string>()
    {
        "C:\\Program Files\\dotnet\\packs\\Microsoft.NETCore.App.Ref\\8.0.1\\ref\\net8.0",
        "C:\\Users\\legov\\.nuget\\packages\\htmlagilitypack\\1.11.59\\lib\\netstandard2.0\\",
        "C:\\Users\\legov\\.nuget\\packages\\newtonsoft.json\\13.0.3\\lib\\net6.0",
    }
});

builder.Plugins.AddFromType<RoslynCompilingPlugin>();
builder.Plugins.AddFromType<QDrantPlugin>();

var kernel = builder.Build();

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var qdrantMemory = kernel.Services.GetRequiredService<IMemoryDb>();

var dataRetrievalService = kernel.Services.GetRequiredService<DataRetrievalService>();

string indexName = "your-index-name1";

// Попытка создания индекса с размером вектора 1,536
try
{
    await qdrantMemory.CreateIndexAsync(indexName, 1536);
    Console.WriteLine($"Индекс '{indexName}' успешно создан.");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при создании индекса: {ex.Message}");
}

// Create the chat history
ChatHistory chatMessages = new ChatHistory();

while (true)
{
    // Get user input
    System.Console.Write("User > ");

    var query = Console.ReadLine()!;

    chatMessages.AddUserMessage(query);

    // Get the chat completions
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        MaxTokens = 4096,
        Temperature = 0,
        ChatSystemPrompt = $@"Current Date: {DateTime.Now}
This ChatGPT system is specifically designed to leverage an external knowledge database through the QDrantPlugin, focusing on generating SEO-optimized content. It excels at synthesizing data into concise summaries, interpreting natural language queries accurately, and extracting key insights from diverse sources, ensuring the integration of relevant keywords and phrases. The system is configured to MINIMIZE THE USE OF STOP WORDS and REDUCE 'WATERINESS' in the text, maintaining an optimal keyword density to avoid 'keyword stuffing' and ensuring grammatical correctness.

Key Operational Principles for SEO-Optimized Text Generation:
- INTEGRATE RELEVANT KEYWORDS AND PHRASES thoughtfully throughout the content to enhance its visibility and relevance in search engine results.
- MINIMIZE THE USE OF STOP WORDS and unnecessary fillers to ensure content is concise and to the point.
- STRUCTURE CONTENT with headings, subheadings, and lists to improve readability and facilitate scanning by both users and search engine algorithms.
- ENSURE GRAMMATICAL CORRECTNESS and use professional, industry-specific terminology to enhance content quality and professionalism.
- MONITOR KEYWORD DENSITY to avoid 'keyword stuffing', aiming for a natural integration of keywords that supports fluid reading and relevance.

The system's primary goal is to ensure all generated content is not only informed by ACCURATE, CURRENT INFORMATION but is also OPTIMIZED FOR SEARCH ENGINES by CONSTANTLY EXPANDING ITS KNOWLEDGE BASE with SEO best practices. This approach ensures the system remains agile, relevant, and capable of providing high-quality, SEO-optimized responses.

Instructions for System Use for SEO-Optimized Content:
1. ALWAYS REFER TO THE KNOWLEDGE DATABASE for retrieving information, ensuring it's current and relevant, and confirming data accuracy with an SEO perspective in mind.
2. ARCHIVE SEO-RELEVANT INFORMATION encountered during interactions into the knowledge database to enhance and update the system's capabilities for generating SEO-optimized content.
3. ENGAGE THE C# INTERPRETER for coding solutions when information needs to be processed or manipulated uniquely for SEO purposes, further enhancing the system's problem-solving capabilities in the context of SEO."

        //        ChatSystemPrompt = $@"Current Date: {DateTime.Now}
        //This ChatGPT system is enhanced with a special capability to ALWAYS RETAIN AND UTILIZE useful information by integrating with an external knowledge database through the QDrantPlugin. The system excels in synthesizing data into concise summaries, interpreting natural language queries with high precision, and extracting fundamental insights from a variety of sources. It's designed to KEEP ITS KNOWLEDGE CURRENT AND RELEVANT by ARCHIVING NEW INFORMATION INTO THE KNOWLEDGE DATABASE, ENSURING THE DATA REMAINS FRESH for temporal queries or up-to-date verification.

        //When interacting with users, the system prioritizes RETRIEVING AND ARCHIVING INFORMATION beyond its immediate knowledge base. It dynamically updates its understanding with the latest data, ensuring responses are informed by the MOST CURRENT INFORMATION AVAILABLE. The system uses an advanced text embedding generator to IDENTIFY SIMILAR EXISTING RECORDS before saving new data, preventing the duplication of information and maintaining the integrity of the database.

        //The goal is to provide users with accurate, up-to-date responses by CONSTANTLY EXPANDING THE SYSTEM'S KNOWLEDGE BASE. This approach facilitates maintaining data freshness and ensures the system's responses are always grounded in the latest, most relevant information.

        //ВСЕГО ОБРАЩАЙСЯ К knowledge.
        //СОХРАНЯЙ ПОЛЕЗНУЮ ИНФОРМАЦИЮ В knowledge.
        //СОХРАНЯЙ АБСОЛЮТНО ВСЮ НОВУЮ ИНФОРМАЦИЮ В knowledge.
        //"


        //@"
        //This chat introduces you to a highly intelligent AI assistant, CSharpGPT, your ultimate guide for C# programming. Enhanced with specialized plugins for dynamic knowledge retrieval and real-time code execution, it features the QDrantPlugin for leveraging a QDrant-based knowledge base. This enables precise information retrieval and synthesis through advanced text embeddings, ensuring responses are both relevant and accurate.

        //The assistant also boasts capabilities for automatic C# code compilation and execution via the RoslynCompilingPlugin. Trained on diverse datasets up to 2021 and operating without real-time internet access, CSharpGPT relies on its extensive, internally curated knowledge base. It is prepared to assist with C# coding, debugging, learning, or enhancing your understanding of programming concepts, making it an invaluable tool for exploring, learning, and solving complex queries.

        //With your precise questions, you can unlock the most accurate and informative responses. Let's dive into this sophisticated AI capability together.
        //"
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