using LLama.Native;
using Spectre.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RagAI.Configuration;
using RagAI.Services;

namespace RagAI;

public class Runner
{
    
    public async Task Run()
    {
        // Configure logging. Change this to `true` to see log messages from llama.cpp
        var showLLamaCppLogs = false;
        NativeLibraryConfig
            .All
            .WithLogCallback((level, message) =>
            {
                if (showLLamaCppLogs)
                    Console.WriteLine($"[llama {level}]: {message.TrimEnd('\n')}");
            });
            
            
        //Console.WriteLine(Paths.BaseDirectory);
        //Console.WriteLine(Paths.AppSettingsPath);
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
            .AddSingleton<ChatSessionService>()
            .BuildServiceProvider();
            
        var kernelMemoryService = serviceProvider.GetService<KernelMemoryService>();
        await kernelMemoryService.IngestDocuments(Paths.DocsPath);
                
        //configure  models
        var chatBot = serviceProvider.GetService<ChatBotService>();
        await chatBot.StartChat();



    }
    
}