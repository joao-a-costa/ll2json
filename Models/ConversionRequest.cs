using smart_label_converter.Constants;

namespace smart_label_converter.Models;

/// <summary>
/// Represents a single file conversion request.
/// </summary>
public class ConversionRequest
{
    public required string SourcePath { get; set; }
    public string? OutputDirectory { get; set; }
    public bool Overwrite { get; set; } = false;
    public bool ShowProgress { get; set; } = true;
}

/// <summary>
/// Represents the result of a conversion attempt.
/// </summary>
public class ConversionResult
{
    public string? SourcePath { get; set; }
    public string? OutputPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long? OutputFileSize { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ConversionResult CreateSuccess(string sourcePath, string outputPath, long fileSize, TimeSpan duration)
        => new()
        {
            SourcePath = sourcePath,
            OutputPath = outputPath,
            Success = true,
            OutputFileSize = fileSize,
            Duration = duration,
            Timestamp = DateTime.UtcNow
        };

    public static ConversionResult CreateFailure(string sourcePath, string errorMessage)
        => new()
        {
            SourcePath = sourcePath,
            Success = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
}

/// <summary>
/// Summary of batch conversion results.
/// </summary>
public class ConversionSummary
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long TotalOutputSize { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<ConversionResult> Results { get; set; } = new();
}
