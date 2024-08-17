using AMSEmailLogParser.Utilities;
using FluentAssertions;
using Xunit;

namespace AMSEmailLogParser.Tests.Utilities
{
    public class ExtensionsTests
    {
        [Theory]
        [InlineData("Some error message\0 with null char", "Some error message with null char")]
        [InlineData("\0\0Null chars everywhere\0", "Null chars everywhere")]
        public void RemoveNullChars_ShouldRemoveAllNullCharacters(string input, string expected)
        {
            // Act
            var result = input.RemoveNullChars();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("Some error\n\nmessage", "Some error\nmessage")]
        [InlineData("Line1\n\nLine2\n\nLine3", "Line1\nLine2\nLine3")]
        public void DoubleToSingleLinefeed_ShouldReplaceDoubleLinefeedsWithSingle(string input, string expected)
        {
            // Arrange
            string normalizedInput = input.Replace("\n", Environment.NewLine);
            string normalizedExpected = expected.Replace("\n", Environment.NewLine);

            // Act
            var result = normalizedInput.DoubleToSingleLinefeed();

            // Assert
            result.Should().Be(normalizedExpected);
        }

        [Theory]
        [InlineData("Some 'error' message\nwith new lines", "Some ''error'' message|with new lines")]
        [InlineData("Another 'test' message", "Another ''test'' message")]
        public void FormatExceptionMessageForDb_ShouldFormatMessageCorrectly(string input, string expected)
        {
            // Arrange
            string normalizedInput = input.Replace("\n", Environment.NewLine);
            string normalizedExpected = expected.Replace("|", Environment.NewLine.Replace(Environment.NewLine, "|"));

            // Act
            var result = normalizedInput.FormatExceptionMessageForDb();

            // Assert
            result.Should().Be(normalizedExpected);
        }
    }
}
