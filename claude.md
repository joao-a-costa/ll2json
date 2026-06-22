# SmartLabel.Converter v2.0.0

A command-line utility that converts **combit List & Label** project files (.lst, .lsr, .lbl, .srt) to JSON format using the ProjectConverter API from List & Label 31. This tool enables programmatic access to List & Label project definitions with enhanced security, interactivity, and batch processing capabilities.

---

## File & Folder Structure

```
ll2json/
├── claude.md                                      ← this file
├── README.md                                      ← Project documentation
├── LL2Json.slnx                                   ← Solution file
├── LL2Json.csproj                                 ← Project file; .NET 10.0 configuration
├── Program.cs                                     ← Main entry point; CLI orchestration
├── Constants/
│   └── AppConstants.cs                            ← App-wide constants and exit codes
├── Exceptions/
│   └── ConverterException.cs                      ← Custom exception hierarchy with exit codes
├── Models/
│   └── ConversionRequest.cs                       ← DTOs: ConversionRequest, ConversionResult, ConversionSummary
├── Services/
│   ├── ConversionService.cs                       ← Wraps List & Label conversion with error handling
│   └── ConfigurationManager.cs                    ← CLI config, file resolution, batch processing
├── Utilities/
│   ├── PathSecurityValidator.cs                   ← Path validation, directory traversal prevention
│   └── ConversionLogger.cs                        ← Colored console output + file logging with sanitization
├── logs/                                          ← Auto-generated timestamped conversion logs
│   └── conversion_log_*.txt                       ← Session logs with timestamps and sanitization
├── sepa.json / sepa.lst                           ← Sample converted SEPA project
├── Layout_MUK.json / Layout_MUK.lst               ← Sample converted label layout
├── bin/                                           ← Compiled output directory
├── obj/                                           ← Build artifacts directory
├── .claude/                                       ← Claude Code settings
└── .vs/                                           ← Visual Studio settings
```

---

## Architecture Overview

### **Modular Services**

| Service | Responsibility |
|---------|-----------------|
| `ConversionService` | Wraps List & Label ProjectConverter; handles single/batch conversions with progress reporting |
| `ConfigurationManager` | Loads config from CLI args, JSON files, or interactive prompts; resolves file patterns |
| `PathSecurityValidator` | Validates paths for security (traversal prevention, extensions, file size) |
| `ConversionLogger` | Colored console output (Spectre.Console) + file logging with sensitive data sanitization |

### **Custom Exceptions**

- `ConverterException` — Base class with exit code support
- `PathValidationException` — Security violations (exit code 3)
- `FileValidationException` — Format/content errors (exit code 2)
- `ConversionFailedException` — List & Label operation failures (exit code 1)

### **Data Models**

- `ConversionRequest` — Single file conversion parameters
- `ConversionResult` — Outcome of one conversion (success/failure, duration, file size)
- `ConversionSummary` — Batch summary (totals, success/failure counts)
- `ConversionConfig` — Configuration from CLI or JSON

---

## Key Features

### 🔒 **Security Hardening**
- **Path Traversal Prevention** — Validates paths don't escape allowed directories
- **Extension Whitelist** — Only allows `.lst`, `.lsr`, `.lbl`, `.srt`
- **File Size Limits** — Prevents processing of files >500 MB
- **Input Validation** — Checks for null bytes, invalid characters
- **Log Sanitization** — Removes passwords, API keys, and system paths from logs (file only; console shows readable errors)
- **Safe File Operations** — Atomic writes with backup/rename on collision

### 🎨 **Interactive CLI UX**
- **Colored Output** — Green (success), red (error), yellow (warning), gray (info)
- **Progress Bars** — Real-time batch processing feedback
- **Interactive Prompts** — File path, output directory, overwrite confirmation
- **Readable Errors** — Clear, actionable error messages
- **Proper Exit Codes** — 0=success, 1=error, 2=validation, 3=security

### ⚡ **Enhanced Features**
- **Batch Processing** — Wildcard patterns (`*.lst`), directory scanning
- **Configuration Files** — JSON config file support with CLI override
- **Batch Progress** — Real-time progress bars for multiple files
- **Pre-Conversion Validation** — File readability, size, format checks before conversion starts
- **Structured Logging** — Timestamped file logs for audit trails
- **Graceful Scripting** — Exit without blocking, proper exit codes for automation

---

## Main Modules / Classes / Functions

### `Program` (`Program.cs`)

Entry point with exception handling and service coordination.

