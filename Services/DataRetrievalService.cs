using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpInterpriterSemanticKernel.Services
{
    public class DataRetrievalService
    {
        private readonly IMemoryDb _memoryDb;
        private readonly ITextEmbeddingGenerator _embeddingGenerator;
        private readonly string _indexName = "your-index-name1";

        public DataRetrievalService(IMemoryDb memoryDb, ITextEmbeddingGenerator textEmbeddingGenerator)
        {
            _memoryDb = memoryDb;
            _embeddingGenerator = textEmbeddingGenerator;
        }

        public async Task<string> RetrieveDataFromQDrantAsync(string query, double minRelevance = 0.85)
        {
            Console.WriteLine($"Initiating search for: {query}");

            StringBuilder sb = new StringBuilder();
            try
            {

                var similarRecords = _memoryDb.GetSimilarListAsync(_indexName, query, minRelevance: minRelevance, withEmbeddings: true, limit: 30); // Assume threshold to filter relevant results
                await foreach (var (record, relevance) in similarRecords)
                {
                    if (record.Payload.TryGetValue("dateTime", out object dateTime))
                    {
                        var text = dateTime.ToString();
                        Console.WriteLine($"Found: {text}");
                        sb.AppendLine($"Information relevance by date: {text}");
                    }

                    // Extract and present the summary information
                    if (record.Payload.TryGetValue("summary", out object summary))
                    {
                        var text = summary.ToString();
                        Console.WriteLine($"Found: {text}");
                        sb.AppendLine("Summary: " + text);
                    }

                    // Extract and present the data information
                    if (record.Payload.TryGetValue("data", out object data))
                    {
                        var text = data.ToString();
                        Console.WriteLine($"Found: {text}");
                        sb.AppendLine("Data (Information): " + text);
                    }

                    sb.AppendLine("");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while searching in QDrant: {ex.Message}");
                return $"Failed to retrieve data for '{query}'. Error: {ex.Message}";
            }

            return sb.Length == 0 ?
                $"No relevant data found for '{query}' in Knowledge. Please attempt to retrieve the information using the C# Interpreter."
                :
                $"Current Date: {DateTime.Now}.\nFor the query '{query}', the following information was found in Knowledge: {sb}.";
        }

        public async Task<bool> ArchiveInformationAsync(
            DateTime dateTime,
            string data,
            string summary)
        {
            try
            {
                var summaryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(summary);
                var existingRecords = _memoryDb.GetSimilarListAsync(_indexName, summary, minRelevance: 1, withEmbeddings: true); // Adjust threshold based on your accuracy needs

                if (await existingRecords.AnyAsync()) // Check if similar record already exists
                {
                    Console.WriteLine($"Similar data already exists. Skipping save.");
                    return true; // Avoid duplication
                }

                MemoryRecord record = new MemoryRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    Vector = summaryEmbedding,
                    Payload = new Dictionary<string, object>
                    {
                        { "data", data },
                        { "summary", summary },
                        { "dateTime", dateTime }
                    }
                };

                await _memoryDb.UpsertAsync(_indexName, record);
                Console.WriteLine($"Data saved successfully: {data} - {summary}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving data to QDrant: {ex.Message}");
                return false;
            }
        }
    }
}
