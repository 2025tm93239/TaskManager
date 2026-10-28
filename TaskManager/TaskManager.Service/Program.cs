using TaskManager.Service.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskManagerService>();
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<TaskManagerService>();
app.MapGet("/", () => "TaskManager gRPC server running on port 5081");

app.Run();
