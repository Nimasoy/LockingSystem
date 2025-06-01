using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace LockingSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<MyDbContext>(options => options.UseInMemoryDatabase("TestDb"));
                    services.AddSingleton<IJobStatusTracker, JobStatusTracker>();
                    services.AddSingleton<DistributedLockService>();
                    services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
                    services.AddHostedService<BackgroundJobProcessor>();
                })
                .Build();

            var lockService = host.Services.GetRequiredService<DistributedLockService>();
            var db = host.Services.GetRequiredService<MyDbContext>();

            await lockService.ExecuteWithLockAsync("lock:mydb:transaction", async () =>
            {
                Console.WriteLine("Lock acquired. Saving new user...");

                db.Users.Add(new User { Name = "Nima", Email = "nima@example.com" });
                await db.SaveChangesAsync();

                Console.WriteLine("User saved to DB safely.");
            });

            // background job
            var jobQueue = host.Services.GetRequiredService<IBackgroundJobQueue>();

            jobQueue.Enqueue(new BackgroundJob("job1", async () =>
            {
                Console.WriteLine($"[Job1] Running at {DateTime.Now}");
                await Task.Delay(2000); // simulate work
            }));

            jobQueue.Enqueue(new BackgroundJob("job2", async () =>
            {
                Console.WriteLine($"[Job2] Running at {DateTime.Now}");
                await Task.Delay(1500); // simulate work
            }));
            //duplicate testing
            jobQueue.Enqueue(new BackgroundJob("job1", async () =>
            {
                Console.WriteLine($"[Job1 duplicate] Should be skipped due to duplicate ID");
                await Task.CompletedTask;
            }));

            await host.StartAsync(); //run host so that bj process
            await Task.Delay(5000); //wait until bj process

            var statusTracker = host.Services.GetRequiredService<IJobStatusTracker>();
            foreach (var status in statusTracker.GetAll())
            {
                Console.WriteLine($"Job {status.Id} - Status: {status.Status} | Started: {status.StartedAt} | Completed: {status.CompletedAt} | Error: {status.Error}");
            }

            await host.StopAsync();

        }
    }

}
