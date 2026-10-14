# Reviewer Guide

## Overview

The implementation provides CRUD operations for insurance claims and covers, progressive premium calculation, business validation, and asynchronous auditing. MongoDB stores claims and covers; SQL Server stores audit records. Swagger documents every controller endpoint.

Recommended review flow: start with Aspire, use Swagger or the PowerShell walkthrough below, then run the tests. Tests do not require Docker or running databases.

| Topic | Reference |
| --- | --- |
| All 11 endpoints and payloads | [API.md](API.md) |
| API startup | [Program.cs](../Claims/Program.cs) |
| Dependency registration | [ServiceCollectionExtensions.cs](../Claims/Extensions/ServiceCollectionExtensions.cs) |
| Thin controllers | [ClaimsController.cs](../Claims/Controllers/ClaimsController.cs), [CoversController.cs](../Claims/Controllers/CoversController.cs) |
| Commands and queries | [Claims requests](../Application/Claims), [Covers requests](../Application/Covers) |
| Business validation | [ClaimService.cs](../Services/ClaimService/ClaimService.cs), [CoverService.cs](../Services/CoverService/CoverService.cs) |
| Premium formula | [PremiumCalculationService.cs](../Services/PremiumCalculationService/PremiumCalculationService.cs) |
| Background audit processor | [QueueProcessorService.cs](../Infrastructure/Messaging/QueueProcessorService.cs) |
| Database orchestration | [AppHost.cs](../AppHost/AppHost.cs) |

All commands assume the terminal is in `insurance-claims-api`, which contains `Claims.sln`. From the parent workspace directory, run:

```powershell
Set-Location insurance-claims-api
```

## Prerequisites

- A .NET SDK capable of building the repository's `net9.0` projects and restoring its current Aspire dependencies. Check installed SDKs with `dotnet --list-sdks`; install a compatible SDK if restore or build reports an unsupported SDK/runtime.
- Docker Desktop or a compatible Docker engine, running Linux containers, for Aspire startup.
- Internet access for the first NuGet restore and container image downloads.
- A trusted HTTPS development certificate. Run `dotnet dev-certs https --trust` locally and accept the OS trust prompt if requested.
- For manual startup instead of Aspire: accessible MongoDB and SQL Server instances. The default audit connection string uses Windows SQL Server LocalDB.

No Azure subscription is needed. Testcontainers packages remain referenced, but the current API startup does not launch Testcontainers; AppHost orchestrates the databases.

## Recommended Startup: Aspire

1. Start Docker Desktop and ensure its engine is ready.
2. Restore and build from the solution directory:

```powershell
dotnet restore Claims.sln
dotnet build Claims.sln
dotnet dev-certs https --trust
```

3. Start the orchestrator:

```powershell
dotnet run --project AppHost/AppHost.csproj --launch-profile https
```

AppHost starts SQL Server and MongoDB, injects the `Audit` and `InsuranceDb` connection strings into the API, and waits for both resources before starting the API. Manual database credentials and application configuration changes are normally unnecessary for this path.

4. Open the Aspire dashboard using the URL printed in the terminal. The HTTPS profile uses `https://localhost:17089`; use the printed URL and login information if requested. Dashboard links can include an access token; do not commit or share it.
5. Wait for the `API` resource to run, open its displayed HTTPS endpoint, and append `/swagger`. Use that endpoint as the request base URL. Do not assume the standalone API port applies under orchestration.

In Development, API startup applies SQL audit migrations and calls MongoDB `EnsureCreated()` for the claims and covers database. MongoDB does not use SQL migrations. Both databases must be reachable before development startup can complete.

Press Ctrl+C in the AppHost terminal to stop the application. The current AppHost does not configure durable database volumes, so do not rely on data surviving resource/container recreation.

## Alternative Startup: Existing Databases

Start your own MongoDB and SQL Server instances, then run the API independently of Aspire.

The defaults in [appsettings.json](../Claims/appsettings.json) are:

| Setting | Default | Purpose |
| --- | --- | --- |
| `ConnectionStrings:InsuranceDb` | `mongodb://localhost:27017` | MongoDB; the API uses database name `InsuranceDb` |
| `ConnectionStrings:Audit` | `Server=(localdb)\mssqllocaldb;Database=AuditDb;Trusted_Connection=True;` | SQL Server LocalDB audit database on Windows |

If MongoDB is not installed locally, one option is a container with a persistent volume:

```powershell
docker run --name insurance-claims-mongo --publish 27017:27017 --volume insurance-claims-mongo-data:/data/db --detach mongo:8.0
```

