namespace smart_label_converter.Constants;

/// <summary>
/// Application-wide constants and configuration values.
/// </summary>
public static class AppConstants
{
    public const string APP_NAME = "SmartLabel Converter";
    public const string APP_VERSION = "2.0.0";

    public const int EXIT_SUCCESS = 0;
    public const int EXIT_ERROR = 1;
    public const int EXIT_VALIDATION_ERROR = 2;
    public const int EXIT_SECURITY_ERROR = 3;

    public static readonly string[] SupportedExtensions = { ".lst", ".lsr", ".lbl", ".srt" };

    public const string LOG_PREFIX = "conversion_log_";
    public const string LOG_SUFFIX = ".txt";
    public const string OUTPUT_SUFFIX = ".json";

    public const int MAX_PATH_LENGTH = 260;
    public const int MAX_FILE_SIZE_MB = 500;
}
