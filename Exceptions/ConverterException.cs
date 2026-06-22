using smart_label_converter.Constants;

namespace smart_label_converter.Exceptions;

/// <summary>
/// Base exception for all converter-related errors.
/// </summary>
public class ConverterException : Exception
{
    public int ExitCode { get; set; }

    public ConverterException(string message, int exitCode = AppConstants.EXIT_ERROR)
        : base(message)
    {
        ExitCode = exitCode;
    }

    public ConverterException(string message, Exception innerException, int exitCode = AppConstants.EXIT_ERROR)
        : base(message, innerException)
    {
        ExitCode = exitCode;
    }
}

/// <summary>
/// Thrown when file path validation fails (security or format issues).
/// </summary>
public class PathValidationException : ConverterException
{
    public PathValidationException(string message)
        : base(message, AppConstants.EXIT_SECURITY_ERROR) { }
}

/// <summary>
/// Thrown when file content or format validation fails.
/// </summary>
public class FileValidationException : ConverterException
{
    public FileValidationException(string message)
        : base(message, AppConstants.EXIT_VALIDATION_ERROR) { }
}

/// <summary>
/// Thrown during the conversion process when List & Label operations fail.
/// </summary>
public class ConversionFailedException : ConverterException
{
    public string? SourceFile { get; set; }

    public ConversionFailedException(string message, string? sourceFile = null)
        : base(message, AppConstants.EXIT_ERROR)
    {
        SourceFile = sourceFile;
    }

    public ConversionFailedException(string message, Exception innerException, string? sourceFile = null)
        : base(message, innerException, AppConstants.EXIT_ERROR)
    {
        SourceFile = sourceFile;
    }
}
