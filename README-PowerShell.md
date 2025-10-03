# Directory Size Calculator Scripts

Two PowerShell scripts to list directories and files with their sizes.

## Scripts

### 1. Get-DirectorySize.ps1 (Recommended)
Uses native PowerShell cmdlets for better compatibility and speed.

**Usage:**
```powershell
# List immediate subdirectories and files with sizes
.\Get-DirectorySize.ps1 -Path "C:\Users\hasenbichlerd"

# List all files recursively
.\Get-DirectorySize.ps1 -Path "C:\Users\hasenbichlerd" -Recurse
```

**Features:**
- Lists all subdirectories and files in the specified path
- Shows sizes in bytes and human-readable format (KB, MB, GB, TB)
- Sorts results by size (largest first)
- Provides summary statistics
- Works without robocopy dependency

### 2. Get-DirectorySize-Robocopy.ps1
Uses robocopy for directory size calculation (Windows-specific).

**Usage:**
```powershell
.\Get-DirectorySize-Robocopy.ps1 -Path "C:\Users\hasenbichlerd"
```

**Features:**
- Uses robocopy to calculate directory sizes
- Lists immediate subdirectories and files
- Shows progress while processing
- Sorts results by size (largest first)

## Output Format

Both scripts produce formatted output like:

```
Analyzing: C:\Users\hasenbichlerd
====================================================================================================
Name                                                                        Size (Bytes)         Size
====================================================================================================
Documents\                                                                12,345,678,901     11.50 GB
Pictures\                                                                  5,678,901,234      5.29 GB
Downloads\                                                                 1,234,567,890      1.15 GB
Videos\                                                                      987,654,321    941.90 MB
Music\                                                                       456,789,012    435.56 MB
file.txt                                                                          12,345     12 bytes
====================================================================================================
TOTAL                                                                     20,703,591,358     19.28 GB
====================================================================================================

Summary:
  Total Size: 19.28 GB
  Total Items: 6
  Directories: 5
  Files: 1
```

## Why Your Original Script Didn't Work

Your original script had issues parsing robocopy output because:

1. **Incorrect regex pattern**: The pattern `'^\s*(\d+)\s+'` tried to match file sizes at the beginning of lines, but robocopy's `/L /S /BYTES /NJH /NJS /FP /NC /NS /NDL` flags don't output individual file sizes in that format.

2. **Missing summary parsing**: Robocopy outputs a summary at the end with total bytes. The correct approach is to parse the summary line that contains `Bytes : <number>`.

3. **Wrong flags combination**: The flags used prevented proper output. You need to either:
   - Parse individual file listings (without `/NFL` flag)
   - Or parse the summary statistics (look for "Bytes :" pattern)

## Fixed Robocopy Approach

If you want to fix your original approach, use:

```powershell
$Path = "C:\Users\hasenbichlerd"

# Run robocopy and capture full output
$output = & robocopy $Path NULL /L /S /NJH /NJS /BYTES /NFL /NDL 2>$null

# Parse the summary line
foreach ($line in $output) {
    if ($line -match 'Bytes\s*:\s*(\d+)') {
        $totalBytes = [int64]$matches[1]
        "Total Size: {0:N2} GB ({1:N2} MB)" -f ($totalBytes / 1GB), ($totalBytes / 1MB)
        break
    }
}
```

## Recommendations

- Use **Get-DirectorySize.ps1** for most cases (more reliable, cross-platform compatible with PowerShell Core)
- Use **Get-DirectorySize-Robocopy.ps1** only if you specifically need robocopy's behavior on Windows
