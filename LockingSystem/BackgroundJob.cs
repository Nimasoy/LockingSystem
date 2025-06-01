namespace LockingSystem
{
    public class BackgroundJob
    {
        public string Id { get; }
        public Func<Task> TaskToRun { get; }

        public BackgroundJob(string id, Func<Task> task)
        {
            Id = id;
            TaskToRun = task;
        }
    }

    public interface IBackgroundJobQueue
    {
        void Enqueue(BackgroundJob job);
        bool TryDequeue(out BackgroundJob? job);
        bool Contains(string jobId);
    }

    public class BackgroundJobQueue : IBackgroundJobQueue
    {
        private readonly Queue<BackgroundJob> _jobs = new();
        private readonly HashSet<string> _jobIds = new();
        private readonly object _lock = new();

        public void Enqueue(BackgroundJob job)
        {
            lock (_lock)
            {
                if (_jobIds.Contains(job.Id)) return; // Prevent duplicate
                _jobs.Enqueue(job);
                _jobIds.Add(job.Id);
            }
        }

        public bool TryDequeue(out BackgroundJob? job)
        {
            lock (_lock)
            {
                if (_jobs.Count > 0)
                {
                    job = _jobs.Dequeue();
                    _jobIds.Remove(job.Id);
                    return true;
                }
                job = null;
                return false;
            }
        }

        public bool Contains(string jobId)
        {
            lock (_lock)
            {
                return _jobIds.Contains(jobId);
            }
        }
    }
}
