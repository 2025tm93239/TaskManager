# Task Manager (Assignment Q1)

gRPC task manager in C# - server and console client.

## How to run

1. Open terminal and start server:
   ```TaskManager/
├── TaskManager.slnx
├── TaskManager.Service/
│   ├── Protos/task.proto
│   ├── Services/
│   ├── Storage/
│   └── Program.cs
└── TaskManager.Client/
    └── Program.cs
   cd TaskManager
   dotnet run --project TaskManager.Service
   ```
   Server listens on http://localhost:5081

2. Open another terminal and run client:
   ```
   dotnet run --project TaskManager.Client
   ```

## Build

```
dotnet build TaskManager.slnx
```
**Visual Studio — timeout screenshot**

1. Run **TaskManager.Service** with launch profile **`http-deadline-demo`** (2s delay on `GetTask`).
2. Run **TaskManager.Client** (default profile). Section *Fetch task #2* should print `DEADLINE_EXCEEDED`.

## Deadline timeout demo

Assignment needs client GetTask with 1 second deadline while server waits 2 seconds.

Run server with launch profile `http-deadline-demo` (sets 2 sec delay on GetTask):
```
dotnet run --project TaskManager.Service --launch-profile http-deadline-demo
```

Then run the client - the GetTask step should show DEADLINE_EXCEEDED.

## What is implemented

- CreateTask, GetTask, ListTasks (server stream), UpdateTask, StreamTaskUpdates (bidirectional)
- Tasks stored in memory with auto increment id
- Status must be pending, in_progress or done
- GetTask id less than 1 -> INVALID_ARGUMENT
- Task not found -> NOT_FOUND
- Bad status -> FAILED_PRECONDITION
