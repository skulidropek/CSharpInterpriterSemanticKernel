using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.Extensions.Logging;
using CSharpInterpriterSemanticKernel.Plugins;

namespace CSharpInterpriterSemanticKernel
{
    internal class KernelASK
    {
        //public string ASK(string query)
        //{
        //    var openAIConfig = new OpenAIConfig
        //    {
        //        TextModel = "gpt-4-1106-preview",
        //        EmbeddingModel = "text-embedding-ada-002",
        //        APIKey = Environment.GetEnvironmentVariable("openai-api-key"),
        //    };

        //    // Валидация конфигурации
        //    openAIConfig.Validate();

        //    var builder = Kernel.CreateBuilder()
        //           .AddOpenAIChatCompletion(
        //                "gpt-4-1106-preview",                  // OpenAI Model name
        //                    Environment.GetEnvironmentVariable("openai-api-key"));
                

        //    // Регистрация OpenAITextEmbeddingGenerator с использованием конфигурации
        //    builder.Services.AddScoped<ITextEmbeddingGenerator>((serviceProvider) =>
        //    {
        //        var httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        //        return new OpenAITextEmbeddingGenerator(openAIConfig, null, serviceProvider.GetService<ILogger<OpenAITextEmbeddingGenerator>>(), httpClient);
        //    });

        //    builder.Services.AddQdrantAsMemoryDb(new QdrantConfig()
        //    {
        //        Endpoint = "http://localhost:6333/", // Используйте актуальный адрес вашего сервера Qdrant
        //        APIKey = "" // Оставьте пустым, если аутентификация не требуется
        //    });

        //    builder.Plugins.AddFromType<QDrantPlugin>();

        //    var kernel = builder.Build();

        //}
    }
}
