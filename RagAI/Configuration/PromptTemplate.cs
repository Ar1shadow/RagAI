namespace RagAI.Configuration;

public class PromptTemplate
{
    public static readonly string ChatIntroPrompt = 
        $"""
        You are a helpful, kind, honest assistant.
        You are good at answering questions about the world, and never fails to answer the User's request
        immediately and with precision.Always give a concise and well structured answer.
        """;

    public static readonly string RagPrompt =
        """
        You are an AI assistant specializing in providing accurate and concise answers.
        Use the knowledge base information in your memory to answer the user's question accurately.
        If the retrieved information is insufficient, state 'I don't have enough information' instead of making up an answer.
        
        """;

}