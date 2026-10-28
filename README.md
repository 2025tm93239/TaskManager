# Task Manager (Assignment Q1)

gRPC task manager in C# — server and console client.

## Project layout

```
TaskManager/
├── TaskManager.slnx
├── TaskManager.Service/   (gRPC server, in-memory store)
│   ├── Protos/task.proto
│   ├── Services/
│   └── Program.cs
├── TaskManager.Client/
└── Screenshots and Demo video/
```

## How to run

From `TaskManager\TaskManager`:

```powershell
# Terminal 1 — server (http://localhost:5081)
dotnet run --project TaskManager.Service

# Terminal 2 — client
dotnet run --project TaskManager.Client
```

Build:

```powershell
dotnet build TaskManager.slnx
```

### Deadline timeout demo

Server delays `GetTask` by 2 seconds; client uses a 1-second deadline → `DEADLINE_EXCEEDED`.

```powershell
dotnet run --project TaskManager.Service --launch-profile http-deadline-demo
# second terminal:
dotnet run --project TaskManager.Client
```

In Visual Studio: run **TaskManager.Service** with profile **`http-deadline-demo`**, then run **TaskManager.Client**.

## What is implemented

- CreateTask, GetTask, ListTasks (server stream), UpdateTask, StreamTaskUpdates (bidirectional)
- In-memory tasks, auto-increment IDs
- Status: `pending`, `in_progress`, `done`
- GetTask `id < 1` → `INVALID_ARGUMENT`; missing task → `NOT_FOUND`; bad status → `FAILED_PRECONDITION`

## Demo video

Local file: [`Screenshots and Demo video/Question 1 Short Demos Recording.mp4`](./Screenshots%20and%20Demo%20video/Question%201%20Short%20Demos%20Recording.mp4)

## Screenshots

Folder: [`Screenshots and Demo video/`](./Screenshots%20and%20Demo%20video/)

| | |
|:---:|:---:|
| ![Picture 1](./Screenshots%20and%20Demo%20video/Picture1.png) | ![Picture 2](./Screenshots%20and%20Demo%20video/Picture2.png) |
| ![Picture 3](./Screenshots%20and%20Demo%20video/Picture3.png) | ![Picture 4](./Screenshots%20and%20Demo%20video/Picture4.png) |
| ![Picture 5](./Screenshots%20and%20Demo%20video/Picture5.png) | ![Picture 6](./Screenshots%20and%20Demo%20video/Picture6.png) |
| ![Picture 7](./Screenshots%20and%20Demo%20video/Picture7.png) | ![Picture 8](./Screenshots%20and%20Demo%20video/Picture8.png) |

![Picture 33](./Screenshots%20and%20Demo%20video/Picture33.png)
