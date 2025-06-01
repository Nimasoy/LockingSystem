using Xunit;
using System.Threading.Tasks;

namespace LockingSystem.Tests
{
    public class BackgroundJobQueueTests
    {
        [Fact]
        public void Enqueue_ShouldAddJobToQueue()
        {
            // Arrange
            var queue = new BackgroundJobQueue();
            var job = new BackgroundJob("test-job", () => Task.CompletedTask);

            // Act
            queue.Enqueue(job);

            // Assert
            Assert.True(queue.Contains("test-job"));
        }

        [Fact]
        public void Enqueue_DuplicateJobId_ShouldNotAddToQueue()
        {
            // Arrange
            var queue = new BackgroundJobQueue();
            var job1 = new BackgroundJob("test-job", () => Task.CompletedTask);
            var job2 = new BackgroundJob("test-job", () => Task.CompletedTask);

            // Act
            queue.Enqueue(job1);
            queue.Enqueue(job2);

            // Assert
            Assert.True(queue.Contains("test-job"));
            Assert.True(queue.TryDequeue(out var dequeuedJob));
            Assert.False(queue.TryDequeue(out _)); // Should be empty after first dequeue
        }

        [Fact]
        public void TryDequeue_EmptyQueue_ShouldReturnFalse()
        {
            // Arrange
            var queue = new BackgroundJobQueue();

            // Act
            var result = queue.TryDequeue(out var job);

            // Assert
            Assert.False(result);
            Assert.Null(job);
        }

        [Fact]
        public void TryDequeue_WithJobs_ShouldReturnFirstJob()
        {
            // Arrange
            var queue = new BackgroundJobQueue();
            var job1 = new BackgroundJob("job1", () => Task.CompletedTask);
            var job2 = new BackgroundJob("job2", () => Task.CompletedTask);
            queue.Enqueue(job1);
            queue.Enqueue(job2);

            // Act
            var result = queue.TryDequeue(out var dequeuedJob);

            // Assert
            Assert.True(result);
            Assert.Equal("job1", dequeuedJob?.Id);
            Assert.False(queue.Contains("job1"));
            Assert.True(queue.Contains("job2"));
        }

        [Fact]
        public void Contains_NonExistentJob_ShouldReturnFalse()
        {
            // Arrange
            var queue = new BackgroundJobQueue();

            // Act
            var result = queue.Contains("non-existent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Queue_ShouldMaintainFIFOOrder()
        {
            // Arrange
            var queue = new BackgroundJobQueue();
            var job1 = new BackgroundJob("job1", () => Task.CompletedTask);
            var job2 = new BackgroundJob("job2", () => Task.CompletedTask);
            var job3 = new BackgroundJob("job3", () => Task.CompletedTask);

            // Act
            queue.Enqueue(job1);
            queue.Enqueue(job2);
            queue.Enqueue(job3);

            // Assert
            Assert.True(queue.TryDequeue(out var first));
            Assert.Equal("job1", first?.Id);

            Assert.True(queue.TryDequeue(out var second));
            Assert.Equal("job2", second?.Id);

            Assert.True(queue.TryDequeue(out var third));
            Assert.Equal("job3", third?.Id);

            Assert.False(queue.TryDequeue(out _));
        }
    }
} 