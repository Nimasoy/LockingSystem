using Xunit;
using System.Linq;

namespace LockingSystem.Tests
{
    public class JobStatusTrackerTests
    {
        [Fact]
        public void Start_ShouldSetCorrectStatus()
        {
            // Arrange
            var tracker = new JobStatusTracker();
            var jobId = "test-job";

            // Act
            tracker.Start(jobId);

            // Assert
            var status = tracker.GetAll().First();
            Assert.Equal(jobId, status.Id);
            Assert.Equal("Running", status.Status);
            Assert.NotNull(status.StartedAt);
            Assert.Null(status.CompletedAt);
            Assert.Null(status.Error);
        }

        [Fact]
        public void Complete_ShouldUpdateStatus()
        {
            // Arrange
            var tracker = new JobStatusTracker();
            var jobId = "test-job";
            tracker.Start(jobId);

            // Act
            tracker.Complete(jobId);

            // Assert
            var status = tracker.GetAll().First();
            Assert.Equal("Completed", status.Status);
            Assert.NotNull(status.CompletedAt);
            Assert.Null(status.Error);
        }

        [Fact]
        public void Fail_ShouldUpdateStatusWithError()
        {
            // Arrange
            var tracker = new JobStatusTracker();
            var jobId = "test-job";
            var error = "Test error";
            tracker.Start(jobId);

            // Act
            tracker.Fail(jobId, error);

            // Assert
            var status = tracker.GetAll().First();
            Assert.Equal("Failed", status.Status);
            Assert.Equal(error, status.Error);
            Assert.NotNull(status.CompletedAt);
        }

        [Fact]
        public void Complete_NonExistentJob_ShouldNotThrow()
        {
            // Arrange
            var tracker = new JobStatusTracker();

            // Act & Assert
            var exception = Record.Exception(() => tracker.Complete("non-existent"));
            Assert.Null(exception);
        }

        [Fact]
        public void Fail_NonExistentJob_ShouldNotThrow()
        {
            // Arrange
            var tracker = new JobStatusTracker();

            // Act & Assert
            var exception = Record.Exception(() => tracker.Fail("non-existent", "error"));
            Assert.Null(exception);
        }

        [Fact]
        public void GetAll_ShouldReturnAllJobs()
        {
            // Arrange
            var tracker = new JobStatusTracker();
            tracker.Start("job1");
            tracker.Start("job2");
            tracker.Complete("job1");
            tracker.Fail("job2", "error");

            // Act
            var allJobs = tracker.GetAll();

            // Assert
            Assert.Equal(2, allJobs.Count);
            Assert.Contains(allJobs, j => j.Id == "job1" && j.Status == "Completed");
            Assert.Contains(allJobs, j => j.Id == "job2" && j.Status == "Failed");
        }

        [Fact]
        public void Status_ShouldTrackTimeCorrectly()
        {
            // Arrange
            var tracker = new JobStatusTracker();
            var jobId = "test-job";

            // Act
            tracker.Start(jobId);
            var startTime = DateTime.UtcNow;
            tracker.Complete(jobId);
            var endTime = DateTime.UtcNow;

            // Assert
            var status = tracker.GetAll().First();
            Assert.True(status.StartedAt <= startTime);
            Assert.True(status.CompletedAt >= startTime);
            Assert.True(status.CompletedAt <= endTime);
        }
    }
} 