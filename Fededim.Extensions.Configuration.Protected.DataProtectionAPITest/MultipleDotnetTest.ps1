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
    
	dotnet test -c "$Configuration" --results-directory "$OutputDirectory" -- --report-trx --report-trx-filename "{tfm}-{arch}\testrun_$i.trx" --report-html --report-html-filename "{tfm}-{arch}\testrun_$i.html"
}

#Write-Host "-> Merging TRX files..." -ForegroundColor Green
#$postfixes = Get-ChildItem -File | ForEach-Object { ($_.Name -split '_')[2] } | Select-Object -Unique
#trx-merge --dir "$OutputDirectory" --output "$MergedFileName"
#Write-Host "[SUCCESS] All TRX files have been merged into: $MergedFileName" -ForegroundColor Cyan

Compress-Archive -Path "$OutputDirectory" -DestinationPath "$OutputArchive" -CompressionLevel Optimal
Write-Host "[SUCCESS] All test output files have been archived into: $OutputArchive" -ForegroundColor Cyan
