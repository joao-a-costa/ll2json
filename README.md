# LL2Json - List & Label to JSON Converter

A command-line utility that converts **combit List & Label** project files (.lst, .lsr, .lbl, .srt) to JSON format using the ProjectConverter API from List & Label 31. This tool enables programmatic access to List & Label project definitions with enhanced security, interactivity, and batch processing capabilities.

## Quick Start

### Interactive Mode (Recommended)
```bash
LL2Json.exe
```

### Convert Single File
```bash
LL2Json.exe "C:\projects\invoice.lst"
```

### Batch Processing with Wildcards
```bash
LL2Json.exe "C:\projects\*.lst"
```

### Batch Processing from Directory
```bash
LL2Json.exe "C:\projects"
```

## Features

### 🔒 Security Hardening
- **Path Traversal Prevention** — Prevents `../` directory escape attacks
- **Extension Whitelist** — Only allows `.lst`, `.lsr`, `.lbl`, `.srt` files
- **File Size Limits** — Prevents processing of files >500 MB (DOS protection)
- **Input Validation** — Rejects null bytes and invalid characters
- **Log Sanitization** — Removes passwords, API keys, and system paths from logs
- **Safe File Operations** — Atomic writes with timestamp-based collision handling

### 🎨 Interactive CLI UX
- **Colored Output** — Green (success), red (error), yellow (warning), gray (info)
- **Progress Bars** — Real-time feedback for batch conversions
- **Interactive Prompts** — File path, output directory, and overwrite options
- **Clear Error Messages** — Actionable guidance for all failure scenarios
- **Proper Exit Codes** — Standard codes (0=success, 1=error, 2=validation, 3=security)

### ⚡ Enhanced Features
- **Batch Processing** — Wildcard patterns (`*.lst`) and directory scanning
- **Configuration Files** — JSON config file support with CLI override
- **Pre-Conversion Validation** — File readability, size, and format checks
- **Structured Logging** — Timestamped logs in dedicated `logs/` folder for audit trails
- **Scripting-Friendly** — No blocking, proper exit codes for automation and PowerShell

## Supported Formats

| Extension | Type | Description |
|-----------|------|-------------|
| `.lst` | List | Multi-record report layout |
| `.lsr` | Subreport | Embedded subreport definition |
| `.lbl` | Label | Single-record label layout |
| `.srt` | Form | Sorted list layout |

All formats are converted to standardized **JSON output** with full project structure.

## Output

- **File naming:** `{original_name}.json` (same directory as source)
- **Collision handling:** If output exists, append timestamp: `{name}_{yyyyMMdd_HHmmss}.json`
- **Formatting:** Indented/prettified JSON
- **Content:** Complete project structure including metadata, variables, fonts, filters, layers, and all design elements

**Example output structure:**
```json
{
  "$type": "list",
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

## Logging

Conversion logs are automatically generated in the `logs/` folder:

- **Location:** `logs/conversion_log_{yyyyMMdd_HHmmss}.txt`
- **Content:** Session activity with timestamps and sanitization
- **Sanitization:** File logs remove passwords, API keys, and system paths (console shows readable errors)

**Example log entry:**
```
[2026-06-22 17:44:52] Converting single file...
[2026-06-22 17:44:52] ⚠ Output file exists. Using: sepa_20260622_174205.json
[2026-06-22 17:44:52] Converting: sepa.lst...
[2026-06-22 17:44:53] ✓ Converted: sepa.lst (245 KB)
```

## Usage Examples

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

**Run with config:**
```bash
LL2Json.exe config.json
```

### PowerShell Batch Script
```powershell
$files = Get-ChildItem -Path "C:\projects" -Filter "*.lst"
foreach ($file in $files) {
    & "C:\path\to\LL2Json.exe" $file.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Conversion failed for $($file.Name)"
    }
}
```

### CI/CD Integration
```bash
# Convert all .lst files and check exit code
LL2Json.exe "C:\label-projects\*.lst"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
```

## Error Handling

The application provides clear, actionable error messages with standardized exit codes:

| Error Type | Exit Code | Example |
|-----------|-----------|---------|
| File not found | 2 | "File not found: C:\missing.lst" |
| Invalid extension | 2 | "Unsupported file extension '.txt'" |
| File too large | 2 | "File size (501 MB) exceeds maximum (500 MB)" |
| Path traversal attempt | 3 | "Access denied: Path is outside allowed directory" |
| Access denied | 3 | "Cannot read file: Access denied" |
| Conversion failure | 1 | "Conversion failed: [List & Label error]" |
| No files found | 1 | "No valid files found to process" |

## Build & Publish

### Debug Build
```powershell
dotnet build LL2Json.csproj
```

### Release Build
```powershell
dotnet build -c Release LL2Json.csproj
```

### Self-Contained Executable
```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -o bin\Release\net10.0\publish\win-x64 `
  LL2Json.csproj
```

## Requirements

- **.NET 10.0** or later
- **Windows** (console application)
- **List & Label 31** libraries (included in NuGet packages)

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `combit.ListLabel31` | 31.2.0 | Core List & Label 31 reporting engine |
| `combit.ListLabel31.ProjectConverter` | 31.2.0 | ProjectConverter API for JSON conversion |
| `System.Drawing.Common` | 10.0.0 | Drawing utilities (List & Label requirement) |
| `Spectre.Console` | 0.49.0 | Rich colored console output and progress bars |

## Sample Files

The project includes example files for testing and validation:

- **sepa.lst / sepa.json** — SEPA (Single Euro Payments Area) invoice layout
- **Layout_MUK.lst / Layout_MUK.json** — Sample label layout

## Performance

- Single file conversion: 1-3 seconds (typical)
- Batch processing: Real-time progress with estimated time remaining
- Large projects (>100 MB): 10+ seconds
- Typical projects (<50 MB): Fast conversion without special optimization

## Architecture

### Core Services
- **ConversionService** — Wraps List & Label ProjectConverter with error handling
- **ConfigurationManager** — Loads config from CLI args, JSON, or interactive prompts
- **PathSecurityValidator** — Validates paths and prevents security issues
- **ConversionLogger** — Dual-output logging (console + file) with sanitization

### Custom Exceptions
- `ConverterException` — Base class with exit code support
- `PathValidationException` — Security violations (exit code 3)
- `FileValidationException` — Format/content errors (exit code 2)
- `ConversionFailedException` — List & Label operation failures (exit code 1)

## Project Structure

```
ll2json/
├── Program.cs                    ← Main entry point
├── Constants/                    ← Application constants
├── Exceptions/                   ← Custom exception types
├── Models/                       ← Data transfer objects
├── Services/                     ← Core conversion logic
├── Utilities/                    ← Security and logging
├── logs/                         ← Auto-generated conversion logs
└── bin/, obj/                    ← Build artifacts
```

## For Developers

See [claude.md](claude.md) for detailed architecture, module documentation, and development guidelines.

## License

© 2026 SmartDigit. All rights reserved.

## Support

For issues or questions, contact: support@smartdigit.pt
