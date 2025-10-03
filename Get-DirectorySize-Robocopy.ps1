param(
    [Parameter(Mandatory=$true)]
    [string]$Path
)

# Verify path exists
if (-not (Test-Path -Path $Path)) {
    Write-Error "Path does not exist: $Path"
    exit 1
}

Write-Host "`nAnalyzing directory sizes using robocopy: $Path" -ForegroundColor Cyan
Write-Host ("=" * 100) -ForegroundColor Gray

# Get all immediate subdirectories
$subdirs = Get-ChildItem -Path $Path -Directory -ErrorAction SilentlyContinue

# Function to get directory size using robocopy
function Get-RobocopySize {
    param([string]$DirPath)
    
    # Run robocopy in list-only mode
    $output = & robocopy $DirPath NULL /L /S /NJH /NJS /BYTES /NFL /NDL 2>$null
    
    # Parse the summary line that contains total bytes
    foreach ($line in $output) {
        if ($line -match 'Bytes\s*:\s*(\d+)') {
            return [int64]$matches[1]
        }
    }
    return 0
}

$results = @()
$totalSize = 0

# Process each subdirectory
foreach ($dir in $subdirs) {
    Write-Host "Processing: $($dir.Name)..." -NoNewline
    $size = Get-RobocopySize -DirPath $dir.FullName
    $results += [PSCustomObject]@{
        Name = $dir.Name
        SizeBytes = $size
    }
    $totalSize += $size
    Write-Host " Done" -ForegroundColor Green
}

# Get files in the root directory
$files = Get-ChildItem -Path $Path -File -ErrorAction SilentlyContinue
foreach ($file in $files) {
    $results += [PSCustomObject]@{
        Name = $file.Name
        SizeBytes = $file.Length
    }
    $totalSize += $file.Length
}

# Display results
Write-Host "`n"
Write-Host ("{0,-70} {1,15} {2,13}" -f "Name", "Size (Bytes)", "Size") -ForegroundColor Yellow
Write-Host ("=" * 100) -ForegroundColor Gray

$results | Sort-Object -Property SizeBytes -Descending | ForEach-Object {
    $nameDisplay = if ($_.Name.Length -gt 68) {
        $_.Name.Substring(0, 65) + "..."
    } else {
        $_.Name
    }
    
    $sizeFormatted = if ($_.SizeBytes -gt 1GB) {
        "{0:N2} GB" -f ($_.SizeBytes / 1GB)
    } elseif ($_.SizeBytes -gt 1MB) {
        "{0:N2} MB" -f ($_.SizeBytes / 1MB)
    } elseif ($_.SizeBytes -gt 1KB) {
        "{0:N2} KB" -f ($_.SizeBytes / 1KB)
    } else {
        "{0} bytes" -f $_.SizeBytes
    }
    
    Write-Host ("{0,-70} {1,15:N0} {2,13}" -f $nameDisplay, $_.SizeBytes, $sizeFormatted)
}

Write-Host ("=" * 100) -ForegroundColor Gray
$totalFormatted = if ($totalSize -gt 1GB) {
    "{0:N2} GB" -f ($totalSize / 1GB)
} elseif ($totalSize -gt 1MB) {
    "{0:N2} MB" -f ($totalSize / 1MB)
} else {
    "{0:N2} KB" -f ($totalSize / 1KB)
}
Write-Host ("{0,-70} {1,15:N0} {2,13}" -f "TOTAL", $totalSize, $totalFormatted) -ForegroundColor Green
Write-Host ("=" * 100) -ForegroundColor Gray
Write-Host ""
