param(
    [Parameter(Mandatory=$true)]
    [string]$Path,
    [switch]$Recurse
)

# Function to format file size
function Format-FileSize {
    param([int64]$Size)
    
    if ($Size -gt 1TB) {
        return "{0:N2} TB" -f ($Size / 1TB)
    } elseif ($Size -gt 1GB) {
        return "{0:N2} GB" -f ($Size / 1GB)
    } elseif ($Size -gt 1MB) {
        return "{0:N2} MB" -f ($Size / 1MB)
    } elseif ($Size -gt 1KB) {
        return "{0:N2} KB" -f ($Size / 1KB)
    } else {
        return "{0} bytes" -f $Size
    }
}

# Function to calculate directory size
function Get-DirectorySize {
    param([string]$DirPath)
    
    $size = 0
    try {
        $size = (Get-ChildItem -Path $DirPath -Recurse -File -ErrorAction SilentlyContinue | 
                 Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
        if ($null -eq $size) { $size = 0 }
    } catch {
        $size = 0
    }
    return $size
}

# Verify path exists
if (-not (Test-Path -Path $Path)) {
    Write-Error "Path does not exist: $Path"
    exit 1
}

Write-Host "`nAnalyzing: $Path" -ForegroundColor Cyan
Write-Host ("=" * 100) -ForegroundColor Gray
Write-Host ("{0,-70} {1,15} {2,13}" -f "Name", "Size (Bytes)", "Size") -ForegroundColor Yellow
Write-Host ("=" * 100) -ForegroundColor Gray

$totalSize = 0
$items = @()

# Get all items in the directory
if ($Recurse) {
    # Recursive: List all files with their sizes
    $files = Get-ChildItem -Path $Path -Recurse -File -ErrorAction SilentlyContinue
    
    foreach ($file in $files) {
        $relPath = $file.FullName.Substring($Path.Length).TrimStart('\', '/')
        $items += [PSCustomObject]@{
            Name = $relPath
            Type = "File"
            SizeBytes = $file.Length
            SizeFormatted = Format-FileSize -Size $file.Length
        }
        $totalSize += $file.Length
    }
} else {
    # Non-recursive: List immediate subdirectories and files
    $childItems = Get-ChildItem -Path $Path -ErrorAction SilentlyContinue
    
    foreach ($item in $childItems) {
        if ($item.PSIsContainer) {
            # It's a directory - calculate its size
            $dirSize = Get-DirectorySize -DirPath $item.FullName
            $items += [PSCustomObject]@{
                Name = $item.Name + "\"
                Type = "Directory"
                SizeBytes = $dirSize
                SizeFormatted = Format-FileSize -Size $dirSize
            }
            $totalSize += $dirSize
        } else {
            # It's a file
            $items += [PSCustomObject]@{
                Name = $item.Name
                Type = "File"
                SizeBytes = $item.Length
                SizeFormatted = Format-FileSize -Size $item.Length
            }
            $totalSize += $item.Length
        }
    }
}

# Sort by size (descending) and display
$items | Sort-Object -Property SizeBytes -Descending | ForEach-Object {
    $nameDisplay = if ($_.Name.Length -gt 68) {
        $_.Name.Substring(0, 65) + "..."
    } else {
        $_.Name
    }
    
    Write-Host ("{0,-70} {1,15:N0} {2,13}" -f $nameDisplay, $_.SizeBytes, $_.SizeFormatted)
}

Write-Host ("=" * 100) -ForegroundColor Gray
Write-Host ("{0,-70} {1,15:N0} {2,13}" -f "TOTAL", $totalSize, (Format-FileSize -Size $totalSize)) -ForegroundColor Green
Write-Host ("=" * 100) -ForegroundColor Gray

# Summary statistics
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  Total Size: $(Format-FileSize -Size $totalSize)"
Write-Host "  Total Items: $($items.Count)"
Write-Host "  Directories: $(($items | Where-Object {$_.Type -eq 'Directory'}).Count)"
Write-Host "  Files: $(($items | Where-Object {$_.Type -eq 'File'}).Count)"
Write-Host ""
