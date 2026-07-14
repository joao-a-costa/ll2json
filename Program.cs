using smart_label_converter.Constants;
using smart_label_converter.Exceptions;
using smart_label_converter.Models;
using smart_label_converter.Services;
using smart_label_converter.Utilities;
using Spectre.Console;

class Program
{
    static int Main(string[] args)
    {
        bool interactive = args.Length == 0;
        int exitCode = RunConverter(args);

        if (interactive)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
            Console.ReadKey(true);
        }

        return exitCode;
    }

    static int RunConverter(string[] args)
    {
        try
        {
            // Initialize services
            var logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
            var logFilePath = Path.Combine(
                logsDirectory,
                $"{AppConstants.LOG_PREFIX}{DateTime.Now:yyyyMMdd_HHmmss}{AppConstants.LOG_SUFFIX}");

            using var logger = new ConversionLogger(logFilePath);
            var pathValidator = new PathSecurityValidator();
            var configManager = new ConfigurationManager(logger, pathValidator);
            using var conversionService = new ConversionService(logger, pathValidator);

            // Load configuration
            var config = configManager.LoadConfiguration(args);

            // Resolve source files
            var sourceFiles = configManager.ResolveSourceFiles(config.SourcePattern ?? "");
            if (sourceFiles.Count == 0)
            {
                logger.Error($"No valid files found to process.");
                return AppConstants.EXIT_ERROR;
            }

            // Create conversion requests
            var requests = sourceFiles
                .Select(file => new ConversionRequest
                {
                    SourcePath = file,
                    OutputDirectory = config.OutputDirectory,
                    Overwrite = config.Overwrite,
                    ShowProgress = config.ShowProgress
                })
                .ToList();

            // Perform conversion(s)
            ConversionSummary summary;
            if (requests.Count == 1)
            {
                logger.Info($"Converting single file...");
                var result = conversionService.Convert(requests[0]);
                summary = new ConversionSummary
                {
                    TotalFiles = 1,
                    SuccessCount = result.Success ? 1 : 0,
                    FailureCount = result.Success ? 0 : 1,
                    Results = new List<ConversionResult> { result },
                    TotalDuration = result.Duration,
                    TotalOutputSize = result.OutputFileSize ?? 0
                };
            }
            else
            {
                summary = conversionService.ConvertBatch(requests);
            }

            // Report summary
            ReportSummary(logger, summary, logFilePath);

            logger.BlankLine();
            logger.Info($"Log file: {logFilePath}");

            return summary.FailureCount == 0 ? AppConstants.EXIT_SUCCESS : AppConstants.EXIT_ERROR;
        }
        catch (PathValidationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Security Error: {ex.Message}[/]");
            return AppConstants.EXIT_SECURITY_ERROR;
        }
        catch (FileValidationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Validation Error: {ex.Message}[/]");
            return AppConstants.EXIT_VALIDATION_ERROR;
        }
        catch (ConverterException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]FATAL ERROR: {ex.GetBaseException().Message}[/]");
            AnsiConsole.WriteException(ex);
            return AppConstants.EXIT_ERROR;
        }
    }

    static void ReportSummary(ConversionLogger logger, ConversionSummary summary, string logFilePath)
    {
        logger.BlankLine();
        logger.Info("═══════════════════════════════════════");
        logger.Info($"  {AppConstants.APP_NAME} Summary");
        logger.Info("═══════════════════════════════════════");
        logger.BlankLine();

        logger.Info($"Total Files:    {summary.TotalFiles}");
        logger.Success($"Succeeded:      {summary.SuccessCount}");

        if (summary.FailureCount > 0)
        {
            logger.Error($"Failed:         {summary.FailureCount}");
        }

        if (summary.TotalOutputSize > 0)
        {
            var sizeInMB = summary.TotalOutputSize / (1024.0 * 1024.0);
            logger.Info($"Output Size:    {sizeInMB:F2} MB");
        }

        var elapsedSeconds = (int)summary.TotalDuration.TotalSeconds;
        logger.Info($"Duration:       {elapsedSeconds}s");

        if (summary.FailureCount > 0)
        {
            logger.BlankLine();
            logger.Warning("Failed Files:");
            foreach (var result in summary.Results.Where(r => !r.Success))
            {
                logger.Info($"  • {Path.GetFileName(result.SourcePath)}: {result.ErrorMessage}");
            }
        }

        logger.BlankLine();
    }
}
