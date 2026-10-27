using Grpc.Core;
using Grpc.Net.Client;
using TaskManager.Grpc;

const string serverAddress = "http://localhost:5081";

using var channel = GrpcChannel.ForAddress(serverAddress);
var client = new TaskService.TaskServiceClient(channel);

Console.WriteLine($"Connected to Task Manager at {serverAddress}");
Console.WriteLine();

Console.WriteLine("--- Create three tasks ---");
var task1 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Write proto file" });
var task2 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Implement gRPC service" });
var task3 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Build gRPC client" });
PrintTask("Created", task1);
PrintTask("Created", task2);
PrintTask("Created", task3);
Console.WriteLine();

Console.WriteLine("--- Fetch task #2 (1-second deadline) ---");
try
{
    var deadline = DateTime.UtcNow.AddSeconds(1);
    var fetched = await client.GetTaskAsync(
        new GetTaskRequest { Id = task2.Id },
        new CallOptions(deadline: deadline));
    PrintTask("Fetched", fetched);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    Console.WriteLine("GetTask timed out after 1 second (DeadlineExceeded).");
    Console.WriteLine($"Details: {ex.Status.Detail}");
}
Console.WriteLine();

Console.WriteLine("--- List all tasks (server streaming) ---");
using (var listCall = client.ListTasks(new ListTasksRequest()))
{
    await foreach (var task in listCall.ResponseStream.ReadAllAsync())
    {
        PrintTask("Listed", task);
    }
}
Console.WriteLine();

Console.WriteLine("--- Stream three task updates (bidirectional) ---");
using var streamCall = client.StreamTaskUpdates();
var updates = new[]
{
    new TaskUpdateRequest { Id = task1.Id, Status = "in_progress" },
    new TaskUpdateRequest { Id = task2.Id, Status = "done" },
    new TaskUpdateRequest { Id = task3.Id, Status = "in_progress" }
};

var readResponses = System.Threading.Tasks.Task.Run(async () =>
{
    await foreach (var reply in streamCall.ResponseStream.ReadAllAsync())
    {
        PrintTask("Server response", reply.Task);
        Console.WriteLine($"  Message: {reply.Message}");
    }
});

foreach (var update in updates)
{
    await streamCall.RequestStream.WriteAsync(update);
}

await streamCall.RequestStream.CompleteAsync();
await readResponses;
Console.WriteLine();

Console.WriteLine("--- Error handling demos ---");
await ExpectRpcError(
    "GetTask with id=0",
    async () => await client.GetTaskAsync(new GetTaskRequest { Id = 0 }),
    StatusCode.InvalidArgument);

await ExpectRpcError(
    "GetTask for missing task",
    async () => await client.GetTaskAsync(new GetTaskRequest { Id = 9_999 }),
    StatusCode.NotFound);

await ExpectRpcError(
    "UpdateTask with invalid status",
    async () => await client.UpdateTaskAsync(new UpdateTaskRequest { Id = task1.Id, Status = "blocked" }),
    StatusCode.FailedPrecondition);
Console.WriteLine();

if (args.Contains("--demo-deadline", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("--- GetTask deadline demo (service should use GetTaskDelaySeconds=2) ---");
    try
    {
        var deadline = DateTime.UtcNow.AddSeconds(1);
        await client.GetTaskAsync(new GetTaskRequest { Id = task1.Id }, new CallOptions(deadline: deadline));
        Console.WriteLine("GetTask completed within the deadline.");
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
    {
        Console.WriteLine("Handled timeout gracefully: DeadlineExceeded.");
        Console.WriteLine($"Details: {ex.Status.Detail}");
    }
}
else
{
    Console.WriteLine("Tip: run with --demo-deadline after starting the service with GetTaskDelaySeconds=2.");
}

Console.WriteLine();
Console.WriteLine("Client finished.");

static void PrintTask(string label, TaskManager.Grpc.Task task) =>
    Console.WriteLine($"{label}: id={task.Id}, title=\"{task.Title}\", status={task.Status}");

static async System.Threading.Tasks.Task ExpectRpcError(
    string scenario,
    Func<System.Threading.Tasks.Task> action,
    StatusCode expected)
{
    try
    {
        await action();
        Console.WriteLine($"{scenario}: expected {expected} but call succeeded.");
    }
    catch (RpcException ex) when (ex.StatusCode == expected)
    {
        Console.WriteLine($"{scenario}: received {ex.StatusCode} as expected ({ex.Status.Detail}).");
    }
}
