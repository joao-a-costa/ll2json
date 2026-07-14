using System.Text.RegularExpressions;
using Spectre.Console;

namespace smart_label_converter.Utilities;

/// <summary>
/// Handles console and file logging with colored output and sensitive data sanitization.
/// </summary>
public class ConversionLogger : IDisposable
{
    private readonly string _logFilePath;
    private bool _disposed = false;

    public ConversionLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        try
        {
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create log directory: {Markup.Escape(ex.Message)}[/]");
        }
    }

    /// <summary>
    /// Logs an informational message in gray.
    /// </summary>
    public void Info(string message)
    {
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
        WriteToFile(SanitizeMessage(message));
    }

    /// <summary>
    /// Logs a success message in green.
    /// </summary>
    public void Success(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {Markup.Escape(message)}[/]");
        WriteToFile($"✓ {SanitizeMessage(message)}");
    }

    /// <summary>
    /// Logs a warning message in yellow.
    /// </summary>
    public void Warning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(message)}[/]");
        WriteToFile($"⚠ {SanitizeMessage(message)}");
    }

    /// <summary>
    /// Logs an error message in red.
    /// </summary>
    public void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(message)}[/]");
        WriteToFile($"✗ {SanitizeMessage(message)}");
    }

    /// <summary>
    /// Logs a fatal error message with exception details.
    /// </summary>
    public void Fatal(string message, Exception? ex = null)
    {
        AnsiConsole.MarkupLine($"[red bold]FATAL ERROR: {Markup.Escape(message)}[/]");
        WriteToFile($"FATAL ERROR: {SanitizeMessage(message)}");

        if (ex != null)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.GetType().Name)}: {Markup.Escape(ex.Message)}[/]");
            WriteToFile($"{ex.GetType().Name}: {SanitizeMessage(ex.Message)}");

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                WriteToFile(SanitizeMessage(ex.StackTrace));
            }
        }
    }

    /// <summary>
    /// Logs a blank line.
    /// </summary>
    public void BlankLine()
    {
        AnsiConsole.WriteLine();
        WriteToFile("");
    }

    /// <summary>
    /// Sanitizes sensitive information from log messages.
    /// Removes passwords, API keys, and system paths.
    /// </summary>
    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Remove potential passwords/connection strings (basic pattern)
        var sanitized = Regex.Replace(message, @"(?i)(password|pwd|secret|key)=([^\s;,]+)", "$1=***");

        // Remove drive letters and reduce paths to relative
        sanitized = Regex.Replace(sanitized, @"[A-Z]:\\[^\s]+", "***");

        return sanitized;
    }

    private void WriteToFile(string message)
    {
        if (string.IsNullOrEmpty(_logFilePath) || _disposed)
            return;

        try
        {
            var timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
        }
        catch
        {
            // Silently fail to avoid disrupting conversion on log write errors
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
