namespace GitTool;

public class Queue : IDisposable
{
    private readonly Action<int> _updateCallback;
    private IList<Task> _taskList = new List<Task>();
    private CancellationTokenSource? _token;
    private Task _worker;

    public Queue(Action<int> updateCallback)
    {
        _updateCallback = updateCallback;
        var taskFactory = new TaskFactory();
        _token?.Cancel();
        _token = new CancellationTokenSource();
        _worker = taskFactory.StartNew(WorkerTask, TaskCreationOptions.LongRunning);
    }

    public async Task Enqueue(Action action)
    {
        var task = new Task(action);
        _taskList.Add(task);
        _updateCallback(_taskList.Count);
        await task;
    }

    public void WorkerTask()
    {
        while (_token?.IsCancellationRequested == false)
        {
            var task = _taskList.FirstOrDefault();
            if (task != null)
            {
                task.Start();
                task.Wait();
                _taskList.Remove(task);
                _updateCallback(_taskList.Count);
            }
        }
    }

    public void Dispose()
    {
        _token?.Cancel();
        _worker.Dispose();
    }
}