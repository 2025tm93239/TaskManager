namespace TaskManager.Service.Storage;

public sealed class TaskRecord
{
    public required int Id { get; init; }
    public required string Title { get; set; }
    public required string Status { get; set; }
}

public sealed class TaskRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<int, TaskRecord> _tasks = new();
    private int _nextId = 1;

    public TaskRecord Create(string title)
    {
        lock (_sync)
        {
            var task = new TaskRecord
            {
                Id = _nextId++,
                Title = title,
                Status = "pending"
            };
            _tasks[task.Id] = task;
            return task;
        }
    }

    public bool TryGet(int id, out TaskRecord? task)
    {
        lock (_sync)
        {
            return _tasks.TryGetValue(id, out task);
        }
    }

    public IReadOnlyList<TaskRecord> GetAll()
    {
        lock (_sync)
        {
            return _tasks.Values.OrderBy(t => t.Id).ToList();
        }
    }

    public bool TryUpdateStatus(int id, string status, out TaskRecord? task)
    {
        lock (_sync)
        {
            if (!_tasks.TryGetValue(id, out var existing))
            {
                task = null;
                return false;
            }

            existing.Status = status;
            task = existing;
            return true;
        }
    }
}
