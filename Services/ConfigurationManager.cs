using System.Text.Json;
using Spectre.Console;
using smart_label_converter.Constants;
using smart_label_converter.Exceptions;
using smart_label_converter.Utilities;

namespace smart_label_converter.Services;

/// <summary>
/// Configuration model for batch conversion settings.
/// </summary>
public class ConversionConfig
{
    public string? SourcePattern { get; set; }
    public string? OutputDirectory { get; set; }
    public bool Overwrite { get; set; } = false;
    public bool ShowProgress { get; set; } = true;
    public bool Interactive { get; set; } = true;
    public int MaxFileSizeMB { get; set; } = 500;
}

/// <summary>
/// Manages configuration from files, CLI arguments, and user input.
/// </summary>
public class ConfigurationManager
{
    private readonly ConversionLogger _logger;
    private readonly PathSecurityValidator _validator;

    public ConfigurationManager(ConversionLogger logger, PathSecurityValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Loads configuration from CLI arguments or prompts user interactively.
    /// </summary>
    public ConversionConfig LoadConfiguration(string[] args)
    {
        var config = new ConversionConfig();

        if (args.Length > 0)
        {
            // Load from file or direct path
            if (args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                config = LoadFromJsonFile(args[0]) ?? config;
            }
            else
            {
                config.SourcePattern = args[0];
            }
        }
        else if (config.Interactive)
        {
            // Interactive mode
            PromptForConfiguration(config);
        }

        ValidateConfiguration(config);
        return config;
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    private ConversionConfig? LoadFromJsonFile(string configPath)
    {
        try
        {
            _validator.ValidatePath(configPath);

            if (!File.Exists(configPath))
            {
                _logger.Warning($"Configuration file not found: {configPath}");
                return null;
            }

            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<ConversionConfig>(json, options);

            _logger.Success($"Configuration loaded from: {configPath}");
            return config;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load configuration file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Prompts user interactively for configuration.
    /// </summary>
    private void PromptForConfiguration(ConversionConfig config)
    {
        AnsiConsole.MarkupLine($"[bold green]═══ {AppConstants.APP_NAME} v{AppConstants.APP_VERSION} ═══[/]");
        AnsiConsole.MarkupLine("[dim]Converts List & Label project files to JSON format[/]");
        AnsiConsole.WriteLine();

        // Source pattern
#if DEBUG
        var sourcePath = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter file path or pattern[/] (e.g., C:\\labels\\*.lst):")
                .DefaultValue(@"C:\projects\ll2json\layoutMuk.lst"));
#else
        var sourcePath = AnsiConsole.Ask<string>("[yellow]Enter file path or pattern[/] (e.g., C:\\labels\\*.lst):");
#endif
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ConverterException("No file path provided.");
        }
        config.SourcePattern = sourcePath;

        // Output directory (optional)
        var outputDir = AnsiConsole.Ask<string>(
            "[yellow]Output directory[/] (press Enter for same as source):", string.Empty);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            config.OutputDirectory = outputDir;
        }
        else
        {
            // Will be set to source directory during conversion
            config.OutputDirectory = null;
        }

        // Overwrite prompt
        config.Overwrite = AnsiConsole.Confirm(
            "[yellow]Overwrite existing output files?[/]", false);

        config.ShowProgress = true;
    }

    /// <summary>
    /// Resolves source pattern to actual file paths.
    /// Supports wildcards and directories.
    /// </summary>
    public List<string> ResolveSourceFiles(string pattern)
    {
        var files = new List<string>();

        try
        {
            _validator.ValidatePath(pattern);

            // Check if it's a directory
            if (Directory.Exists(pattern))
            {
                var directory = new DirectoryInfo(pattern);
                files.AddRange(
                    directory.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => AppConstants.SupportedExtensions.Contains(f.Extension.ToLower()))
                        .Select(f => f.FullName)
                );
            }
            // Check if it contains wildcards
            else if (pattern.Contains('*') || pattern.Contains('?'))
            {
                var directory = Path.GetDirectoryName(pattern) ?? ".";
                var searchPattern = Path.GetFileName(pattern);

                if (Directory.Exists(directory))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    files.AddRange(
                        dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
                            .Select(f => f.FullName)
                    );
                }
            }
            // Single file
            else if (File.Exists(pattern))
            {
                files.Add(Path.GetFullPath(pattern));
            }
            else
            {
                throw new FileNotFoundException($"File or pattern not found: {pattern}");
            }

            // Filter by supported extensions
            files = files
                .Where(f => AppConstants.SupportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            if (files.Count == 0)
            {
                _logger.Warning($"No supported files found matching: {pattern}");
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error resolving source files: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Validates configuration for required fields.
    /// </summary>
    private void ValidateConfiguration(ConversionConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SourcePattern))
            throw new ConverterException("Source file path or pattern is required.");

        if (config.MaxFileSizeMB < 1 || config.MaxFileSizeMB > 1000)
            config.MaxFileSizeMB = 500;
    }
}
