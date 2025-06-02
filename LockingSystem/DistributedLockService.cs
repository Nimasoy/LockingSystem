using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System.Net;

namespace LockingSystem
{

    public class DistributedLockService
    {
        private readonly RedLockFactory _redLockFactory;
        private readonly IList<RedLockEndPoint> _redlockEndpoints;

        public DistributedLockService()
        {
            _redlockEndpoints = new List<RedLockEndPoint>
            {
                new DnsEndPoint("localhost", 6379),
                new DnsEndPoint("localhost", 6380),
                new DnsEndPoint("localhost", 6381)
            };

            _redLockFactory = RedLockFactory.Create(_redlockEndpoints);
        }

        public async Task<bool> ExecuteWithLockAsync(string resource, Func<Task> action)
        {
            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentException("Resource cannot be empty", nameof(resource));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            TimeSpan expiry = TimeSpan.FromSeconds(30);         // Lock expires after 30 seconds
            TimeSpan wait = TimeSpan.FromSeconds(5);            // Wait max 5 seconds to acquire lock
            TimeSpan retry = TimeSpan.FromMilliseconds(100);    // Retry every 100ms

            using var cts = new CancellationTokenSource(expiry);
            using var redLock = await _redLockFactory.CreateLockAsync(resource, expiry, wait, retry);

            if (redLock.IsAcquired)
            {
                try
                {
                    var actionTask = action();
                    var timeoutTask = Task.Delay(expiry, cts.Token);
                    
                    var completedTask = await Task.WhenAny(actionTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine("Action execution timed out.");
                        return false;
                    }

                    await actionTask; // tell us any exceptions
                    return true;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Action execution was cancelled due to timeout.");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lock action failed: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Could not acquire lock.");
                return false;
            }
        }
    }
}
