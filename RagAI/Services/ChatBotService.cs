using DocumentFormat.OpenXml.Spreadsheet;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory.Context;
using RagAI.Configuration;
namespace RagAI.Services;

public class ChatBotService
{
    private readonly ILogger<ChatBotService> _logger;
    private  readonly LLamaWeights _llm;
    private  readonly ModelParams _llmParas;
    private readonly KernelMemoryService _kernelMemoryService;
    
    /// <summary>
    /// configure the chat model|||
    ///Note the parameters are the path of chat model stored in the PathsConfiguration 
    /// </summary>
    /// 
    public ChatBotService(KernelMemoryService kernelMemoryService, ILogger<ChatBotService> logger)
    {
        //configuration of Chat model
        _llmParas = new ModelParams(Paths.LlmPath)
        {
             ContextSize = 1024,//the longest length of chat as memory
            GpuLayerCount = 4, //layers to offload to GPU
        };
        _llm = LLamaWeights.LoadFromFile(_llmParas);
        _logger = logger;
        _kernelMemoryService = kernelMemoryService;
    }
    
    
    public async Task StartChat()
    {
        //Initialization of Chat session and Chat state
        /*
        using var context = _llm.CreateContext(_llmParas);
        var history = new ChatHistory();
        var inferenceParas = new InferenceParams()
        {
            MaxTokens = 2048,
            AntiPrompts = new List<string> { "User:" },
            SamplingPipeline = new DefaultSamplingPipeline()
            
        };
        var executor = new InteractiveExecutor(context);
        history.AddMessage(AuthorRole.System, PromptTemplate.ChatIntroPrompt);
        
        var session = new ChatSession(executor, history);
        
        */
        
        var kmContext = new RequestContext();
        // use a custom template for facts
        kmContext.SetArg("custom_rag_fact_template", "{fact_name}: {fact_value}");
        // use a custom RAG prompt
        
        // Interactive Loop
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Session started. Type 'exit' to quit.\nUser: ");
        Console.ForegroundColor = ConsoleColor.Green;
        var userInput = Console.ReadLine() ?? "";
        //history.AddMessage(AuthorRole.User, userInput);
        
        while (userInput.ToLower() != "exit")
        {
            
            await _kernelMemoryService.GenerateResponse(userInput);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("User: ");
            userInput = Console.ReadLine()??string.Empty;
            if (string.IsNullOrEmpty(userInput)) return;
            
                
            
            //
            /*
            await _kernelMemoryService.GenerateResponse(userInput);
            Console.ForegroundColor = ConsoleColor.Green;
            userInput = Console.ReadLine() ?? "";
            */

            //string response = "";
            /* await foreach (
                 var text in session.ChatAsync(
                                 new ChatHistory.Message(AuthorRole.User,userInput),
                                 inferenceParas))
             {
                 Console.ForegroundColor = ConsoleColor.White;
                 Console.Write(text);
                 //response += text;
             }
             Console.ForegroundColor = ConsoleColor.Green;
             userInput = Console.ReadLine() ?? "";
             //history.AddMessage(AuthorRole.Assistant, response);
            */
        }
    }
}