using AMSEmailLogParser.Utilities;
using FluentAssertions;
using Xunit;

namespace AMSEmailLogParser.Tests.Utilities
{
    public class FormattingTests
    {
        [Theory]
        [InlineData("192.168.1.1", "192.168.001.001")]
        [InlineData("10.0.0.1", "010.000.000.001")]
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("8.8.8.8", "008.008.008.008")]
        public void FormatIPAddress_ShouldFormatValidIPv4Addresses(string input, string expected)
        {
            // Act
            var result = Formatting.FormatIPAddress(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("::1", "0000:0000:0000:0000:0000:0000:0000:0001")]
        [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", "2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
        [InlineData("fe80::1ff:fe23:4567:890a", "fe80:0000:0000:0000:01ff:fe23:4567:890a")]
        [InlineData("::", "0000:0000:0000:0000:0000:0000:0000:0000")]
        public void FormatIPAddress_ShouldFormatValidIPv6Addresses(string input, string expected)
        {
            // Act
            var result = Formatting.FormatIPAddress(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("InvalidIPAddress")]
        [InlineData("")]
        [InlineData(null)]
        public void FormatIPAddress_ShouldReturnOriginalStringForInvalidOrEmptyInput(string input)
        {
            // Act
            var result = Formatting.FormatIPAddress(input);

            // Assert
            result.Should().Be(input);
        }
    }
}
