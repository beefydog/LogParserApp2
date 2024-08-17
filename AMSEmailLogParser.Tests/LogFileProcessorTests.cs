using AMSEmailLogParser.Data;
using AMSEmailLogParser.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;

namespace AMSEmailLogParser.Tests
{
    public class LogFileProcessorTests
    {
        private readonly LogDbContext _dbContext;
        private readonly TestLogger<LogFileProcessor> _testLogger;
        private readonly LogFileProcessor _logFileProcessor;

        public LogFileProcessorTests()
        {
            var options = new DbContextOptionsBuilder<LogDbContext>()
                .UseInMemoryDatabase(databaseName: "TestLogDb")
                .Options;

            _dbContext = new LogDbContext(options);
            _testLogger = new TestLogger<LogFileProcessor>();
            _logFileProcessor = new LogFileProcessor(_dbContext, _testLogger);
        }

        [Fact]
        public async Task ProcessLogFileAsync_ShouldReturnFalse_WhenFileNotFoundExceptionIsThrown()
        {
            // Arrange
            string filePath = "nonexistentfile.log";

            // Act
            var result = await _logFileProcessor.ProcessLogFileAsync(filePath);

            // Assert
            result.Should().BeFalse();
            _testLogger.Logs.Should().Contain(log => log.Contains("File not found:"));
        }

        [Fact]
        public async Task ProcessLogFileAsync_ShouldReturnFalse_WhenLogFileAlreadyProcessed()
        {
            // Arrange
            string filePath = "logfile.log";
            string fileName = Path.GetFileName(filePath);

            _dbContext.ParsedLogs.Add(new ParsedLog { FileName = fileName });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _logFileProcessor.ProcessLogFileAsync(filePath);

            // Assert
            result.Should().BeFalse();
            _testLogger.Logs.Should().Contain(log => log.Contains("Log file already processed:"));
        }

        [Fact]
        public void ExtractFileType_ShouldReturnCorrectFileType()
        {
            // Arrange
            string fileNameNoExt = "logfile_1234";

            // Act
            var fileType = LogFileProcessor.ExtractFileType(fileNameNoExt);

            // Assert
            fileType.Should().Be("logfile");
        }

        [Fact]
        public void ExtractLogFileId_ShouldReturnCorrectLogFileId()
        {
            // Arrange
            string fileNameNoExt = "logfile_1234";

            // Act
            var logFileId = LogFileProcessor.ExtractLogFileId(fileNameNoExt);

            // Assert
            logFileId.Should().Be(1234);
        }

        [Fact]
        public void ParseLine_ShouldReturnNull_WhenLineIsInvalid()
        {
            // Arrange
            string invalidLine = "Invalid log line";

            // Act
            var result = LogFileProcessor.ParseLine(invalidLine, 1, 1);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseLine_ShouldReturnLogEntry_WhenLineIsValid()
        {
            // Arrange
            string validLine = "Sun, 01 Jan 2023 12:00:00 -> 192.168.1.1 -> Success: Action=[Login], Details=[User logged in]";
            int parsedLogId = 1;
            int lineNum = 1;

            // Act
            var logEntry = LogFileProcessor.ParseLine(validLine, parsedLogId, lineNum);

            // Assert
            logEntry.Should().NotBeNull();
            logEntry!.ParsedLogId.Should().Be(parsedLogId);
            logEntry.LineNum.Should().Be(lineNum);
            logEntry.IPaddress.Should().Be("192.168.001.001");
            logEntry.Status.Should().Be("Success");
            logEntry.Action.Should().Be("Login");
            logEntry.Details.Should().Be("User logged in");
        }
    }
}
