var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SpeakStoreLocate_ApiService>("apiservice");

builder.Build().Run();