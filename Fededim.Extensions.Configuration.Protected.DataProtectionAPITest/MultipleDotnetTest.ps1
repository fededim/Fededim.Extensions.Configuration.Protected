[CmdletBinding()]
param (
    [ValidateNotNullOrEmpty()] [Alias('i')] [int] $Iterations = 2,
    [ValidateNotNullOrEmpty()] [Alias('c')] [String] $Configuration = "Release",
    [ValidateNotNullOrEmpty()] [Alias('o')] [String] $OutputFolder = "./TestResults"
)

$Timestamp = (Get-Date -Format "ddMMyyyyHHmmss")
$ProjectName = $(Get-Item $PSScriptRoot).Name
$OutputDir = "$($ProjectName)_$($Iterations)-Runs_$($Timestamp)"
$OutputDirectory = Join-Path $OutputFolder $OutputDir
#$MergedFileName = Join-Path $OutputDirectory "Cumulative_Result.trx"
$OutputArchive = Join-Path $OutputFolder "$($OutputDir).zip"

Write-Host "=== Starting multiple retries test ($Iterations iterations) ===" -ForegroundColor Cyan

Write-Host "`n`ProjectName: $ProjectName" -ForegroundColor DarkGreen
Write-Host "Output Directory: $OutputDirectory" -ForegroundColor DarkGreen

$ToolCheck = dotnet tool list --global | Select-String "dotnet-trx-merge"
if (-not $ToolCheck) {
    Write-Host "Tool 'dotnet-trx-merge' not found. Installing it..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-trx-merge
}

[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

dotnet clean
dotnet build -c $Configuration

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "-> Executing test $i of $Iterations..." -ForegroundColor Green
    
    # split test separately using --framework option, because launching dotnet test without it went into a deadlock after a few hundred tests
    # there is even a bug on GitHub https://github.com/xunit/xunit/issues/864 which has been closed, attributing the culprit to async / await code in the tests
    # unfortunately none of this code uses any async or await, except for a global mutex, which is perfect :-) Probably it will be reopened and fixed in the fixture
	dotnet test -c "$Configuration" --framework net48 --results-directory "$OutputDirectory" -- --report-trx --report-trx-filename "{tfm}-{arch}\testrun_$i.trx" --report-html --report-html-filename "{tfm}-{arch}\testrun_$i.html"
	dotnet test -c "$Configuration" --framework net10.0 --results-directory "$OutputDirectory" -- --report-trx --report-trx-filename "{tfm}-{arch}\testrun_$i.trx" --report-html --report-html-filename "{tfm}-{arch}\testrun_$i.html"
}

#Write-Host "-> Merging TRX files..." -ForegroundColor Green
#$postfixes = Get-ChildItem -File | ForEach-Object { ($_.Name -split '_')[2] } | Select-Object -Unique
#trx-merge --dir "$OutputDirectory" --output "$MergedFileName"
#Write-Host "[SUCCESS] All TRX files have been merged into: $MergedFileName" -ForegroundColor Cyan

Compress-Archive -Path "$OutputDirectory" -DestinationPath "$OutputArchive" -CompressionLevel Optimal
Write-Host "[SUCCESS] All test output files have been archived into: $OutputArchive" -ForegroundColor Cyan
