using smart_label_converter.Constants;
using smart_label_converter.Exceptions;

namespace smart_label_converter.Utilities;

/// <summary>
/// Validates file paths to prevent directory traversal and unauthorized access.
/// </summary>
public class PathSecurityValidator
{
    private readonly string? _allowedBaseDirectory;

    /// <summary>
    /// Creates a validator with optional directory boundary enforcement.
    /// </summary>
    /// <param name="allowedBaseDirectory">Optional base directory to restrict file access. If null, no directory restrictions applied.</param>
    public PathSecurityValidator(string? allowedBaseDirectory = null)
    {
        _allowedBaseDirectory = allowedBaseDirectory;
    }

    /// <summary>
    /// Validates that a path is safe and within allowed boundaries.
    /// </summary>
    public void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new PathValidationException("Path cannot be empty.");

        if (path.Length > AppConstants.MAX_PATH_LENGTH)
            throw new PathValidationException($"Path exceeds maximum length of {AppConstants.MAX_PATH_LENGTH} characters.");

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new PathValidationException("Path contains invalid characters.");

        if (path.Contains('\0'))
            throw new PathValidationException("Path contains null characters (security violation).");

        // Check for path traversal attempts (only if base directory is specified)
        if (!string.IsNullOrEmpty(_allowedBaseDirectory))
        {
            var fullPath = Path.GetFullPath(path);
            var basePath = Path.GetFullPath(_allowedBaseDirectory);

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new PathValidationException(
                    $"Access denied: Path is outside allowed directory.");
        }
    }

    /// <summary>
    /// Validates file extension is in the supported list.
    /// </summary>
    public void ValidateExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        if (!AppConstants.SupportedExtensions.Contains(extension))
            throw new FileValidationException(
                $"Unsupported file extension '{extension}'. Supported: {string.Join(", ", AppConstants.SupportedExtensions)}");
    }

    /// <summary>
    /// Validates that a file exists and is readable.
    /// </summary>
    public void ValidateFileExists(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileValidationException($"File not found: {filePath}");

        try
        {
            // Test read access
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
        catch (UnauthorizedAccessException)
        {
            throw new PathValidationException($"Access denied: Cannot read file: {filePath}");
        }
        catch (IOException ex)
        {
            throw new FileValidationException($"Cannot access file: {filePath} - {ex.Message}");
        }
    }

    /// <summary>
    /// Validates file size is within acceptable limits.
    /// </summary>
    public void ValidateFileSize(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var maxBytes = AppConstants.MAX_FILE_SIZE_MB * 1024 * 1024;

            if (fileInfo.Length > maxBytes)
                throw new FileValidationException(
                    $"File size ({fileInfo.Length / (1024 * 1024)} MB) exceeds maximum ({AppConstants.MAX_FILE_SIZE_MB} MB)");
        }
        catch (IOException ex)
        {
            throw new FileValidationException($"Cannot determine file size: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs all validation checks.
    /// </summary>
    public void ValidateFullPath(string filePath)
    {
        ValidatePath(filePath);
        ValidateFileExists(filePath);
        ValidateExtension(filePath);
        ValidateFileSize(filePath);
    }
}