If that container already exists but is stopped, use `docker start insurance-claims-mongo`. SQL Server or LocalDB is still required for audit storage.

Override connection strings with environment variables in the API terminal or with .NET user secrets. Never commit database passwords. These password-free examples use the default local setup:

```powershell
$env:ConnectionStrings__InsuranceDb = "mongodb://localhost:27017"
$env:ConnectionStrings__Audit = "Server=(localdb)\mssqllocaldb;Database=AuditDb;Trusted_Connection=True;"
```

Replace these values with connection strings appropriate to your database servers. LocalDB is Windows-specific; credentials and TLS settings for other SQL Server deployments depend on your environment.

Start the development profile:

```powershell
dotnet run --project Claims/API.csproj --launch-profile Claims
```

- Swagger UI: `https://localhost:7052/swagger`
- OpenAPI JSON: `https://localhost:7052/swagger/v1/swagger.json`
- API base URL: `https://localhost:7052`
- HTTP listener: `http://localhost:5052`; requests can redirect to HTTPS.

The `Claims` profile sets `ASPNETCORE_ENVIRONMENT=Development`, enabling Swagger and database initialization. In other environments these startup actions are not performed; initialize databases separately before deployment.

Configuration follows ASP.NET Core defaults: base settings, environment-specific settings, Development user secrets, then environment/command-line overrides. [appsettings.Dev.json](../Claims/appsettings.Dev.json) is not automatically loaded by the `Development` profile; the conventional filename is `appsettings.Development.json`. No edit to that file is needed when using Aspire-injected connection strings or the overrides above.

Manual EF commands are not required for Development startup. The optional root tool manifest specifies `dotnet-ef` 10.0.12 while the projects use EF Core 9 packages; check tool/SDK compatibility before using it for manual migration work.

## Architecture

```text
HTTP request
  -> API controller (MediatR dispatch only)
  -> Application command/query handler
  -> Business service
  -> AppDbContext -> MongoDB

Successful write
  -> handler enqueues AuditLog
  -> bounded in-memory channel
  -> QueueProcessorService -> AuditService -> SQL Server

Handler response
  -> ResponseModel<T>
  -> global MVC result filter applies HTTP status
  -> JSON response
```

| Project | Responsibility |
| --- | --- |
| `Claims` / API | HTTP routes, JSON formatting, Swagger, response-status filter, dependency registration |
| `Application` | DTOs, service/queue contracts, response models, typed MediatR requests and handlers |
| `Services` | Business validation, CRUD persistence orchestration, premium calculation, audit persistence |
| `Domain` | Claim, Cover, AuditLog, common entity fields, and enums |
| `Infrastructure` | MongoDB/SQL EF contexts, audit migrations, channel implementation, hosted queue processor |
| `AppHost` | Aspire orchestration and database connection-string injection |
| `ServiceDefaults` | Shared telemetry, resilience, service-discovery, and health-check registrations |
| `Claims.Tests` | Unit, handler, Swagger, HTTP, and real-application integration coverage |

`AuditWorker` is not the active audit processing host. The API registers `QueueProcessorService` as its hosted worker, so audit processing happens in the API process.

## Implemented Features

- CQRS requests and handlers for all 11 endpoints, including premium computation.
- Thin controllers that delegate to `ISender.Send`, with no business, audit, or response-mapping logic.
- Service-layer validation for claim and cover creation/update, with exact service error messages preserved by write handlers.
- A single UTC claim creation timestamp, used both for validation and persistence; an optional `TimeProvider` supports deterministic tests.
- Progressive decimal premium calculation with clear type multipliers and period discounts. Cover updates recalculate the premium.
- Audit records for successful POST, PUT, and DELETE operations, using a channel instead of awaiting a SQL insert in the handler.
- `ResponseModel<T>` envelopes and a global result filter that applies their actual HTTP statuses.
- `[SwaggerOperation]` summaries and descriptions on every controller action, with annotation support enabled.
- API documentation and automated coverage for HTTP contracts, validation, persistence, premiums, timestamp boundaries, and audit enqueueing.

### Validation and Premium Rules

Claim damage cost cannot exceed 100,000; exactly 100,000 is allowed. The related cover must exist, and the claim creation timestamp must lie within its start/end timestamps, inclusively. Claim updates validate the original creation timestamp, not the update time.

Cover start dates cannot precede the server's local current calendar date. End cannot precede start or exceed `startDate.AddYears(1)`. Exactly one calendar year is allowed.

