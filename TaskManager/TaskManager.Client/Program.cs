using Grpc.Core;
using Grpc.Net.Client;
using TaskManager.Grpc;

var serverUrl = "http://localhost:5081";
using var channel = GrpcChannel.ForAddress(serverUrl);
var client = new TaskService.TaskServiceClient(channel);

Console.WriteLine("Task Manager Client");
Console.WriteLine("Connecting to " + serverUrl);
Console.WriteLine();

// 1. Create three tasks
Console.WriteLine("Creating 3 tasks...");
var t1 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Write proto file" });
var t2 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Implement gRPC service" });
var t3 = await client.CreateTaskAsync(new CreateTaskRequest { Title = "Build gRPC client" });
Print(t1);
Print(t2);
Print(t3);
Console.WriteLine();

// 2. Fetch task #2 with 1 second deadline
Console.WriteLine("GetTask for id=" + t2.Id + " (1 sec deadline)...");
try
{
    var deadline = DateTime.UtcNow.AddSeconds(1);
    var fetched = await client.GetTaskAsync(
        new GetTaskRequest { Id = t2.Id },
        new CallOptions(deadline: deadline));
    Print(fetched);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    Console.WriteLine("Timeout - DEADLINE_EXCEEDED");
    Console.WriteLine(ex.Status.Detail);
}
Console.WriteLine();

// 3. ListTasks server streaming
Console.WriteLine("ListTasks (streaming):");
using (var listCall = client.ListTasks(new ListTasksRequest()))
{
    await foreach (var task in listCall.ResponseStream.ReadAllAsync())
    {
        Print(task);
    }
}
Console.WriteLine();

// 4. StreamTaskUpdates - send 3 updates
Console.WriteLine("StreamTaskUpdates:");
using var stream = client.StreamTaskUpdates();

await stream.RequestStream.WriteAsync(new TaskUpdateRequest { Id = t1.Id, Status = "in_progress" });
await stream.RequestStream.WriteAsync(new TaskUpdateRequest { Id = t2.Id, Status = "done" });
await stream.RequestStream.WriteAsync(new TaskUpdateRequest { Id = t3.Id, Status = "in_progress" });
await stream.RequestStream.CompleteAsync();

await foreach (var reply in stream.ResponseStream.ReadAllAsync())
{
    Print(reply.Task);
    Console.WriteLine("  " + reply.Message);
}
Console.WriteLine();

// Error cases from assignment
Console.WriteLine("Testing error responses:");

try
{
    await client.GetTaskAsync(new GetTaskRequest { Id = 0 });
    Console.WriteLine("GetTask id=0: expected error but got success");
}
catch (RpcException ex)
{
    Console.WriteLine("GetTask id=0: " + ex.StatusCode + " - " + ex.Status.Detail);
}

try
{
    await client.GetTaskAsync(new GetTaskRequest { Id = 9999 });
    Console.WriteLine("GetTask missing: expected error but got success");
}
catch (RpcException ex)
{
    Console.WriteLine("GetTask missing: " + ex.StatusCode + " - " + ex.Status.Detail);
}

try
{
    await client.UpdateTaskAsync(new UpdateTaskRequest { Id = t1.Id, Status = "blocked" });
    Console.WriteLine("UpdateTask bad status: expected error but got success");
}
catch (RpcException ex)
{
    Console.WriteLine("UpdateTask bad status: " + ex.StatusCode + " - " + ex.Status.Detail);
}

Console.WriteLine();
Console.WriteLine("Done. Press Enter to exit.");
Console.ReadLine();

static void Print(TaskManager.Grpc.Task t)
{
    Console.WriteLine($"  id={t.Id}, title={t.Title}, status={t.Status}");
}
