var builder = DistributedApplication.CreateBuilder(args);

// add sql server
var sqlServer = builder.AddSqlServer("sqlServer");
var sqlDatabase = sqlServer.AddDatabase("Audit");   // this represents connectionString - to be injected 

// add mongodb server
var mongoDb = builder.AddMongoDB("mongoDb");
var mongoDatabase = mongoDb.AddDatabase("InsuranceDb"); // this represents connectionString - to be injected 

builder.AddProject<Projects.API>("API")
    .WithReference(sqlDatabase)
    .WithReference(mongoDatabase)
    .WaitFor(sqlDatabase)
    .WaitFor(mongoDatabase);

builder.Build().Run();
