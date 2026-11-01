var builder = DistributedApplication.CreateBuilder(args);

// Secrets saved in: %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json by default.
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);
var qdrantApiKey = builder.AddParameter("qdrant-api-key", secret: true);
var stringEncryptionPassPhrase = builder.AddParameter(
    "string-encryption-passphrase",
    new GenerateParameterDefault { MinLength = 32, Special = false },
    secret: true,
    persist: true);
var adminPassword = builder.AddParameter("admin-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 15432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var aiSoftDb = postgres.AddDatabase("aisoft-db", databaseName: "ai_soft_prod");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", password: rabbitmqPassword)
    .WithManagementPlugin(port: 15673)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Vecto db
var qdrant = builder.AddQdrant("qdrant", apiKey: qdrantApiKey, httpPort: 16333)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// File storage
var seaweedfs = builder.AddContainer("seaweedfs", "chrislusf/seaweedfs", "4.48")
    .WithArgs("server", "-filer")
    .WithHttpEndpoint(port: 18888, targetPort: 8888, name: "filer")
    .WithHttpEndpoint(targetPort: 9333, name: "master")
    .WithHttpHealthCheck("/", endpointName: "filer")
    .WithVolume("core-aisoft-seaweedfs-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var ollama = builder.AddOllama("ollama", port: 11435)
    .WithImageTag("0.40.13")
    .WithDataVolume("core-aisoft-ollama-data")
    .WithGPUSupport()
    .WithLifetime(ContainerLifetime.Persistent);

var embeddingModel = ollama.AddModel("embedding-model", "qwen3-embedding:0.6b");
var chatModel = ollama.AddModel("chat-model", "qwen3:8b");

// Reserved
builder.AddContainer("falkordb", "falkordb/falkordb", "6.0.2")
    .WithEndpoint(targetPort: 6379, name: "redis")
    .WithHttpEndpoint(targetPort: 3000, name: "browser")
    .WithVolume("core-aisoft-falkordb-data", "/var/lib/falkordb/data")
    .WithLifetime(ContainerLifetime.Persistent);

var migrator = builder.AddProject<Projects.Core_AiSoft_DbMigrator>("dbmigrator")
    .WithReference(aiSoftDb, "Default")
    .WithEnvironment("StringEncryption__DefaultPassPhrase", stringEncryptionPassPhrase)
    .WithEnvironment("SeedData__AdminPassword", adminPassword)
    .WaitFor(aiSoftDb);

var blazorHost = builder.AddProject<Projects.Core_AiSoft_Blazor>("blazor-host")
    .WithReference(aiSoftDb, "Default")
    .WithReference(rabbitmq)
    .WithReference(qdrant)
    .WithReference(embeddingModel)
    .WithReference(chatModel)
    .WithEnvironment("SeaweedFs__FilerUrl", seaweedfs.GetEndpoint("filer"))
    .WithEnvironment("StringEncryption__DefaultPassPhrase", stringEncryptionPassPhrase)
    .WaitForCompletion(migrator)
    .WaitFor(rabbitmq)
    .WaitFor(qdrant)
    .WaitFor(seaweedfs)
    .WaitFor(embeddingModel)
    .WithEndpoint("https", endpoint => endpoint.IsProxied = false);

if (builder.ExecutionContext.IsPublishMode)
{
    var openIddictCertPassPhrase = builder.AddParameter("openiddict-cert-passphrase", secret: true);
    blazorHost.WithEnvironment("AuthServer__CertificatePassPhrase", openIddictCertPassPhrase);
}

builder.Build().Run();
