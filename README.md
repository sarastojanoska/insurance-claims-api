# Insurance Claims API

A layered .NET API for insurance covers, claims, progressive premium calculation, and asynchronous auditing.

Start with the [Reviewer Guide](docs/README.md) for prerequisites, startup commands, architecture, implemented features, a runnable API walkthrough, tests, and known limitations.

See the [API Reference](docs/API.md) for all 11 endpoints, payloads, HTTP statuses, validation messages, and date semantics.

Quick start from this directory, with a compatible .NET SDK and Docker running:

```powershell
dotnet restore Claims.sln
dotnet dev-certs https --trust
dotnet run --project AppHost/AppHost.csproj --launch-profile https
```

Open the API endpoint displayed in the Aspire dashboard and append `/swagger`. The reviewer guide also explains running with your own MongoDB and SQL Server instances.

NOTES:
- Due to lack of business requirements, updating covers leaves claims in an inconsistent state (outside the time period). This is a known limitation. It will be fixed once clarified requirements are provided.