using Grpc.Core;
using TaskManager.Grpc;
using TaskManager.Service.Storage;

namespace TaskManager.Service.Services;

public sealed class TaskManagerService : TaskService.TaskServiceBase
{
    private readonly TaskRepository _repository;
    private readonly IConfiguration _configuration;

    public TaskManagerService(TaskRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    public override Task<Grpc.Task> CreateTask(CreateTaskRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title is required."));
        }

        var created = _repository.Create(request.Title.Trim());
        return System.Threading.Tasks.Task.FromResult(ToProto(created));
    }

    public override async Task<Grpc.Task> GetTask(GetTaskRequest request, ServerCallContext context)
    {
        if (request.Id < 1)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Task id must be greater than or equal to 1."));
        }

        var delaySeconds = _configuration.GetValue("TaskService:GetTaskDelaySeconds", 0);
        if (delaySeconds > 0)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delaySeconds), context.CancellationToken);
        }

        if (!_repository.TryGet(request.Id, out var task) || task is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Task {request.Id} was not found."));
        }

        return ToProto(task);
    }

    public override async System.Threading.Tasks.Task ListTasks(
        ListTasksRequest request,
        IServerStreamWriter<Grpc.Task> responseStream,
        ServerCallContext context)
    {
        foreach (var task in _repository.GetAll())
        {
            await responseStream.WriteAsync(ToProto(task));
        }
    }

    public override Task<Grpc.Task> UpdateTask(UpdateTaskRequest request, ServerCallContext context)
    {
        if (!TaskStatusValidator.IsValid(request.Status))
        {
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition,
                $"Status must be one of: {string.Join(", ", TaskStatusValidator.AllowedStatuses)}."));
        }

        if (!_repository.TryUpdateStatus(request.Id, request.Status, out var updated) || updated is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Task {request.Id} was not found."));
        }

        return System.Threading.Tasks.Task.FromResult(ToProto(updated));
    }

    public override async System.Threading.Tasks.Task StreamTaskUpdates(
        IAsyncStreamReader<TaskUpdateRequest> requestStream,
        IServerStreamWriter<TaskUpdateReply> responseStream,
        ServerCallContext context)
    {
        await foreach (var update in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (!TaskStatusValidator.IsValid(update.Status))
            {
                throw new RpcException(new Status(
                    StatusCode.FailedPrecondition,
                    $"Status must be one of: {string.Join(", ", TaskStatusValidator.AllowedStatuses)}."));
            }

            if (!_repository.TryUpdateStatus(update.Id, update.Status, out var updated) || updated is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Task {update.Id} was not found."));
            }

            await responseStream.WriteAsync(new TaskUpdateReply
            {
                Task = ToProto(updated),
                Message = $"Task {updated.Id} updated to '{updated.Status}'."
            });
        }
    }

    private static Grpc.Task ToProto(TaskRecord record) =>
        new()
        {
            Id = record.Id,
            Title = record.Title,
            Status = record.Status
        };
}
