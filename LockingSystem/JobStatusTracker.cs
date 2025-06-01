using System.Collections.Concurrent;

namespace LockingSystem
{
    public class JobStatus
    {
        public string ?Id { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    public interface IJobStatusTracker
    {
        void Start(string jobId);
        void Complete(string jobId);
        void Fail(string jobId, string error);
        IReadOnlyCollection<JobStatus> GetAll();
    }

    public class JobStatusTracker : IJobStatusTracker
    {
        private readonly ConcurrentDictionary<string, JobStatus> _statuses = new();

        public void Start(string jobId)
        {
            _statuses[jobId] = new JobStatus
            {
                Id = jobId,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };
        }

        public void Complete(string jobId)
        {
            if (_statuses.TryGetValue(jobId, out var status))
            {
                status.Status = "Completed";
                status.CompletedAt = DateTime.UtcNow;
            }
        }

        public void Fail(string jobId, string error)
        {
            if (_statuses.TryGetValue(jobId, out var status))
            {
                status.Status = "Failed";
                status.Error = error;
                status.CompletedAt = DateTime.UtcNow;
            }
        }

        public IReadOnlyCollection<JobStatus> GetAll()
        {
            return _statuses.Values.ToList().AsReadOnly();
        }
    }
}
