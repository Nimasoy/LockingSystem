using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LockingSystem
{
    public class BackgroundJobProcessor : BackgroundService
    {
        private readonly IBackgroundJobQueue _jobQueue;
        private readonly DistributedLockService _lockService;
        private readonly ILogger<BackgroundJobProcessor> _logger;
        private readonly IJobStatusTracker _statusTracker;

        public BackgroundJobProcessor(IBackgroundJobQueue jobQueue, DistributedLockService lockService, ILogger<BackgroundJobProcessor> logger, IJobStatusTracker statusTracker)
        {
            _jobQueue = jobQueue;
            _lockService = lockService;
            _logger = logger;
            _statusTracker = statusTracker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Job Processor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_jobQueue.TryDequeue(out var job) && job != null)
                    {
                        _statusTracker.Start(job.Id);
    
                        bool acquired = await _lockService.ExecuteWithLockAsync($"job:{job.Id}", async () =>
                        {
                            try
                            {
                                _logger.LogInformation($"Executing job: {job.Id}");
                                await job.TaskToRun();
                                _logger.LogInformation($"Completed job: {job.Id}");
                                _statusTracker.Complete(job.Id);
                            }
                            catch (Exception ex)
                            {
                                _statusTracker.Fail(job.Id, ex.Message);
                                _logger.LogError(ex, $"Job {job.Id} failed.");
                            }
                        });

                        if (!acquired)
                        {
                            _statusTracker.Fail(job.Id, "Lock not acquired.");
                            _logger.LogWarning($"Could not acquire lock for job: {job.Id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job.");
                }
                try
                {
                    await Task.Delay(1000, stoppingToken); // delay interval
                }
                catch (TaskCanceledException)
                {
                    // exit when cancellation is requested during delay
                    break;
                }
            }

            _logger.LogInformation("Background Job Processor stopping.");
        }
    }
}
