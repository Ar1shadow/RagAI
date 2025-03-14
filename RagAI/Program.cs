using LLama.Common;
using LLama;
using LLama.Sampling;
using LLamaSharp.KernelMemory;  
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RagAI.Configuration;
using RagAI.Services;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;


namespace RagAI
{
    class Program
    {
        static async Task Main(string[] args)
        { 
            Console.WriteLine(Paths.BaseDirectory);
            Console.WriteLine(Paths.AppSettingsPath);
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Paths.BaseDirectory)
                .AddJsonFile(Paths.AppSettingsPath, optional: false)
                .Build();
            // Set up dependency injection
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                
                // Logging
                .AddLogging(configure => 
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(LogLevel.Information);
                })
                .AddSingleton<KernelMemoryService>()
                .AddSingleton<ChatBotService>()
                .BuildServiceProvider();
            
            var kernelMemoryService = serviceProvider.GetService<KernelMemoryService>();
            //kernelMemoryService.Initialize(Paths.LlmName);
            await kernelMemoryService.IngestDocments(Paths.DocsPath);
                
            //configure  models
            var chatBot = serviceProvider.GetService<ChatBotService>();
            await chatBot.StartChat();
          
           






        }
    }
    
}