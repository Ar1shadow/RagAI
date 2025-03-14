using System.Diagnostics;
using LLama;
using LLama.Common;
using LLamaSharp.KernelMemory;
using RagAI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;


namespace RagAI.Services;

/// <summary>
/// Provides functionality to manage and utilize kernel memory for the application.
/// </summary>
public class KernelMemoryService
{
    private readonly ILogger<KernelMemoryService> _logger;
    private readonly IKernelMemory _kernelMemory;
    // Using local storage for test
    static string StorageFolder => Path.GetFullPath(Path.Combine(Paths.BaseDirectory, "storage"));
    static bool StorageExists => Directory.Exists(StorageFolder) && Directory.GetDirectories(StorageFolder).Length > 0;

    public IKernelMemory KernelMemory => _kernelMemory; // Get methode
    
    public KernelMemoryService(ILogger<KernelMemoryService> logger)
    {
        _logger = logger;
        _kernelMemory = Initialize(Paths.LlmPath, Paths.EmbeddingPath);
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
        InferenceParams infParams = new InferenceParams() { AntiPrompts = ["\n\n", "User;"]};
        // configure LLM
        LLamaSharpConfig lsConfig = new(Paths.LlmPath)
        {
            DefaultInferenceParams = infParams,
            GpuLayerCount = 0, //Use Cpu Only
        };
        // Search options
        SearchClientConfig searchClientConfig = new()
        {
            MaxMatchesCount = 5,
            AnswerTokens = 200
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
            Embeddings = true
        });
        
        SimpleFileStorageConfig storageConfig = new()
        {
            Directory = StorageFolder,
            StorageType = FileSystemTypes.Disk
        };

        SimpleVectorDbConfig vectorDb = new()
            {
                Directory = StorageFolder,
                StorageType = FileSystemTypes.Disk,
            };
        //var context = llmWeights.CreateContext(llmParams);
        //var executor = new StatelessExecutor(llmWeights, llmParams);
        
        _logger.LogInformation("Creating Kernel Memory, Memory Folder: {path}",StorageFolder);
        try
        {
            return new KernelMemoryBuilder()
                .WithSimpleFileStorage(storageConfig)
                .WithSimpleVectorDb(vectorDb)
                .WithLLamaSharpTextGeneration(new LlamaSharpTextGenerator(lsConfig))
                .WithLLamaSharpTextEmbeddingGeneration(new LLamaSharpTextEmbeddingGenerator(lsConfig, embedWeights))
                .WithSearchClientConfig(searchClientConfig)
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
    public async Task IngestDocments(string DocumentsPath)
    {
        if (StorageExists)
        {
            _logger.LogInformation("Kernel Memory Storage Folder located,\n" +
                                   "Information about previously analyzed documents has been loaded");
        
        }else
        {
            Console.WriteLine($"""
                            Kernel Memory Storage Folder not located!
                            Documents will be loaded into Kernel Memory.
                            Analysis will not be required the next time.
                            """);
            _logger.LogInformation("Start loading documents...");
            string[] files = Directory.GetFiles(DocumentsPath);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("{index} of {total} : Loading {file}", i+1, files.Length, file);
                    await _kernelMemory.ImportDocumentAsync(file, steps: Constants.PipelineWithoutSummary);
                    _logger.LogInformation("Loading documents done in {time}", sw.Elapsed);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error loading {file} : {index} of {total} :",file, i+1, files.Length);
                    throw;
                }
            
            }
            _logger.LogInformation("Loading documents done");
        }

        
    }

    /// <summary>
    /// Generates a response based on the provided query by utilizing the kernel memory service.
    /// </summary>
    /// <param name="query">The input query or prompt for which a response is to be generated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the response generation process.</exception>
    public async Task GenerateResponse(string query)
    {
        MemoryAnswer answer;
        Stopwatch sw = Stopwatch.StartNew();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Start generating response...");
        try
        {
             answer = await _kernelMemory.AskAsync(query);
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
            Console.WriteLine(source.Partitions);
        }
        Console.WriteLine();
    }
    
    
    
}