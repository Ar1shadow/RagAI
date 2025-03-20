using Spectre.Console;

namespace RagAI.Configuration
{
    ///<summary>
    /// For store the path needed
    /// </summary>
    public static class Paths
    {
        // Model Name
        private static readonly Dictionary<string,string> LlmModel =new ()
        {
            {"Mistral-7B","Model/Mistral-7B-Instruct-v0.3.Q4_K_M.gguf"},
            {"DeepSeek-R1-8B","Model/DeepSeek-R1-Distill-Llama-8B-Q4_K_M.gguf"},
            {"Mistral-4B","Model/Mistral-4B.Q4_K_M.gguf"}
        };

        public static string GetModelPath()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please choose[green] an model[/]: ")
                    .AddChoices(LlmModel.Keys));

            if (LlmModel.TryGetValue(choice, out var model))
            {
                AnsiConsole.Write(new Rule(choice));
                return Path.Combine(BaseDirectory, model);
            }else
            {
                throw new InvalidOperationException("Unable to determine the base directory.");
            }
        }
        
        public static string LlmName => GetModelPath();
        public static readonly string EmbeddingName = "Model/nomic-embed-text-v1.5.Q5_K_M.gguf";

        // // generally, working space is Debug,
        // // so return the path of the directory three levels above the current program’s directory.
        public static readonly string BaseDirectory =
            Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.ToString()
            ?? throw new InvalidOperationException("Unable to determine the base directory.");
        
        // Full path of Model
        public static readonly string EmbeddingPath = Path.Combine(BaseDirectory, EmbeddingName);
        
        // path of Docs
        public static readonly string DocsPath = Path.Combine(BaseDirectory, "Docs");
      
        //path of save of Chat session
        public static readonly string ChatHistory = Path.Combine(BaseDirectory, "Assets/ChatSession.json");

        //path of appsetting.json
        public static readonly string AppSettingsPath = Path.Combine(BaseDirectory, "appsettings.json");

        
    }
}