The premium base daily rate is 1250. Multipliers are Yacht 1.10, PassengerShip 1.20, Tanker 1.50, and other types 1.30. The first 30 days use the full rate; the next 150 receive a 5% Yacht or 2% other-type discount; subsequent days receive an 8% Yacht or 3% other-type total discount. Final discounts are additive percentage points, not compounded. Duration uses calendar-day differences and excludes the end date.

## Using the API

There is no frontend application. Use Swagger's **Try it out**, PowerShell, curl, or an API client. Authentication is not currently required.

Create a currently active cover first, then create a claim using its returned ID. IDs, creation timestamps, and cover premiums are server-generated. Do not invent a cover ID or create a claim against a future cover.

The walkthrough below uses dynamic dates. Set `$baseUrl` to the API endpoint shown by Aspire, or use the standalone URL. Trust the development HTTPS certificate; do not disable TLS validation.

```powershell
$baseUrl = "https://localhost:7052"
$start = (Get-Date).Date
$end = $start.AddDays(30)

# Create a cover and retain its ID.
$coverBody = @{
    startDate = $start.ToString("yyyy-MM-dd")
    endDate = $end.ToString("yyyy-MM-dd")
    type = "Yacht"
} | ConvertTo-Json
$coverResponse = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/covers" -ContentType "application/json" -Body $coverBody
$coverId = $coverResponse.data.id
$coverResponse
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/covers/$coverId"
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/covers"

# Compute without saving another cover.
$computeUri = "$baseUrl/api/covers/compute?startDate=$($start.ToString('yyyy-MM-dd'))&endDate=$($end.ToString('yyyy-MM-dd'))&coverType=Yacht"
Invoke-RestMethod -Method Post -Uri $computeUri

# Create and read a claim under the active cover.
$claimBody = @{
    coverId = $coverId
    name = "Engine fire"
    type = "Fire"
    damageCost = 25000
} | ConvertTo-Json
$claimResponse = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/claims" -ContentType "application/json" -Body $claimBody
$claimId = $claimResponse.data.id
$claimResponse
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/claims/$claimId"
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/claims"

# Update all editable claim fields; success returns 202 with null data.
$claimUpdate = @{
    name = "Revised engine fire"
    type = "Fire"
    damageCost = 30000
} | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/claims/$claimId" -ContentType "application/json" -Body $claimUpdate
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/claims/$claimId"

# Extend the cover and recalculate its premium.
$coverUpdate = @{
    startDate = $start.ToString("yyyy-MM-dd")
    endDate = $start.AddDays(60).ToString("yyyy-MM-dd")
    type = "Yacht"
} | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/covers/$coverId" -ContentType "application/json" -Body $coverUpdate
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/covers/$coverId"

# Delete the claim first, then its cover.
Invoke-RestMethod -Method Delete -Uri "$baseUrl/api/claims/$claimId"
Invoke-RestMethod -Method Delete -Uri "$baseUrl/api/covers/$coverId"
```

The 30-day Yacht cover costs 41,250. Creation returns 201, updates return 202 after saving, and successful reads/deletes return 200. See [API.md](API.md) for all request schemas and response details.

To review validation, submit a claim above 100,000, a cover starting yesterday, or a cover longer than one calendar year. Expect HTTP 400 with the specific service message. PowerShell raises an HTTP error for non-success responses; Swagger displays the status and body directly. Missing claim reads currently return 400, while missing cover reads return 404.

Near midnight in a non-UTC time zone, confirm that the cover timestamps include the server's current UTC claim creation time; local-date and UTC-date conventions can differ. The walkthrough assumes compatible server/client date conventions.

### Inspecting Audits

Use a SQL client to inspect the database configured by `ConnectionStrings:Audit`. Successful writes enqueue entity type, entity ID, operation, and timestamp. Allow the worker time to insert the records; there is no audit HTTP endpoint.

The bounded channel's default capacity is 1000 and its full mode is `Wait`, so requests can wait for space when processing falls behind. Audit SQL inserts normally happen outside the request path. If a worker insert throws, the error is logged and the dequeued record is not retried.

## Troubleshooting and Known Limits

