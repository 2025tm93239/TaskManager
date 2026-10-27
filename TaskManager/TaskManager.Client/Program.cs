using Grpc.Core;
using Grpc.Net.Client;
using TaskManager.Grpc;

const string serverAddress = "http://localhost:5081";

using var grpcChannel = GrpcChannel.ForAddress(serverAddress);
var taskServiceClient = new TaskService.TaskServiceClient(grpcChannel);

Console.WriteLine($"Connected to Task Manager at {serverAddress}");
Console.WriteLine();

Console.WriteLine("--- TasK Manager servvice will Create three tasks ---");
var firstTask = await taskServiceClient.CreateTaskAsync(new CreateTaskRequest { Title = "Write proto file" });
var secondTask = await taskServiceClient.CreateTaskAsync(new CreateTaskRequest { Title = "Implement gRPC service" });
var thirdTask = await taskServiceClient.CreateTaskAsync(new CreateTaskRequest { Title = "Build gRPC client" });
PrintTask("Created", firstTask);
PrintTask("Created", secondTask);
PrintTask("Created", thirdTask);
Console.WriteLine();

Console.WriteLine("--- Fetch task #2 (GetTask with 1-second client deadline) ---");
try
{
    var deadline = DateTime.UtcNow.AddSeconds(1);
    var fetched = await taskServiceClient.GetTaskAsync(
        new GetTaskRequest { Id = secondTask.Id },
        new CallOptions(deadline: deadline));
    PrintTask("Fetched", fetched);
    Console.WriteLine("GetTask completed before the 1-second deadline.");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    Console.WriteLine("GetTask handled timeout gracefully: DEADLINE_EXCEEDED.");
    Console.WriteLine($"Details: {ex.Status.Detail}");
}
Console.WriteLine();

Console.WriteLine("--- List all tasks (server streaming) ---");
using (var listCall = taskServiceClient.ListTasks(new ListTasksRequest()))
{
    await foreach (var task in listCall.ResponseStream.ReadAllAsync())
    {
        PrintTask("Listed", task);
    }
}
Console.WriteLine();

Console.WriteLine("--- Stream three task updates (bidirectional) ---");
using var streamCall = taskServiceClient.StreamTaskUpdates();
var updates = new[]
{
    new TaskUpdateRequest { Id = firstTask.Id, Status = "in_progress" },
    new TaskUpdateRequest { Id = secondTask.Id, Status = "done" },
    new TaskUpdateRequest { Id = thirdTask.Id, Status = "in_progress" }
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

Console.WriteLine("--- Assignment error cases (server status codes) ---");

//To test INVALID_ARGUMENT: If GetTaskRequest.id < 1, return INVALID_ARGUMENT
await ExpectRpcError(
    "Error -  GetTask id < 1 → INVALID_ARGUMENT",
    async () => await taskServiceClient.GetTaskAsync(new GetTaskRequest { Id = 0 }),
    StatusCode.InvalidArgument);

//To test NOT_FOUND error
await ExpectRpcError(
    "Error: GetTask missing id → NOT_FOUND",
    async () => await taskServiceClient.GetTaskAsync(new GetTaskRequest { Id = 9_999 }),
    StatusCode.NotFound);

//To Test FAILED_PRECONDITION scenario
await ExpectRpcError(
    "Error - UpdateTask invalid status → FAILED_PRECONDITION",
    async () => await taskServiceClient.UpdateTaskAsync(new UpdateTaskRequest { Id = firstTask.Id, Status = "blocked" }),
    StatusCode.FailedPrecondition);

Console.WriteLine();

if (args.Contains("--demo-deadline", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("--- Extra GetTask deadline call (service GetTaskDelaySeconds=2) ---");
    try
    {
        var deadline = DateTime.UtcNow.AddSeconds(1);
        await taskServiceClient.GetTaskAsync(new GetTaskRequest { Id = firstTask.Id }, new CallOptions(deadline: deadline));
        Console.WriteLine("GetTask completed before the 1-second deadline.");
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
    {
        Console.WriteLine("GetTask handled timeout gracefully: DEADLINE_EXCEEDED.");
        Console.WriteLine($"Details: {ex.Status.Detail}");
    }
}
else
{

    Console.WriteLine(
        "Deadline timeout screenshot: start TaskManager.Service with profile http-deadline-demo " +
        "(GetTask sleeps 2s); Fetch task #2 above will show DEADLINE_EXCEEDED.");
}

Console.WriteLine();

Console.WriteLine("Client finished its execution.");
Console.WriteLine("Press Enter to close the client application...");
Console.ReadLine();

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
        Console.WriteLine($"{scenario}: OK — {ToGrpcCodeName(ex.StatusCode)} ({ex.Status.Detail}).");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"{scenario}: unexpected gRPC status {ToGrpcCodeName(ex.StatusCode)} ({ex.Status.Detail}).");
    }
}


static string ToGrpcCodeName(StatusCode code) => code switch
{
    StatusCode.InvalidArgument => "INVALID_ARGUMENT",
    StatusCode.NotFound => "NOT_FOUND",
    StatusCode.FailedPrecondition => "FAILED_PRECONDITION",
    StatusCode.DeadlineExceeded => "DEADLINE_EXCEEDED",
    _ => code.ToString()
};
