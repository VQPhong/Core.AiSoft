var builder = DistributedApplication.CreateBuilder(args);

// Persistent container + data volume so data and the generated password survive AppHost restarts.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var aiSoftDb = postgres.AddDatabase("aisoft-db", databaseName: "ai_soft_prod");

// Injected as ConnectionStrings__Default, the name ABP reads.
var migrator = builder.AddProject<Projects.Core_AiSoft_DbMigrator>("dbmigrator")
    .WithReference(aiSoftDb, "Default")
    .WaitFor(aiSoftDb);

// Single host: API + Auth Server (OpenIddict) + ABP admin UI (Blazor Web App).
// Keep https://localhost:44312 unproxied: App:SelfUrl and AuthServer:Authority point at it.
builder.AddProject<Projects.Core_AiSoft_Blazor>("blazor-host")
    .WithReference(aiSoftDb, "Default")
    .WaitForCompletion(migrator)
    .WithEndpoint("https", endpoint => endpoint.IsProxied = false);

builder.Build().Run();
