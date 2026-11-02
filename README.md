# Core.AiSoft

## About this solution

This is a layered startup solution based on [Domain Driven Design (DDD)](https://abp.io/docs/latest/framework/architecture/domain-driven-design) practises. All the fundamental ABP modules are already installed. Check the [Application Startup Template](https://abp.io/docs/latest/solution-templates/layered-web-application) documentation for more info.

### Pre-requirements

* [.NET10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 or 20](https://nodejs.org/en)

### Configurations

The solution comes with a default configuration that works out of the box. However, you may consider to change the following configuration before running your solution:


### Before running the application

* Run `abp install-libs` command on your solution folder to install client-side package dependencies. This step is automatically done when you create a new solution, if you didn't especially disabled it. However, you should run it yourself if you have first cloned this solution from your source control, or added a new client-side package dependency to your solution.
* Run `Core.AiSoft.DbMigrator` to create the initial database. This step is also automatically done when you create a new solution, if you didn't especially disabled it. This should be done in the first run. It is also needed if a new database migration is added to the solution later.

#### Generating a Signing Certificate

In the production environment, you need to use a production signing certificate. ABP Framework sets up signing and encryption certificates in your application and expects an `openiddict.pfx` file in your application.

To generate a signing certificate, you can use the following command:

```bash
dotnet dev-certs https -v -ep openiddict.pfx -p d82f790c-4103-43fb-a3ce-0183ed54a382
```

> `d82f790c-4103-43fb-a3ce-0183ed54a382` is the password of the certificate, you can change it to any password you want.

It is recommended to use **two** RSA certificates, distinct from the certificate(s) used for HTTPS: one for encryption, one for signing.

For more information, please refer to: [OpenIddict Certificate Configuration](https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html#registering-a-certificate-recommended-for-production-ready-scenarios)

> Also, see the [Configuring OpenIddict](https://abp.io/docs/latest/Deployment/Configuring-OpenIddict#production-environment) documentation for more information.

### Solution structure

This is a layered monolith application that consists of the following applications:

* `Core.AiSoft.Blazor`: ASP.NET Core Blazor Web App application that supports server-side prerendering and optional client-side interactivity running on WebAssembly.
* `Core.AiSoft.DbMigrator`: A console application which applies the migrations and also seeds the initial data. It is useful on development as well as on production environment.
* `Core.AiSoft.AppHost`: .NET Aspire AppHost that starts the infrastructure containers, the DbMigrator and the Blazor host.
* `Core.AiSoft.ServiceDefaults`: Aspire service defaults (OpenTelemetry, health checks, service discovery, HTTP resilience).
* `Core.AiSoft.Infrastructure`: Non-EF infrastructure: SeaweedFS blob provider (Filer HTTP API, no S3/AWS SDK) and health checks.

### Running with Aspire

```bash
dotnet run --project src/Core.AiSoft.AppHost
```

Running through the AppHost is the only supported way: the apps' `appsettings.json` contain no infrastructure
connection values or secrets, the AppHost injects them. The AppHost prints the dashboard URL.
All containers are persistent and keep their data in Docker volumes.

Endpoints opened by hand have fixed host ports; the others (RabbitMQ AMQP, Qdrant gRPC, SeaweedFS master, FalkorDB) are assigned by Aspire and shown on the dashboard.

| Resource | Purpose | Fixed port |
|---|---|---|
| `postgres` | Database `ai_soft_prod` (user `postgres`) | `localhost:15432` |
| `rabbitmq` | ABP distributed event bus (exchange/queue `AiSoft`, with EF Core outbox/inbox) | management UI http://localhost:15673 (user `guest`) |
| `qdrant` | Vector store for the ingest pipeline | REST + dashboard http://localhost:16333/dashboard |
| `seaweedfs` | File storage via the Filer HTTP API (S3 gateway is not enabled) | filer http://localhost:18888 |
| `falkordb` | Graph store reserved for Graph RAG (not used yet) | – |
| `dbmigrator` | Applies migrations and seeds data, then exits | – |
| `blazor-host` | API + Auth Server + admin UI | https://localhost:44312 (`/health` in Development) |

In Development, `api/dev/infra-check/*` (Swagger, requires login) verifies SeaweedFS (`blob`), RabbitMQ through the outbox/inbox (`event`) and Qdrant (`qdrant`).

### Secrets

Secrets are declared only as AppHost parameters (`secret: true`) in `src/Core.AiSoft.AppHost/AppHost.cs`.
Their values live in the AppHost user secrets under `Parameters:<name>`
(`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`), outside the repository.

| Parameter | Used by | Notes |
|---|---|---|
| `postgres-password` | postgres container, connection strings | Generated by Aspire on first run |
| `rabbitmq-password` | rabbitmq container, event bus | Generated by Aspire on first run |
| `qdrant-api-key` | qdrant container, Qdrant client | Generated by Aspire on first run |
| `string-encryption-passphrase` | blazor-host, dbmigrator (`StringEncryption:DefaultPassPhrase`) | Generated and persisted on first run. Do not change it once encrypted settings exist |
| `admin-password` | dbmigrator (`SeedData:AdminPassword`) | Asked by the dashboard on a new machine; only used when the admin user is first seeded. Must satisfy the ABP password policy |
| `openiddict-cert-passphrase` | blazor-host (`AuthServer:CertificatePassPhrase`) | Publish/deploy only, for `openiddict.pfx` |

View or change them on the dashboard (values are masked) or with:

```bash
dotnet user-secrets list --project src/Core.AiSoft.AppHost
dotnet user-secrets set "Parameters:admin-password" "<value>" --project src/Core.AiSoft.AppHost
```

Note: `postgres-password` and `rabbitmq-password` were used to initialise the data volumes; changing them later requires changing the password inside the database/broker too (or recreating the volumes).

#### Test Projects

The `test` folder contains the following test projects:

* `Core.AiSoft.Application.Tests`: Application layer tests.
* `Core.AiSoft.Domain.Tests`: Domain layer tests.
* `Core.AiSoft.EntityFrameworkCore.Tests`: Entity Framework Core integration tests.

## Known limitations

* **Stateless chat.** Each question is answered on its own. Retrieval and the LLM see only the current question,
  so a follow-up must be self-contained: "còn nghỉ ốm thì sao?" or "what was my first question?" will not work.
  The conversation list in the UI is display-only (kept in server memory, lost on restart).
  - Why: the demo targets a weak local GPU. Sending history makes every prompt longer, so answers get slower.
    It also needs a larger context window (more VRAM), and an 8B model tends to repeat earlier, possibly wrong answers as facts.
  - Without history, the prompt stays small and predictable: system prompt + 5 sources + question.
    The chat model runs with an explicit `num_ctx` of 6144 (`Ollama:ChatContextLength`), because Ollama silently truncates longer prompts.
  - Planned shape if added: send the last turns, budgeted in characters (the same convention as chunking), always keep the first question,
    and shorten old answers. Tell the model that history only explains the question and that facts still come only from the current sources.
* **No reranking.** The knowledge chat sends the top 5 vector search hits (cosine, score ≥ 0.45) to the LLM as they are.
  A chunk on the same topic that does not answer the question can therefore rank above the one that does.
  Ollama has no rerank endpoint ([ollama#16076](https://github.com/ollama/ollama/issues/16076) is still open).
  The workarounds were left out for now:
  - scoring each chunk with a reranker model through `/api/generate` + `logprobs`: one call per chunk, community GGUF builds only;
  - running a separate reranker server (e.g. HuggingFace TEI with `bge-reranker-v2-m3`) as an extra container.
  Planned shape if added: retrieve ~20 candidates, rerank behind an `IKnowledgeReranker` in Infrastructure, keep the top 5.
  Add it once an evaluation set shows that retrieval is the weak point.



## Deploying the application

Deploying an ABP application follows the same process as deploying any .NET or ASP.NET Core application. However, there are important considerations to keep in mind. For detailed guidance, refer to ABP's [deployment documentation](https://abp.io/docs/latest/Deployment/Index).

### Additional resources

You can see the following resources to learn more about your solution and the ABP Framework:

* [Web Application Development Tutorial](https://abp.io/docs/latest/tutorials/book-store/part-1)
* [Application Startup Template](https://abp.io/docs/latest/startup-templates/application/index)
