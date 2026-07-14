using combit.Reporting.ProjectConverter;
using Spectre.Console;
using smart_label_converter.Constants;
using smart_label_converter.Exceptions;
using smart_label_converter.Models;
using smart_label_converter.Utilities;

namespace smart_label_converter.Services;

/// <summary>
/// Handles the actual List & Label project conversion with proper error handling and resource management.
/// </summary>
public class ConversionService : IDisposable
{
    private readonly ConversionLogger _logger;
    private readonly PathSecurityValidator _pathValidator;
    private bool _disposed = false;

    public ConversionService(ConversionLogger logger, PathSecurityValidator pathValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pathValidator = pathValidator ?? throw new ArgumentNullException(nameof(pathValidator));
    }

    /// <summary>
    /// Converts a single List & Label project file to JSON format.
    /// </summary>
    public ConversionResult Convert(ConversionRequest request)
    {
        ThrowIfDisposed();

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.Info($"Starting conversion: {request.SourcePath}");

            // Validate input
            _logger.Info("Validating source file path...");
            _pathValidator.ValidateFullPath(request.SourcePath);
            _logger.Info("Source file path validated.");

            // Determine output path
            var outputDirectory = request.OutputDirectory ?? Path.GetDirectoryName(request.SourcePath) ?? ".";
            var fileName = Path.GetFileNameWithoutExtension(request.SourcePath);
            var outputPath = Path.Combine(outputDirectory, $"{fileName}.json");
            _logger.Info($"Output directory: {outputDirectory}");
            _logger.Info($"Output path: {outputPath}");

            // Handle collision
            if (File.Exists(outputPath) && !request.Overwrite)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                outputPath = Path.Combine(outputDirectory, $"{fileName}_{timestamp}.json");
                _logger.Warning($"Output file exists. Using: {Path.GetFileName(outputPath)}");
            }

            // Show progress
            if (request.ShowProgress)
            {
                AnsiConsole.MarkupLine($"[dim]Converting: {Markup.Escape(Path.GetFileName(request.SourcePath))}...[/]");
            }

            // Perform conversion
            _logger.Info("Initializing ProjectConverter...");
            ProjectConversionResult? lastResult = null;
            using var projectConverter = new ProjectConverter
            {
                Progress = new SyncProgress<ProjectConversionResult>(r =>
                {
                    lastResult = r;
                    var status = r.IsSuccessful ? "Success" : "Failed";
                    var detail = string.IsNullOrEmpty(r.ErrorMessage) ? "" : $" - {r.ErrorMessage}";
                    _logger.Info($"Conversion progress [{Path.GetFileName(r.ProjectIdentifier)}]: {status}{detail}");
                })
            };
            var options = new ProjectConversionOptions
            {
                DestinationDirectory = outputDirectory,
                OutputExistsBehavior = request.Overwrite
                    ? OutputExistsBehavior.Overwrite
                    : OutputExistsBehavior.Rename,
                WriteIndented = true,
                ConvertReferencedProjects = false,
                ImportData = "ProjectConverter"
            };

            _logger.Info("Running List & Label project conversion...");
            projectConverter.ConvertProjectsAndWriteToFile(new[] { request.SourcePath }, options);
            _logger.Info("Conversion call completed.");

            if (lastResult != null && !lastResult.IsSuccessful)
            {
                throw new ConversionFailedException(
                    $"List & Label reported a conversion failure: {lastResult.ErrorMessage}",
                    request.SourcePath);
            }

            // Verify output
            _logger.Info("Verifying output file was written...");
            if (!File.Exists(outputPath))
            {
                throw new ConversionFailedException(
                    $"Conversion completed but output file not found at: {outputPath}",
                    request.SourcePath);
            }

            var fileInfo = new FileInfo(outputPath);
            var duration = DateTime.UtcNow - startTime;

            _logger.Success($"Converted: {Path.GetFileName(request.SourcePath)} ({fileInfo.Length / 1024} KB)");

            return ConversionResult.CreateSuccess(request.SourcePath, outputPath, fileInfo.Length, duration);
        }
        catch (PathValidationException ex)
        {
            _logger.Error($"Validation failed: {ex.Message}");
            return ConversionResult.CreateFailure(request.SourcePath, ex.Message);
        }
        catch (FileValidationException ex)
        {
            _logger.Error($"File validation failed: {ex.Message}");
            return ConversionResult.CreateFailure(request.SourcePath, ex.Message);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            var errorMsg = $"Conversion failed: {ex.GetBaseException().Message}";
            _logger.Error(errorMsg);
            return ConversionResult.CreateFailure(request.SourcePath, errorMsg);
        }
    }

    /// <summary>
    /// Converts multiple files with batch progress reporting.
    /// </summary>
    public ConversionSummary ConvertBatch(IEnumerable<ConversionRequest> requests)
    {
        ThrowIfDisposed();

        var requestList = requests.ToList();
        var summary = new ConversionSummary { TotalFiles = requestList.Count };

        if (requestList.Count == 0)
        {
            _logger.Warning("No files to process.");
            return summary;
        }

        _logger.BlankLine();
        _logger.Info($"Processing {requestList.Count} file(s)...");
        _logger.BlankLine();

        AnsiConsole.Progress().Start(ctx =>
        {
            var task = ctx.AddTask("[green]Converting files[/]", maxValue: requestList.Count);

            foreach (var request in requestList)
            {
                _logger.Info($"Processing file {summary.Results.Count + 1} of {requestList.Count}: {Path.GetFileName(request.SourcePath)}");

                var result = Convert(request);
                summary.Results.Add(result);

                if (result.Success)
                {
                    summary.SuccessCount++;
                    summary.TotalOutputSize += result.OutputFileSize ?? 0;
                }
                else
                {
                    summary.FailureCount++;
                }

                summary.TotalDuration += result.Duration;
                task.Increment(1);
            }
        });

        _logger.Info($"Batch processing complete: {summary.SuccessCount} succeeded, {summary.FailureCount} failed.");

        return summary;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Invokes the callback synchronously, unlike System.Progress&lt;T&gt; which posts
    /// through the captured SynchronizationContext and can run after the caller returns.
    /// </summary>
    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;
        public SyncProgress(Action<T> callback) => _callback = callback;
        public void Report(T value) => _callback(value);
    }

    public void Dispose()
    {
        _disposed = true;
        _logger?.Dispose();
    }
}