- Aspire database startup failures: check Docker, Linux containers, image downloads, ports, and dashboard resource logs.
- Standalone migration failures: check SQL Server/LocalDB, connection strings, and database creation/migration permissions. MongoDB must also be reachable.
- HTTPS failures: trust the development certificate and restart the client. Resolve port conflicts by changing launch settings and the client base URL.
- Swagger is Development-only. Open the API endpoint, not the dashboard endpoint with `/swagger` appended.
- No authentication, authorization policies, pagination, or explicit lower-bound damage-cost validation is implemented.
- Cover date changes do not revalidate existing claims. Cover deletion does not explicitly cascade or block when claims exist.
- Cover validation uses local calendar dates, claim validation uses UTC timestamps, and premiums use calendar-day differences; see [API.md](API.md).
- Premium computation allows past dates and periods longer than one year. Reversed date ranges currently throw an unhandled exception and may return 500.
- MVC binding errors can return `ValidationProblemDetails`, unlike business failures using `ResponseModel<T>`. There is no general exception-to-envelope mapping.
- The audit queue is not durable. Enqueueing follows MongoDB persistence and is not protected by an outbox or distributed transaction; pending audits can be lost on process exit.
- Builds currently report a moderate-severity vulnerability warning for `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.12.0. Dependency remediation is outside this documentation update.

The [Tests](#tests) section below contains full-suite and integration-only commands and explains coverage limits.

## Original Template Notes and Assignment

The following upstream template notes and assignment are retained for reference. Their original setup assumptions are historical; use the reviewer startup instructions above for the current implementation.

# Read this first!
This repository is a template repository for our technical interview, so create your own project using this guide:

[GitHub - Creating a repository from a template](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-repository-from-a-template).

An alternative is to download the code and create a new repository using a different VCS provider (gitlab / Azure repos). **Do not fork this repository.**

When you have completed the tasks, please share the repository link with us. We will review your submission before the interview.

Good luck! 😊

# API Documentation

See [API.md](API.md) for all claims and covers endpoints, request and response examples, HTTP statuses, validation rules, premium computation, and auditing behavior.

# Prerequisites

- Your favourite IDE to code in C# 😊
- _Optional_ - an Azure Subscription. You can demo this API by hosting it in Azure. If that is not an option for you, you can run the demo having a locally running instance. If you select a different cloud provider, that is fine for us.
	- _This requires a rewrite of the setup (program.cs) of the application, as it currently depends on TestContainer packages to simplify the local development experience._
- `Docker Desktop` or a different Docker deamon running on your machine.

# Programming Task
Complete the backend for a multi-tier application for Insurance Claims Handling.
The use case is to maintain a list of insurance claims. The user should be able to create, delete and read claims.
## Task 1
The codebase is messy:
* The controller has too much responsibility. 
* Introduce proper layering within the codebase. 
* Documentation is missing.
* ++

Adhere to the SOLID principles.

### Task 2
As you can see, the API supports some basic REST operations. But validation is missing. The basic rules are:

* Claim
  * DamageCost cannot exceed 100.000
  * Created date must be within the period of the related Cover
* Cover
  * StartDate cannot be in the past
  * total insurance period cannot exceed 1 year

## Task 3
Auditing. The basics are there, but the execution of the DB command (INSERT & DELETE) blocks the processing of the HTTP request. How can this be improved? Look into some asynchronous patterns. It is ok to introduce an Azure managed service to help you with this (ServiceBus/EventGrid/Whatever), but that is not required. Whatever you can manage to get working which is in-memory is also ok.

## Task 4
One basic test is included, please add other (mandatory) unit tests. Note: If you start on this task first, you will find it hard to write proper tests. Some refactoring of the Claims API will be needed. 

## Task 5
Cover premium computation formula evolved over time. Fellow developers were lazy and omitted all tests. Now there are a couple of bugs. Can you fix them? Can you make the computation more readable?

Desired logic: 
* Premium depends on the type of the covered object and the length of the insurance period. 
* Base day rate was set to be 1250.
* Yacht should be 10% more expensive, Passenger ship 20%, Tanker 50%, and other types 30%
* The length of the insurance period should influence the premium progressively:
  * First 30 days are computed based on the logic above
  * Following 150 days are discounted by 5% for Yacht and by 2% for other types
  * The remaining days are discounted by additional 3% for Yacht and by 1% for other types

# Tests

Run all tests from the `insurance-claims-api` directory:

```powershell
dotnet test Claims.Tests/Claims.Tests.csproj
```

Run only the claims and covers integration tests:

```powershell
dotnet test Claims.Tests/Claims.Tests.csproj --filter "FullyQualifiedName~ClaimsControllerTests|FullyQualifiedName~CoversControllerTests"
```

Integration tests start the actual API with `WebApplicationFactory<Program>` and exercise HTTP routing, JSON binding, controllers, MediatR handlers, real services, response statuses, validation, premium calculations, and persistence. Each test uses an isolated EF Core in-memory database, a fixed claim-creation clock, and a recording audit queue. The audit background worker is disabled, so Docker, MongoDB, and SQL Server are not required.

These tests verify audit enqueueing, not the background worker or SQL audit persistence. They also do not test MongoDB-specific queries, mappings, or durability; those require separate database-backed tests.