| Member | Description |
|--------|-------------|
| `Main(string[] args)` | Orchestrates services, loads config, performs conversions, returns exit code |
| `ReportSummary(...)` | Displays conversion results with summary statistics |

**Exit Codes:**
- `0` — Success
- `1` — Conversion error
- `2` — Validation error (file format, size, extension)
- `3` — Security error (path traversal, access denied)

### `ConversionService` (`Services/ConversionService.cs`)

Wraps List & Label operations with proper resource management.

| Method | Description |
|--------|-------------|
| `Convert(ConversionRequest)` | Converts single file with validation and error handling |
| `ConvertBatch(IEnumerable<ConversionRequest>)` | Converts multiple files with progress bar |

### `ConfigurationManager` (`Services/ConfigurationManager.cs`)

Handles configuration from multiple sources.

| Method | Description |
|--------|-------------|
| `LoadConfiguration(string[] args)` | Loads config from CLI args, JSON file, or interactive prompts |
| `ResolveSourceFiles(string pattern)` | Expands wildcards, scans directories, filters by extension |

### `PathSecurityValidator` (`Utilities/PathSecurityValidator.cs`)

Prevents security issues with path handling.

| Method | Description |
|--------|-------------|
| `ValidatePath(string)` | Checks for null bytes, invalid characters, path length |
| `ValidateExtension(string)` | Ensures file extension is supported |
| `ValidateFileExists(string)` | Verifies file exists and is readable |
| `ValidateFileSize(string)` | Ensures file doesn't exceed size limits |
| `ValidateFullPath(string)` | Runs all validation checks |

### `ConversionLogger` (`Utilities/ConversionLogger.cs`)

Dual-output logging with colored console and file persistence.

| Method | Description |
|--------|-------------|
| `Info(string)` | Gray informational message |
| `Success(string)` | Green success message with ✓ |
| `Warning(string)` | Yellow warning message with ⚠ |
| `Error(string)` | Red error message with ✗ |
| `Fatal(string, Exception?)` | Red fatal error with exception details |
| `BlankLine()` | Outputs blank line |

---

## Dependencies / Libraries

| Package | Version | Purpose |
|---------|---------|---------|
| `combit.ListLabel31` | 31.2.0 | Core List & Label 31 reporting engine |
| `combit.ListLabel31.ProjectConverter` | 31.2.0 | ProjectConverter API for JSON conversion |
| `System.Drawing.Common` | 10.0.0 | Drawing utilities (List & Label requirement) |
| `Spectre.Console` | 0.49.0 | Rich colored console output and progress bars |
| `System.CommandLine` | 2.0.0-beta5 | Command-line argument parsing (optional, available) |

**Target Framework:** `.NET 10.0` (console application)

**Language Features:**
- Nullable reference types enabled
- Implicit usings enabled

---

## Supported File Formats

| Extension | Type | Description |
|-----------|------|-------------|
| `.lst` | List | Multi-record report layout |
| `.lsr` | Subreport | Embedded subreport definition |
| `.lbl` | Label | Single-record label layout |
| `.srt` | Form | Sorted list layout |

All formats are converted to standardized **JSON output**.

---

## Output Format

Converted files are written as JSON with the following characteristics:

- **File naming:** `{original_name}.json` (same directory as source)
- **Collision handling:** If output exists, append timestamp: `{name}_{yyyyMMdd_HHmmss}.json`
- **Formatting:** Indented/prettified JSON
- **Content:** Complete project structure including:
  - Project metadata (format version, assembly versions)
  - Collection variables
  - Embedded fonts
  - Filter definitions
  - Layer definitions
  - Design elements and all project objects

**Example output structure:**

```json
{
  "$type": "list",
  "Changed": null,
  "ProjectMetadata": {
    "ProjectFormat": 2,
    "AssemblyVersion": "31.2.0.0",
    "ConverterVersion": "31.2.0.0"
  },
  "CollectionVariables": [],
  "EmbeddedFonts": [...],
  "Layers": [...],
  ...
}
```

---

## Logging

The application generates timestamped log files for every conversion session in a dedicated `logs` folder:

- **Location:** `logs/conversion_log_{yyyyMMdd_HHmmss}.txt`
- **Naming:** `conversion_log_{yyyyMMdd_HHmmss}.txt` (created automatically)
- **Content:** Mirrored console output with timestamps and sanitization
- **Sanitization:** Removes passwords, API keys, and system paths from file logs (console output unaffected for readability)
- **Auto-creation:** The `logs` folder is automatically created if it doesn't exist

