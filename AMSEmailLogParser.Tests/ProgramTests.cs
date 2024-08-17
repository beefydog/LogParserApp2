using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AMSEmailLogParser;
using AMSEmailLogParser.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;

[TestCaseOrderer("PriorityOrderer", "AMSEmailLogParser.Tests")]
public class ProgramTests
{
    [Fact, TestPriority(1)]
    public async Task ProcessFileAsync_ShouldReturnTrue_WhenProcessingSucceeds()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string uniqueTestDirectory = Path.Combine(baseDirectory, "logs", Guid.NewGuid().ToString());
        string logDirectory = Path.Combine(uniqueTestDirectory, "logs");
        string archiveDirectory = Path.Combine(logDirectory, "archive");

        Directory.CreateDirectory(archiveDirectory);
        string filePath = Path.Combine(logDirectory, "test.log");
        Directory.CreateDirectory(logDirectory);
        await File.WriteAllTextAsync(filePath, "Sample log content");

        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestLogDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using (var dbContext = new LogDbContext(options))
        {
            var testLogger = new Mock<ILogger<LogFileProcessor>>();
            var logFileProcessor = new LogFileProcessor(dbContext, testLogger.Object);

            var services = new ServiceCollection();
            services.AddSingleton(logFileProcessor);
            services.AddSingleton(dbContext);
            var serviceProvider = services.BuildServiceProvider();

            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);

            var processedFiles = new ConcurrentBag<string>();
            var semaphore = new SemaphoreSlim(1, 1);

            // Act
            var result = await Program.ProcessFileAsync(
                filePath,
                mockHost.Object,
                archiveDirectory,
                "archive",
                false,
                processedFiles,
                semaphore
            );

            // Assert
            result.Should().BeTrue();
        }

        // Cleanup
        if (Directory.Exists(uniqueTestDirectory))
        {
            Directory.Delete(uniqueTestDirectory, true);
        }
    }

    [Fact, TestPriority(2)]
    public async Task ProcessFileAsync_ShouldReturnFalse_WhenProcessingFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestLogDb_{Guid.NewGuid()}")
            .Options;

        var dbContext = new LogDbContext(options);
        var testLogger = new Mock<ILogger<LogFileProcessor>>();
        var logFileProcessor = new LogFileProcessor(dbContext, testLogger.Object);

        var services = new ServiceCollection();
        services.AddSingleton(logFileProcessor);
        services.AddSingleton(dbContext);
        var serviceProvider = services.BuildServiceProvider();

        var mockHost = new Mock<IHost>();
        mockHost.Setup(h => h.Services).Returns(serviceProvider);

        var processedFiles = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(1, 1);

        // Act
        var result = await Program.ProcessFileAsync(
            "nonexistent.log",
            mockHost.Object,
            "archivePath",
            "archive",
            false,
            processedFiles,
            semaphore
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact, TestPriority(4)]
    public async Task ProcessFileAsync_ShouldAddToProcessedFiles_WhenAfterProcessingIsTrue()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string uniqueTestDirectory = Path.Combine(baseDirectory, "logs", Guid.NewGuid().ToString());
        string logDirectory = Path.Combine(uniqueTestDirectory, "logs");
        string archiveDirectory = Path.Combine(logDirectory, "archive");

        Directory.CreateDirectory(archiveDirectory);
        string filePath = Path.Combine(logDirectory, "test.log");
        Directory.CreateDirectory(logDirectory);
        await File.WriteAllTextAsync(filePath, "Sample log content");

        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestLogDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using (var dbContext = new LogDbContext(options))
        {
            var testLogger = new Mock<ILogger<LogFileProcessor>>();
            var logFileProcessor = new LogFileProcessor(dbContext, testLogger.Object);

            var services = new ServiceCollection();
            services.AddSingleton(logFileProcessor);
            services.AddSingleton(dbContext);
            var serviceProvider = services.BuildServiceProvider();

            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);

            var processedFiles = new ConcurrentBag<string>();
            var semaphore = new SemaphoreSlim(1, 1);

            // Act
            var result = await Program.ProcessFileAsync(
                filePath,
                mockHost.Object,
                archiveDirectory,
                "archive",
                true,
                processedFiles,
                semaphore
            );

            // Assert
            result.Should().BeTrue("the file processing should succeed");
            processedFiles.Should().Contain(filePath, "the file should be added to processedFiles");
        }

        // Cleanup
        if (Directory.Exists(uniqueTestDirectory))
        {
            Directory.Delete(uniqueTestDirectory, true);
        }
    }

    [Fact, TestPriority(3)]
    public async Task ProcessFileAsync_ShouldNotAddToProcessedFiles_WhenProcessingFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestLogDb_{Guid.NewGuid()}")
            .Options;

        var dbContext = new LogDbContext(options);
        var testLogger = new Mock<ILogger<LogFileProcessor>>();
        var logFileProcessor = new LogFileProcessor(dbContext, testLogger.Object);

        var services = new ServiceCollection();
        services.AddSingleton(logFileProcessor);
        services.AddSingleton(dbContext);
        var serviceProvider = services.BuildServiceProvider();

        var mockHost = new Mock<IHost>();
        mockHost.Setup(h => h.Services).Returns(serviceProvider);

        var processedFiles = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(1, 1);

        // Act
        var result = await Program.ProcessFileAsync(
            "nonexistent.log",
            mockHost.Object,
            "archivePath",
            "archive",
            true,
            processedFiles,
            semaphore
        );

        // Assert
        result.Should().BeFalse();
        processedFiles.Should().BeEmpty();
    }
}
