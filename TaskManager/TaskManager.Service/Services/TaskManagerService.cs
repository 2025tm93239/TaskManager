using Grpc.Core;
using TaskManager.Grpc;

namespace TaskManager.Service.Services;

public class TaskManagerService : TaskService.TaskServiceBase
{
    private readonly Dictionary<int, TaskItem> _tasks = new();
    private int _nextId = 1;
    private readonly IConfiguration _config;

    public TaskManagerService(IConfiguration config)
    {
        _config = config;
    }

    public override Task<Grpc.Task> CreateTask(CreateTaskRequest request, ServerCallContext context)
    {
        var title = request.Title?.Trim() ?? "";
        if (title.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title cannot be empty"));
        }

        var item = new TaskItem
        {
            Id = _nextId++,
            Title = title,
            Status = "pending"
        };
        _tasks[item.Id] = item;

        return System.Threading.Tasks.Task.FromResult(MapToGrpc(item));
    }

    public override async System.Threading.Tasks.Task<Grpc.Task> GetTask(GetTaskRequest request, ServerCallContext context)
    {
        if (request.Id < 1)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be >= 1"));
        }

        // for deadline demo - set TaskService:GetTaskDelaySeconds to 2 in appsettings
        var delayStr = _config["TaskService:GetTaskDelaySeconds"];
        if (int.TryParse(delayStr, out var delaySec) && delaySec > 0)
        {
            await System.Threading.Tasks.Task.Delay(delaySec * 1000, context.CancellationToken);
        }

        if (!_tasks.ContainsKey(request.Id))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Task not found"));
        }

        return MapToGrpc(_tasks[request.Id]);
    }

    public override async System.Threading.Tasks.Task ListTasks(
        ListTasksRequest request,
        IServerStreamWriter<Grpc.Task> responseStream,
        ServerCallContext context)
    {
        foreach (var item in _tasks.Values.OrderBy(t => t.Id))
        {
            await responseStream.WriteAsync(MapToGrpc(item));
        }
    }

    public override Task<Grpc.Task> UpdateTask(UpdateTaskRequest request, ServerCallContext context)
    {
        if (!IsValidStatus(request.Status))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invalid status value"));
        }

        if (!_tasks.ContainsKey(request.Id))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Task not found"));
        }

        _tasks[request.Id].Status = request.Status;
        return System.Threading.Tasks.Task.FromResult(MapToGrpc(_tasks[request.Id]));
    }

    public override async System.Threading.Tasks.Task StreamTaskUpdates(
        IAsyncStreamReader<TaskUpdateRequest> requestStream,
        IServerStreamWriter<TaskUpdateReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            var update = requestStream.Current;

            if (!IsValidStatus(update.Status))
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invalid status value"));
            }

            if (!_tasks.ContainsKey(update.Id))
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Task not found"));
            }

            _tasks[update.Id].Status = update.Status;
            var item = _tasks[update.Id];

            await responseStream.WriteAsync(new TaskUpdateReply
            {
                Task = MapToGrpc(item),
                Message = "Updated task " + item.Id
            });
        }
    }

    private static bool IsValidStatus(string status)
    {
        return status == "pending" || status == "in_progress" || status == "done";
    }

    private static Grpc.Task MapToGrpc(TaskItem item)
    {
        return new Grpc.Task
        {
            Id = item.Id,
            Title = item.Title,
            Status = item.Status
        };
    }

    private class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "pending";
    }
}
