using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using RagAI.Configuration;
using UglyToad.PdfPig.Graphics.Operations.PathPainting;


namespace RagAI.Services;

public class ChatSessionService
{
    
    private readonly ILogger<ChatSessionService> _logger;
    private readonly KernelMemoryService _kernelMemoryService;
    private const string DefaultAssistantMessage = "Hello, I'm your personal assistant. How can I help you today?";
    public ChatSession ChatSession { get; set; }

  

    public ChatSessionService(ILogger<ChatSessionService> logger, KernelMemoryService kernelMemoryService)
    {
        this._logger = logger;
        _kernelMemoryService = kernelMemoryService;
    }
    
    public (ChatSession, ChatHistory) CreatSession(ModelParams modelParams)
    {
        var llm = LLamaWeights.LoadFromFile(modelParams);
        var context = llm.CreateContext(modelParams);
        var executor = new InteractiveExecutor(context);
        ChatSession session;
        var history = new ChatHistory();
        session = Directory.Exists(Paths.ChatHistory) && PromptToLoadHistory()
            ? InitializeChatSessionWithHistory(executor)
            : InitializeDefaultChatSession(executor, history);

        return (session, history);
    }
    
    
    bool PromptToLoadHistory()
    {
        Console.WriteLine("\nChat history found, enter \"y\" to load the history(y|n): ");
        return Console.ReadLine()?.Trim().ToLower() == "y";
    }
    
    ChatSession InitializeChatSessionWithHistory(InteractiveExecutor executor)
    {
        var chatSession = new ChatSession(executor);
        try
        {
            chatSession.LoadSession(Paths.ChatHistory);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading chat history");
        }
        chatSession.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            new[] { "Assistant:", "User:", "System:" },
            redundancyLength: 8)); // 避免魔法值
        return chatSession;
    }

    // 方法：创建默认聊天记录并初始化会话
    ChatSession InitializeDefaultChatSession(InteractiveExecutor executor, ChatHistory history)
    {
        CreateDefaultChatHistory(history);
        return new ChatSession(executor, history);
    }

    // 方法：创建默认聊天记录
    void CreateDefaultChatHistory(ChatHistory history)
    {
        history.AddMessage(AuthorRole.System, PromptTemplate.RagPrompt);
        history.AddMessage(AuthorRole.User, "Hello!");
        history.AddMessage(AuthorRole.Assistant, DefaultAssistantMessage);
    }

    
    public void LoadChatSession(ChatSession chatSession, string path)
    {
        if (!Path.Exists(path))
        {
            _logger.LogError("Session path does not exist.");
        }else
            chatSession.LoadSession(path);
    }

    public void SaveChatSession(ChatSession chatSession, string path)
    {
        if (Path.Exists(path))
        {
            _logger.LogWarning("Session overwriting.");
        }
        chatSession.SaveSession(path);
    }
}

