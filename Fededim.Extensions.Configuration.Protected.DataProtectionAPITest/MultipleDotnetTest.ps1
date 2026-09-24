<#
.SYNOPSIS

Utility to test multiple times a .NET application, storing all outputs (TRX file and HTML report) in a specified output folder, creating also a ZIP archive.

.DESCRIPTION

This script must be copied into the folder of the test project, which must be based on Microsoft.Testing.Platform (MTP) framework. The test project is rebuilt automatically before testing.

.PARAMETER StartIteration
Specifies the starting number of iterations. Useful in case, the tests hang and you need to start them again from the last iteration.

.PARAMETER EndIteration
Specifies the ending number of iterations, defining the number of times the tests need to be launched

.PARAMETER MaxRunningTimeInSeconds
Specifies the maximum number of running time in seconds the test can run, after which it gets automatically killed and restarted. This parameter was added because testing with XUnit V3 and MTP framework on net48, dotnet.exe often hangs indefinitely without an apparent reason.

.PARAMETER Configuration
Specifies the configuration Debug or Release. Defaults to release

.PARAMETER OutputFolder
Specifies the output folder where the test results (TRX file and HTML report) will be saved.

.PARAMETER Frameworks
Specifies an array of the frameworks which needs to be tested, this is necessary due to a XUnit V3 unresolved bug https://github.com/xunit/xunit/issues/864

.INPUTS

None.

.OUTPUTS

None

.EXAMPLE

PS> MultipleDotnetTests -i 10 -o "c:\temp" -f @("net10.0","net48")

Executes the tests 10 times, saving all output file in c:\temp

PS> MultipleDotnetTests -i 10 -o "c:\temp" -c Debug -f @("net10.0","net48")

Executes the tests 10 times, saving all output file in c:\temp, compiling in debug mode.

PS> MultipleDotnetTests -i 1000 -o "c:\temp" -f @("net10.0","net48") -t 600

Executes the tests 1000 times, saving all output file in c:\temp. If a test runs for more than 10 minutes, kill it and restart it automatically.

.LINK

https://github.com/fededim/Fededim.Resources/tree/master/PowershellResources
https://github.com/fededim/Fededim.Resources/blob/master/LICENSE.txt

.NOTES

© 2026 Federico Di Marco <fededim@gmail.com> released under MIT LICENSE 
#>
[CmdletBinding()]
param (
    [ValidateNotNullOrEmpty()] [Alias('s')] [int] $StartIteration = 1,
    [ValidateNotNullOrEmpty()] [Alias('i')] [int] $EndIteration = 10,
	[Alias('t')] [AllowNull()] [Nullable[System.Int32]] $MaxRunningTimeInSeconds,
    [ValidateNotNullOrEmpty()] [Alias('c')] [String] $Configuration = "Release",
    [ValidateNotNullOrEmpty()] [Alias('o')] [String] $OutputFolder = "./TestResults",
    [ValidateNotNullOrEmpty()] [Alias('f')] [String[]] $Frameworks = @("net48","net10.0")
)


function Invoke-ProcessWithTimeout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command,

        [Parameter(Mandatory = $false)]
        [string] $Arguments,

        [Parameter(Mandatory = $false)]
        [AllowNull()] [Nullable[System.Int32]] $Timeout
    )


    while ($true) {
        Write-Host "`nLaunching $Command $Arguments..." -ForegroundColor Cyan
        
        # Start the process and capture the process object
        $proc = Start-Process -FilePath $Command -ArgumentList $Arguments -NoNewWindow -PassThru
        
        try {
			if ($null -eq $Timeout) {
				#Write-Host "No timeout specified, waiting indefinitely for process termination..." -ForegroundColor Cyan
				$proc | Wait-Process -ErrorAction Stop				
			}
			else {
				#Write-Host "Waiting for $Timeout seconds for process termination" -ForegroundColor Cyan
				$proc | Wait-Process -Timeout $Timeout -ErrorAction Stop
			}
            #Write-Host "Process completed normally." -ForegroundColor Green
			return;
        }
        catch {
            Write-Host "Process exceeded maximum running time of $Timeout seconds! Terminating and restarting..." -ForegroundColor Red
            
            if (!$proc.HasExited) {
                $proc.Kill()
				$proc.WaitForExit()
            }
        }
    }
}


$Timestamp = (Get-Date -Format "ddMMyyyyHHmmss")
$ProjectName = $(Get-Item $PSScriptRoot).Name
$OutputDir = "$($ProjectName)_$($EndIteration)-Runs_$($Timestamp)"
$OutputDirectory = Join-Path $OutputFolder $OutputDir
#$MergedFileName = Join-Path $OutputDirectory "Cumulative_Result.trx"
$OutputArchive = Join-Path $OutputFolder "$($OutputDir).zip"

Write-Host "=== Starting multiple retries test ($EndIteration iterations) ===" -ForegroundColor Cyan

Write-Host "`n`ProjectName: $ProjectName" -ForegroundColor DarkGreen
Write-Host "Output Directory: $OutputDirectory" -ForegroundColor DarkGreen
if ($null -eq $Timeout) {
	Write-Host "Max Running Time in seconds: $MaxRunningTimeInSeconds" -ForegroundColor DarkGreen
}

[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

dotnet clean
dotnet build -c $Configuration

for ($i = $StartIteration; $i -le $EndIteration; $i++) {
    Write-Host "-> Executing test $i of $EndIteration..." -ForegroundColor Green
    
    # split test separately using --framework option, because launching dotnet test without it went into a deadlock after a few hundred tests
    # there is even a bug on GitHub https://github.com/xunit/xunit/issues/864 which has been closed, attributing the culprit to async / await code in the tests
    # unfortunately none of this code uses any async or await, except for a global mutex, which is perfect :-) Probably it will be reopened and fixed in the fixture
	foreach ($framework in $Frameworks) {
		$arguments = "test -c `"$Configuration`" --framework `"$framework`" --results-directory `"$OutputDirectory`" -- --report-trx --report-trx-filename `"{tfm}-{arch}\testrun_$i.trx`" --report-html --report-html-filename `"{tfm}-{arch}\testrun_$i.html`""
		Invoke-ProcessWithTimeout -Command "dotnet.exe" -Arguments $arguments -Timeout $MaxRunningTimeInSeconds
	}
}

Compress-Archive -Path "$OutputDirectory" -DestinationPath "$OutputArchive" -CompressionLevel Optimal
Write-Host "[SUCCESS] All test output files have been archived into: $OutputArchive" -ForegroundColor Cyan
