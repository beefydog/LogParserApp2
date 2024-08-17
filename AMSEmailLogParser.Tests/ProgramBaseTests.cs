using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using AMSEmailLogParser;

public class ProgramBaseTests
{
    [Fact]
    public void ParseArguments_ShouldHandleSingleFileType()
    {
        // Arrange
        var args = new[] { "-filetype", "smtp" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("filetype");
        result["filetype"].Should().ContainSingle().Which.Should().Be("smtp");
    }

    [Fact]
    public void ParseArguments_ShouldHandleMultipleFileTypes()
    {
        // Arrange
        var args = new[] { "-filetype", "smtp,pop3,imap4" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("filetype");
        result["filetype"].Should().BeEquivalentTo(new List<string> { "smtp", "pop3", "imap4" });
    }

    [Fact]
    public void ParseArguments_ShouldHandlePostProcessWithArchivePath()
    {
        // Arrange
        var args = new[] { "-postprocess", "archive", "C:\\archivepath" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("postprocess");
        result["postprocess"].Should().ContainSingle().Which.Should().Be("archive");
        result.Should().ContainKey("archivepath");
        result["archivepath"].Should().ContainSingle().Which.Should().Be("C:\\archivepath");
    }

    [Fact]
    public void ParseArguments_ShouldHandleAfterFlag()
    {
        // Arrange
        var args = new[] { "-after" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("after");
        result["after"].Should().ContainSingle().Which.Should().Be("true");
    }

    [Fact]
    public void ParseArguments_ShouldHandleQuietModeFlag()
    {
        // Arrange
        var args = new[] { "-q" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("q");
        result["q"].Should().ContainSingle().Which.Should().Be("true");
    }

    [Fact]
    public void ParseArguments_ShouldHandleMixedArguments()
    {
        // Arrange
        var args = new[] { "-filetype", "smtp,pop3", "-postprocess", "archive", "C:\\archivepath", "-after", "-q" };

        // Act
        var result = ProgramBase.ParseArguments(args);

        // Assert
        result.Should().ContainKey("filetype");
        result["filetype"].Should().BeEquivalentTo(new List<string> { "smtp", "pop3" });
        result.Should().ContainKey("postprocess");
        result["postprocess"].Should().ContainSingle().Which.Should().Be("archive");
        result.Should().ContainKey("archivepath");
        result["archivepath"].Should().ContainSingle().Which.Should().Be("C:\\archivepath");
        result.Should().ContainKey("after");
        result["after"].Should().ContainSingle().Which.Should().Be("true");
        result.Should().ContainKey("q");
        result["q"].Should().ContainSingle().Which.Should().Be("true");
    }
}
