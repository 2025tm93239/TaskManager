# Task Manager

gRPC-based Task Manager **service** (`TaskManager.Service`) with console client (`TaskManager.Client`).

## Structure

```
TaskManager/
├── TaskManager.slnx
├── TaskManager.Service/
│   ├── Protos/task.proto
│   ├── Services/
│   ├── Storage/
│   └── Program.cs
└── TaskManager.Client/
    └── Program.cs
```

## Build

```bash
cd TaskManager
dotnet build TaskManager.slnx
```

## Run

**Terminal 1 — service:**

```bash
dotnet run --project TaskManager.Service --launch-profile http
```

**Terminal 2 — client:**

```bash
dotnet run --project TaskManager.Client
```

Service URL: `http://localhost:5081` (HTTP/2).

## Assignment Q1 — error codes and deadline (already implemented)

| Requirement | Server (`TaskManagerService.cs`) | Client (`Program.cs`) |
|-------------|----------------------------------|------------------------|
| `GetTask` id &lt; 1 → `INVALID_ARGUMENT` | `GetTask` throws `InvalidArgument` | `GetTask` with `id=0` |
| Task not found → `NOT_FOUND` | `GetTask` / `UpdateTask` | `GetTask` with `id=9999` |
| Invalid status → `FAILED_PRECONDITION` | `UpdateTask`, `StreamTaskUpdates` | `UpdateTask` with `status=blocked` |
| `GetTask` with **1s deadline** | — | Fetch task #2 uses `CallOptions(deadline: …)` |
| Server sleeps **2s**, client handles timeout | `TaskService:GetTaskDelaySeconds` | Same fetch #2 catches `DeadlineExceeded` |

**Visual Studio — timeout screenshot**

1. Run **TaskManager.Service** with launch profile **`http-deadline-demo`** (2s delay on `GetTask`).
2. Run **TaskManager.Client** (default profile). Section *Fetch task #2* should print `DEADLINE_EXCEEDED`.

**CLI — deadline demo**

```bash
dotnet run --project TaskManager.Service --launch-profile http-deadline-demo
dotnet run --project TaskManager.Client
```

Optional second deadline call: client profile **`deadline-demo`** or `--demo-deadline`.

## Assignment coverage

- RPCs: `CreateTask`, `GetTask`, `ListTasks` (server stream), `UpdateTask`, `StreamTaskUpdates` (bidirectional)
- In-memory store, incremental IDs, status validation (`pending`, `in_progress`, `done`)
- Errors: `INVALID_ARGUMENT` (id &lt; 1), `NOT_FOUND`, `FAILED_PRECONDITION`
