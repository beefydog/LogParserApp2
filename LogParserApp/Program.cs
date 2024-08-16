namespace LogParserApp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using LogParserApp.Data;
using LogParserApp.Utilities;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Threading;
using NetTopologySuite.Index.Quadtree;
using System.Runtime.CompilerServices;

internal partial class Program : ProgramBase
{
    public static bool QuietMode { get; set; } = false;

    public static async Task Main(string[] args)
    {
        var settings = ParseArguments(args);

        QuietMode = settings.ContainsKey("q");

        if (!settings.TryGetValue("filetype", out List<string>? value) || value.Count == 0) 
        {
            AnsiConsole.MarkupLine("Please specify at least one filetype using [green3]-filetype \"[gold3]smtp[/],[gold3]pop3[/]\"[/].");
            AnsiConsole.MarkupLine("Valid log file types: [gold3]accounts[/],[gold3]contentfiltering[/],[gold3]imap4[/],[gold3]outmail[/],[gold3]outmailfail[/],[gold3]pop3[/],[gold3]pop3retr[/],[gold3]remoteadmin[/],[gold3]server[/],[gold3]smtp[/],[gold3]webmail[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("To specify the folder path: use the [green3]-folderpath[/] switch followed by [green3]\"[gold3]YourFolderPath[/]\"[/] as in: ");
            AnsiConsole.MarkupLine("[green3]-folderpath \"[yellow3_1]C:\\MyEmailLogs[/]\" [/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("To specify post processing options: use the [green3]-postprocess[/] switch followed by [green3]\"[gold3]archive[/]\"[/], [green3]\"[gold3]delete[/]\"[/] or [green3]\"[gold3]keep[/]\"[/]  ");
            Console.WriteLine();
            AnsiConsole.MarkupLine("If archiving, specify the path after the [green3]\"[gold3]archive[/]\"[/] option as in: ");
            AnsiConsole.MarkupLine("[green3]-postprocess \"[gold3]archive[/]\" \"[yellow3_1]C:\\MyLogArchive[/]\"[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("If you which to perform archive or delete AFTER all files have been processed, use the following:");
            AnsiConsole.MarkupLine("[green3]-postprocess \"[gold3]archive[/]\" \"[yellow3_1]C:\\MyLogArchive[/]\" -after[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("Example with all options specified:");
            AnsiConsole.MarkupLine("[green4]LogParserApp[/] [green3]-filetype \"[gold3]smtp[/],[gold3]pop3[/],[gold3]imap4[/]\" [green3]-folderpath \"[yellow3_1]C:\\MyEmailLogs[/]\" [/] -postprocess \"[gold3]archive[/]\" \"[yellow3_1]C:\\MyLogArchive[/]\" -after[/]");
            AnsiConsole.MarkupLine("This would process [gold3]smtp[/], [gold3]pop3[/] and [gold3]imap4[/] logs, archive them and do so after files parsed successfully.");
            Console.WriteLine();
            AnsiConsole.MarkupLine("The default archive folder path can be specified in the [yellow3_1]appsettings.json[/] file");
            Console.WriteLine();
            AnsiConsole.MarkupLine("Enable quiet mode by appending the [green3]-q[/] switch to disable all output.");

            return;
        }

        var host = CreateHostBuilder(args).Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        string? folderPath = settings.TryGetValue("folderpath", out List<string>? value1) && value1.Count > 0 ? value1[0]
                              : config["LogFileSettings:FolderPath"];

        string? archivePath = settings.TryGetValue("archivepath", out List<string>? value2) && value2.Count > 0 ? value2[0]
                              : config["LogFileSettings:ArchivePath"];

        string postProcess = settings.TryGetValue("postprocess", out List<string>? value3) && value3.Count > 0 ? value3[0].ToLower() : "keep";
        bool afterProcessing = settings.ContainsKey("after");

        ConcurrentBag<string> processedFiles = [];
        var semaphore = new SemaphoreSlim(9); // Limit to 9 concurrent tasks

        foreach (var fileType in value)
        {
            if (!QuietMode)
            {
                AnsiConsole.MarkupLine($"[green3]Processing log type:[/] [yellow3_1]{fileType}[/]");
            }

            var logFiles = Directory.GetFiles(folderPath ?? @"C:\logs", $"{fileType}_*.txt")
                .Select(file => new
                {
                    FileName = file,
                    OrderKey = int.Parse(OrderKeyRegex().Match(Path.GetFileName(file)).Groups[1].Value)
                })
                .OrderBy(f => f.OrderKey)
                .Select(f => f.FileName)
                .ToList();

            var tasks = logFiles.Select(file => ProcessFileAsync(file, host, archivePath, postProcess, afterProcessing, processedFiles, semaphore)).ToArray();
            await Task.WhenAll(tasks);
        }

        if (afterProcessing)
        {
            PostProcessFiles([.. processedFiles], archivePath, postProcess);
        }

        Environment.Exit(0);
    }

    private static async Task<bool> ProcessFileAsync(string file, IHost host, string? archivePath, string postProcess, bool afterProcessing, ConcurrentBag<string> processedFiles, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            if (!QuietMode)
            {
                AnsiConsole.MarkupLine($"[green3]Processing file:[/] [yellow3_1]{file}[/]");
            }

            using var scope = host.Services.CreateScope();
            var logFileProcessor = scope.ServiceProvider.GetRequiredService<LogFileProcessor>();

            var processSuccess = await logFileProcessor.ProcessLogFileAsync(file);

            if (processSuccess)
            {
                if (afterProcessing)
                {
                    processedFiles.Add(file);
                }
                else
                {
                    PostProcessFile(file, archivePath, postProcess);
                }
            }
            else
            {
                if (!QuietMode)
                {
                    AnsiConsole.MarkupLine($"[red]Processing failed for file: [yellow3_1]{file}[/], skipping post-processing steps.[/]");
                }
            }

            return processSuccess;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static void PostProcessFile(string file, string? archivePath, string postProcess)
    {
        switch (postProcess)
        {
            case "archive":
                string targetPath = Path.Combine(archivePath ?? @"C:\logs\archive", Path.GetFileName(file));
                File.Move(file, targetPath);
                if (!QuietMode)
                {
                    AnsiConsole.MarkupLine($"[aqua]Archived file to:[/] [yellow3_1]{targetPath}[/]");
                }
                break;
            case "delete":
                File.Delete(file);
                if (!QuietMode)
                {
                    AnsiConsole.MarkupLine($"[aqua]Deleted file:[/] [yellow3_1]{file}[/]");
                }
                break;
            case "keep":
                break;
            default:
                break;
        }
    }

    private static void PostProcessFiles(List<string> files, string? archivePath, string postProcess)
    {
        foreach (var file in files)
        {
            PostProcessFile(file, archivePath, postProcess);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddDbContext<LogDbContext>(options =>
                options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<LogFileProcessor>();
            services.AddLogging();

            services.AddSingleton<IConfiguration>(hostContext.Configuration);
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            if (!QuietMode)
            {
                logging.AddConsole();
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            }
        });

    [GeneratedRegex(@"^.*?_(\d+)\.txt$")]
    private static partial Regex OrderKeyRegex();
}
