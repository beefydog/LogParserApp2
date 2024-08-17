using AMSEmailLogParser.Utilities;
using FluentAssertions;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AMSEmailLogParser.Tests.Utilities
{
    public class HashingTests : IDisposable
    {
        private readonly string _testDirectory;

        public HashingTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "HashingTests");
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task ComputeSha256HashAsync_ShouldReturnCorrectHashForKnownFile()
        {
            // Arrange
            string filePath = CreateTestFile("TestFile.txt", "Hello, World!");
            string expectedHash;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("Hello, World!"));
                expectedHash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
            }

            // Act
            var result = await Hashing.ComputeSha256HashAsync(filePath);

            // Assert
            result.Should().Be(expectedHash);
        }

        [Fact]
        public async Task ComputeSha256HashAsync_ShouldReturnCorrectHashForEmptyFile()
        {
            // Arrange
            string filePath = CreateTestFile("EmptyFile.txt", string.Empty);
            string expectedHash;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Array.Empty<byte>());
                expectedHash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
            }

            // Act
            var result = await Hashing.ComputeSha256HashAsync(filePath);

            // Assert
            result.Should().Be(expectedHash);
        }

        [Fact]
        public async Task ComputeSha256HashAsync_ShouldThrowFileNotFoundExceptionForNonExistentFile()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "NonExistentFile.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => Hashing.ComputeSha256HashAsync(filePath));
        }

        private string CreateTestFile(string fileName, string content)
        {
            string filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}
