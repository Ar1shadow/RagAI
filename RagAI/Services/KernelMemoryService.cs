using System.Diagnostics;
using System.Formats.Asn1;
using LLama;
using LLama.Common;
using LLamaSharp.KernelMemory;
using RagAI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using System.Text.RegularExpressions;

namespace RagAI.Services;

/// <summary>
/// Provides functionality to manage and utilize kernel memory for the application.
/// </summary>
public class KernelMemoryService
{
    private readonly ILogger<KernelMemoryService> _logger;
    private readonly IKernelMemory _kernelMemory;
    private readonly string _modelPath;
    // Using local storage 
    static string StorageFolder => Path.GetFullPath(Path.Combine(Paths.BaseDirectory, "storage"));
    //static bool StorageExists => Directory.Exists(StorageFolder) && Directory.GetDirectories(StorageFolder).Length > 0;

    public IKernelMemory KernelMemory => _kernelMemory; // Get methode
    public string ModelPath => _modelPath; // Get methode
    
    public KernelMemoryService(ILogger<KernelMemoryService> logger)
    {
        _logger = logger;
        _modelPath = Paths.GetModelPath();
        _kernelMemory = Initialize(_modelPath, Paths.EmbeddingPath);
    }

    /// <summary>
    /// Initializes and configures a kernel memory service with specified model and embedding paths.
    /// </summary>
    /// <param name="modelPath">The file path to the large language model (LLM).</param>
    /// <param name="embeddingPath">The file path to the embedding model.</param>
    /// <returns>An instance of <see cref="IKernelMemory"/> that represents the configured kernel memory service.</returns>
    /// <exception cref="Exception">Thrown when there is an error during the creation of the kernel memory service.</exception>
    public IKernelMemory Initialize(string modelPath, string embeddingPath)
    {
        // configure kernel memory
        InferenceParams infParams = new InferenceParams() 
        { 
            AntiPrompts = [ "User;"],
        };
        // configure LLM
        LLamaSharpConfig lsConfig = new(modelPath)
        {
            DefaultInferenceParams = infParams,
            GpuLayerCount = 5, //for CUDA Metal OpenCL

        };
        // Search options
        SearchClientConfig searchClientConfig = new()
        {
            MaxMatchesCount = 5,
            AnswerTokens = 300
        };
        // Text partitioning options
        TextPartitioningOptions parseOptions = new()
        {
            MaxTokensPerParagraph = 400,
            OverlappingTokens = 100
            
        };
        //var llmParams = new ModelParams(modelPath)
        //{
        //    ContextSize = 4096,
        //    GpuLayerCount = 0,
        //    Embeddings = false
        //};
        //var llmWeights = LLamaWeights.LoadFromFile(llmParams);
        // configure embedding model
        var embedWeights = LLamaWeights.LoadFromFile(new ModelParams(embeddingPath)
        {
            Embeddings = true,
            GpuLayerCount = 10, //for CUDA Metal OpenCL
            BatchSize= 2048
        });
        
        SimpleFileStorageConfig storageConfig = new()
        {
            Directory = StorageFolder,
            StorageType = FileSystemTypes.Disk
        };
        
        
        _logger.LogInformation("Creating Kernel Memory, Memory Folder: {path}",StorageFolder);
        try
        {
            return new KernelMemoryBuilder()
                .WithSimpleFileStorage(storageConfig)
                .WithLLamaSharpTextGeneration(new LlamaSharpTextGenerator(lsConfig))
                .WithLLamaSharpTextEmbeddingGeneration(new LLamaSharpTextEmbeddingGenerator(lsConfig, embedWeights))
                .WithSearchClientConfig(searchClientConfig)
                .WithQdrantMemoryDb(new QdrantConfig()
                {
                    Endpoint = "http://localhost:6333",
                    APIKey = ""
                })
                .With(parseOptions)
                .Build();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Kernel Memory");
            throw;
        }
        
    }

    /// <summary>
    /// Ingests documents from a specified directory into kernel memory, ensuring document information
    /// is either loaded or processed and stored for future use.
    /// </summary>
    /// <param name="DocumentsPath">The file path to the directory containing the documents to ingest.</param>
    /// <returns>A task representing the asynchronous operation of ingesting documents.</returns>
    /// <exception cref="Exception">Thrown when there is an error during the ingestion of documents.</exception>
    public async Task IngestDocuments(string DocumentsPath)
    {
        _logger.LogInformation("Start loading documents...");
        // filter the hidden files
        string[] files = Directory.GetFiles(DocumentsPath)
            .Where(file => !Path.GetFileName(file).StartsWith("."))
            .ToArray();
            
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i]; 
            var documentId = Normalize_fileNames(file); 
            Stopwatch sw = Stopwatch.StartNew(); 
            try 
            {   // Check if the document is already in memory
                if (await _kernelMemory.IsDocumentReadyAsync(documentId,index:"docs")) 
                { 
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"{i+1} of {files.Length} :  {file} exists, skipping loading");
                    sw.Stop();
                    continue;
                }
                    
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{i+1} of {files.Length} : Loading {file}");
                await _kernelMemory.ImportDocumentAsync(
                    file,
                    documentId:documentId,
                    index: "docs",
                    steps: Constants.PipelineWithoutSummary);
                    
                Console.WriteLine($"Loading documents {file} done in {sw.Elapsed}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error loading {file} : {index} of {total} \n" +
                                    "\"Skipping file due to unknown error: {file}",file, i+1, files.Length,file);
            }
            
        }
        _logger.LogInformation("Loading documents done");
            
    }

    /// <summary>
    /// Generates a response based on the provided query by utilizing the kernel memory service.
    /// </summary>
    /// <param name="query">The input query or prompt for which a response is to be generated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the response generation process.</exception>
    public async Task GenerateResponse(
        string query,
        string? index = "docs",
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        SearchOptions? options = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        MemoryAnswer answer;
        Stopwatch sw = Stopwatch.StartNew();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Start generating response...");
        try
        {
             answer = await _kernelMemory
                 .AskAsync(query,index,filter,filters,minRelevance,options,context,cancellationToken);
            Console.WriteLine($"Generating response done in {sw.Elapsed}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error generating response");
            throw;
        }
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Assistant:{answer.Result}");
        foreach (var source in answer.RelevantSources)
        {
            Console.WriteLine($"Source:{source.SourceName}");
            //Console.WriteLine(source.Partitions);
        }
        Console.WriteLine();
    }

    public async Task<string> GetMemory(string query, string? index="docs", double minRelevance = 0,bool asChunks = true)
    {
        if (asChunks)
        {
            try
            {
                SearchResult memories = await _kernelMemory.SearchAsync(query, index: index, limit:3);
                return memories.Results
                    .Select(m=>m.Partitions)
                    .Aggregate("",(sum,chunk)=>sum + string.Join("\n",chunk.Select(p=>p.Text)) + "\n").Trim();//chunk[0]?
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error searching in memory");
                throw;
            }
           
        }
        MemoryAnswer answer = await _kernelMemory.AskAsync(query, index: index, minRelevance: minRelevance);
        return answer.Result.Trim();
    }
    
    public static string Normalize_fileNames(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        // remove special characters
        var documentId = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "_")
                .Replace("-","_");
        // Replaces all non-alphanumeric characters in the file name with underscores
        // and removes consecutive underscores.
        documentId = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "_")
            .Replace(@"_{2,}", "_");
        // remove underscore from start to end
        documentId = documentId.Trim('_');
        return documentId;
    }
    
    
}