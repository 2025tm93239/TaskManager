using TaskManager.Service.Services;
using TaskManager.Service.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TaskRepository>();
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<TaskManagerService>();
app.MapGet("/", () =>
    "Task Manager gRPC service. Run TaskManager.Client to invoke TaskService RPCs.");

app.Run();
