namespace TaskManager.Service.Services;

public static class TaskStatusValidator
{
    public static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "pending",
        "in_progress",
        "done"
    };

    public static bool IsValid(string status) => AllowedStatuses.Contains(status);
}
