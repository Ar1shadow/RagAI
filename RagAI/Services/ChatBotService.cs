using System.Diagnostics;
using System.Text;
using Spectre.Console;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Context;
using RagAI.Configuration;
namespace RagAI.Services;
/// <summary>
/// Cette service sert a creer les façons de chat
/// </summary>
/// 
public class ChatBotService
{
    private readonly ILogger<ChatBotService> _logger;
    private  readonly ModelParams _llmParas;
    private readonly KernelMemoryService _kernelMemoryService;
    private readonly ChatSessionService _chatSessionService;
    
    /// <summary>
    /// configure the chat model|||
    ///Note the parameters are the path of chat model stored in the PathsConfiguration 
    /// </summary>
    /// 
    public ChatBotService(
        KernelMemoryService kernelMemoryService, 
        ILogger<ChatBotService> logger,
        ChatSessionService chatSessionService)
    {
        _logger = logger;
        _kernelMemoryService = kernelMemoryService;
        _chatSessionService = chatSessionService;
        //configuration of Chat model
        _llmParas = new ModelParams(_kernelMemoryService.ModelPath)
        {
            ContextSize = 1024,//the longest length of chat as memory
            GpuLayerCount = 4, //layers to offload to GPU
        };
    }

    public async Task StartChat()
    {
        Dictionary<string, Func<Task>> menu = new()
        {
            { "Chat Mode 1 stateless", StartChatExample1 },
            { "Chat Mode 2 with history ", StartChatExample2 },
        };
       
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Please choose[green] an mode[/] to run: ")
                .AddChoices(menu.Keys));

        if (menu.TryGetValue(choice, out var example))
        {
            AnsiConsole.Write(new Rule(choice));
            await example();
        }
    }

    private async Task StartChatExample1()
    {
        var kmContext = new RequestContext();
        // Use a custom template for facts
        kmContext.SetArg("custom_rag_fact_template_str", "=== Last update: {{$meta[last_update]}} ===\n{{$content}}\n");
        kmContext.SetArg("sd", "");
        // Use a custom RAG prompt
        kmContext.SetArg("custom_rag_prompt_str", """
                                                  Facts:
                                                  {{$facts}}
                                                  ======
                                                  Given only the timestamped facts above, provide a concise answer, include the relevant dates in brackets.
                                                  If you don't have sufficient information, show me all the facts, then reply with '{{$notFound}}',
                                                  Question: {{$input}}
                                                  Answer:
                                                  """);

        // Interactive Loop
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Session started. Type 'exit' to quit.\nUser: ");
        Console.ForegroundColor = ConsoleColor.Green;
        var userInput = Console.ReadLine() ?? "";


        while (userInput.ToLower() != "exit")
        {

            await _kernelMemoryService.GenerateResponse(userInput, minRelevance: 0);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("User: ");
            userInput = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrEmpty(userInput)) return;



        }
    }

    private async Task StartChatExample2()
    {
        var (session, history) = _chatSessionService.CreatSession(_llmParas);
       
        
        // initialization of inference params
        var inferenceParas = new InferenceParams()
        {
            MaxTokens = -1,
            AntiPrompts = new List<string> { "User:" },
            SamplingPipeline = new DefaultSamplingPipeline()
        };
        
        // Interactive Loop
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Session started. Type 'exit' to quit.\nUser: ");
        var userInput = Console.ReadLine() ?? "";

        while (userInput.ToLower() != "exit")
        {
            //_logger.LogInformation("Searching for relevance in memory");
            //Console.ForegroundColor = ConsoleColor.Gray;
            try
            {
                var longTermMemory = await _kernelMemoryService.GetMemory(userInput);
                history.Messages[0].Content =
                    $"{PromptTemplate.RagPrompt}\n\ninformation in memory :\n{longTermMemory}";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error searching for relevance in memory");
            }

            
            //Console.WriteLine($"----------------------\nrecall form memory\n{longTermMemory}\n----------------------------");
           
           // add memory 
           //session.History.Messages.Add(new ChatHistory.Message(AuthorRole.System,longTermMemory));
           //history.AddMessage(AuthorRole.System, longTermMemory);
           
           //generate response, stream the response
           Console.ForegroundColor = ConsoleColor.Green;
           //Console.Write("Assistant:");
           var response = new StringBuilder();
           var sw = Stopwatch.StartNew();
           
           await foreach (
                var text in session.ChatAsync(
                    new ChatHistory.Message(AuthorRole.User,userInput), inferenceParas))
            {
                Console.Write(text);
                //Console.Write($"[{text}]"); 
                response.Append(text);
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"response generated in {sw.Elapsed} \n");
            //session.AddAssistantMessage(response.ToString());
            //history.AddMessage(AuthorRole.Assistant, response.ToString());
           
            
           Console.ForegroundColor = ConsoleColor.Yellow; 
           //Console.Write("User: ");
           userInput = Console.ReadLine() ?? string.Empty; 
           if (string.IsNullOrEmpty(userInput)) _logger.LogInformation("User input is empty") ;
        }
        //save the chat history
        Console.WriteLine("\nSave chat history? (y/n)");
        if (Console.ReadLine() == "y")
        {
            session.SaveSession(Paths.ChatHistory);
            Console.WriteLine("\nChat history saved.");
        }
    }
}