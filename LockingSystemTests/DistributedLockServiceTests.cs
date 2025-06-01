using Xunit;
using System;
using System.Threading.Tasks;
using Moq;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace LockingSystem.Tests
{
    public class DistributedLockServiceTests
    {
        [Fact]
        public async Task ExecuteWithLockAsync_SuccessfulExecution_ShouldReturnTrue()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "test-resource";
            var executionCount = 0;

            // Act
            var result = await service.ExecuteWithLockAsync(resource, async () =>
            {
                executionCount++;
                await Task.CompletedTask;
            });

            // Assert
            Assert.True(result);
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public async Task ExecuteWithLockAsync_FailedExecution_ShouldReturnFalse()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "test-resource";
            var executionCount = 0;

            // Act
            var result = await service.ExecuteWithLockAsync(resource, async () =>
            {
                executionCount++;
                await Task.CompletedTask;
                throw new Exception("Test exception");
            });

            // Assert
            Assert.False(result);
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public async Task ExecuteWithLockAsync_ConcurrentExecution_ShouldPreventOverlap()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "test-resource";
            var executionCount = 0;
            var executionTimes = new List<DateTime>();

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(service.ExecuteWithLockAsync(resource, async () =>
                {
                    executionCount++;
                    executionTimes.Add(DateTime.UtcNow);
                    await Task.Delay(100); // Simulate some work
                }));
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, executionCount);
            Assert.Equal(3, executionTimes.Count);

            // Verify that executions didn't overlap
            for (int i = 1; i < executionTimes.Count; i++)
            {
                var timeDiff = executionTimes[i] - executionTimes[i - 1];
                Assert.True(timeDiff.TotalMilliseconds >= 100);
            }
        }

        [Fact]
        public async Task ExecuteWithLockAsync_Timeout_ShouldReturnFalse()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "test-resource";
            var executionCount = 0;

            // Act
            var result = await service.ExecuteWithLockAsync(resource, async () =>
            {
                executionCount++;
                await Task.Delay(TimeSpan.FromSeconds(35)); // Longer than lock expiry
            });

            // Assert
            Assert.False(result);
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public async Task ExecuteWithLockAsync_NullAction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "test-resource";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.ExecuteWithLockAsync(resource, null!));
        }

        [Fact]
        public async Task ExecuteWithLockAsync_EmptyResource_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new DistributedLockService();
            var resource = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ExecuteWithLockAsync(resource, () => Task.CompletedTask));
        }
    }
} 