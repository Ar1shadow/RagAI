connectors”) varies across languages.
Kernel Memory (KM) is a service built on the feedback received and lessons learned from
developing Semantic Kernel (SK) and Semantic Memory (SM). It provides several features that would
otherwise have to be developed manually, such as storing files, extracting text from files, providing a
framework to secure users’ data, etc. The KM codebase is entirely in .NET, which eliminates the need to
write and maintain features in multiple languages. As a service, KM can be used from any language,
tool, or platform, e.g. browser extensions and ChatGPT assistants.
Kernel Memory
Kernel Memory (KM) and Semantic Memory (SM)Here’s a few notable differences:
Feature Semantic Memory Kernel Memory
Data formats Text only
Web pages, PDF, Images, Word, PowerPoint,
Excel, Markdown, Text, JSON, more being
added
Search Cosine similarity Cosine similarity, Hybrid search with filters,
AND/OR conditions
Language
support C#, Python, Java
Any language, command line tools, browser
extensions, low-code/no-code apps,
chatbots, assistants, etc.
Storage engines
Azure AI Search, Chroma, DuckDB, Kusto,
Milvus, MongoDB, Pinecone, Postgres,
Qdrant, Redis, SQLite, Weaviate
Azure AI Search, Elasticsearch, Postgres,
Qdrant, Redis, SQL Server, In memory KNN,
On disk KNN. 