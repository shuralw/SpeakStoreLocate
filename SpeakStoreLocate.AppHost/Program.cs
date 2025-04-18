var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SpeakStoreLocate_ApiService>("apiservice");

builder.AddProject<Projects.SpeakStoreLocate_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
