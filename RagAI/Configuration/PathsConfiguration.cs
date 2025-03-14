namespace RagAI.Configuration
{
    ///<summary>
    /// For store the path needed
    /// </summary>
    public static class Paths
    {
        // Model Name
        public static readonly string LlmName = "Model/DeepSeek-R1-Distill-Llama-8B-Q4_K_M.gguf";
        //public static readonly string LlmName = "Model/Mistral-4B.Q4_K_M.gguf";
        public static readonly string EmbeddingName = "Model/nomic-embed-text-v1.5.Q5_K_M.gguf";

        // // generally, working space is Debug,
        // // so return the path of the directory three levels above the current program’s directory.
        public static readonly string BaseDirectory =
            Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.ToString()
            ?? throw new InvalidOperationException("Unable to determine the base directory.");
        
        // Full path of Model
        public static readonly string LlmPath = Path.Combine(BaseDirectory, LlmName);
        public static readonly string EmbeddingPath = Path.Combine(BaseDirectory, EmbeddingName);
        
        // path of Docs
        public static readonly string DocsPath = Path.Combine(BaseDirectory, "Docs");
      
        //path of save of Chat session
        public static readonly string ChatHistory = Path.Combine(BaseDirectory, "Assets/ChatSession.json");

        //path of appsetting.json
        public static readonly string AppSettingsPath = Path.Combine(BaseDirectory, "appsettings.json");

        
    }
}