**Example log entry:**
```
[2026-06-22 17:44:52] Converting single file...
[2026-06-22 17:44:52] ⚠ Output file exists. Using: sepa_20260619_174205.json
[2026-06-22 17:44:52] Converting: sepa.lst...
[2026-06-22 17:44:53] ✓ Converted: sepa.lst (245 KB)
```

---

## Usage Examples

### Interactive Mode (Recommended)

```bash
SmartLabel.Converter.exe
```

**Output:**
```
═══ SmartLabel Converter v2.0.0 ═══
Converts List & Label project files to JSON format

Enter file path or pattern (e.g., C:\labels\*.lst): C:\projects\invoice.lst
Output directory (press Enter for same as source): 
Overwrite existing output files? [y/n] (n): n

Converting single file...
✓ Converted: invoice.lst (156 KB)

═══════════════════════════════════════
  SmartLabel Converter Summary
═══════════════════════════════════════

Total Files:    1
✓ Succeeded:      1
Output Size:    0.15 MB
Duration:       2s

Log file: logs/conversion_log_20260622_143000.txt
```

### Single File (Command Line)

```bash
SmartLabel.Converter.exe "C:\projects\invoice.lst"
```

### Batch Processing (Wildcards)

```bash
SmartLabel.Converter.exe "C:\projects\*.lst"
```

### Batch Processing (Directory)

```bash
SmartLabel.Converter.exe "C:\projects"
```

Processes all supported files (.lst, .lsr, .lbl, .srt) in directory with progress bar.

### Configuration File

**config.json:**
```json
{
  "sourcePattern": "C:\\projects\\*.lst",
  "outputDirectory": "C:\\output",
  "overwrite": false,
  "showProgress": true,
  "interactive": false
}
```

**Command:**
```bash
SmartLabel.Converter.exe config.json
```

### Batch Processing (PowerShell Script)

```powershell
$files = Get-ChildItem -Path "C:\projects" -Filter "*.lst"
foreach ($file in $files) {
    & "C:\path\to\SmartLabel.Converter.exe" $file.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Conversion failed for $($file.Name)"
    }
}
```

---

## Build & Publish

```powershell
# Debug build
dotnet build smart-label-converter\SmartLabel.Converter.csproj

# Release build
dotnet build -c Release smart-label-converter\SmartLabel.Converter.csproj

# Publish as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true `
  -o smart-label-converter\bin\Release\net10.0\publish\win-x64 `
  smart-label-converter\SmartLabel.Converter.csproj
```

---

## Error Handling

The application validates inputs comprehensively and provides clear error messages:

| Error Type | Exit Code | Example | Handling |
|-----------|-----------|---------|----------|
| File not found | 2 | "File not found: C:\missing.lst" | Logs and exits gracefully |
| Invalid extension | 2 | "Unsupported file extension '.txt'" | Lists supported extensions |
| Path traversal attempt | 3 | "Access denied: Path is outside allowed directory" | Blocks and logs security issue |
| File too large | 2 | "File size (501 MB) exceeds maximum (500 MB)" | Prevents processing |
| Access denied | 3 | "Cannot read file: Access denied" | Logs permission issue |
| Conversion failure | 1 | "Conversion failed: [List & Label error]" | Logs exception details |
| No files found | 1 | "No valid files found to process" | Exits after validation |

All errors are logged to both console (readable) and file (sanitized).

---

## Sample Files

The project includes example files demonstrating conversion:

- **sepa.lst / sepa.json** — SEPA (Single Euro Payments Area) invoice layout and converted output
- **Layout_MUK.lst / Layout_MUK.json** — Sample label layout and converted output

These serve as references for input/output format validation and testing.

---

## Security Considerations

✅ **Path Traversal Prevention** — Paths validated to prevent `../` attacks
✅ **Extension Whitelist** — Only supported file types accepted
✅ **File Size Limits** — Prevents DOS via large files (default 500 MB)
✅ **Input Sanitization** — Null bytes and invalid characters blocked
✅ **Log Sanitization** — Sensitive data removed from file logs
✅ **Resource Cleanup** — IDisposable patterns for proper cleanup
✅ **Error Handling** — Comprehensive exception handling with exit codes

---

## Performance Notes

- Single file conversion typically completes in 1-3 seconds
- Batch processing shows real-time progress with estimated time remaining
- Larger projects (>100 MB) may take 10+ seconds
- No special optimization needed for typical use cases (<50 MB projects)

