# =============================================================================
#  SmartRetailX - JMeter Test Execution Runner
# =============================================================================
param(
    [ValidateSet('Load', 'Stress', 'All')]
    [string]$TestType = 'Load',
    [string]$HostName = 'localhost',
    [int]$Port = 80,
    [string]$Protocol = 'http',
    [string]$JMeterPath = 'jmeter'
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir 'results'
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

function Test-JMeterInstalled {
    param([string]$Path)
    $cmd = Get-Command $Path -ErrorAction SilentlyContinue
    if ($cmd) { return $true }
    if (Test-Path $Path) { return $true }
    return $false
}

function Run-JMeterPlan {
    param(
        [string]$PlanFile,
        [string]$ResultFile,
        [string]$ReportFolder,
        [hashtable]$Properties
    )

    $propArgs = @()
    foreach ($k in $Properties.Keys) {
        $val = $Properties[$k]
        $propArgs += "-J{0}={1}" -f $k, $val
    }

    $jtlPath = Join-Path $OutputDir $ResultFile
    $reportPath = Join-Path $OutputDir $ReportFolder

    if (Test-Path $jtlPath) { Remove-Item $jtlPath -Force }
    if (Test-Path $reportPath) { Remove-Item $reportPath -Recurse -Force }

    $targetUrl = "{0}://{1}:{2}" -f $Protocol, $HostName, $Port
    Write-Host ''
    Write-Host ("[+] Executing JMeter Plan: " + $PlanFile) -ForegroundColor Cyan
    Write-Host ("    Target Endpoint: " + $targetUrl) -ForegroundColor DarkGray
    Write-Host ("    Output Results:  " + $jtlPath) -ForegroundColor DarkGray
    Write-Host ("    HTML Dashboard:  " + $reportPath) -ForegroundColor DarkGray

    if (-not (Test-JMeterInstalled -Path $JMeterPath)) {
        Write-Host ("[!] JMeter command '{0}' was not found in PATH or at specified path." -f $JMeterPath) -ForegroundColor Yellow
        Write-Host "    To run tests in JMeter GUI or CLI:" -ForegroundColor Gray
        Write-Host "    1. Download Apache JMeter from https://jmeter.apache.org/download_jmeter.cgi" -ForegroundColor Gray
        Write-Host "    2. Open the test plan in JMeter GUI or run CLI:" -ForegroundColor Gray
        Write-Host ("       jmeter -n -t `"{0}`" -l `"{1}`" -e -o `"{2}`"" -f $PlanFile, $jtlPath, $reportPath) -ForegroundColor Gray
        return
    }

    try {
        $jmeterArgs = @('-n', '-t', $PlanFile, '-l', $jtlPath, '-e', '-o', $reportPath) + $propArgs
        Write-Host ("    Running JMeter with arguments: " + ($jmeterArgs -join ' ')) -ForegroundColor Gray
        & $JMeterPath @jmeterArgs
        Write-Host ("[OK] Test Run Completed. Report generated at: " + $reportPath) -ForegroundColor Green
    } catch {
        Write-Host ("[!] Error executing JMeter: " + $_) -ForegroundColor Red
    }
}

if ($TestType -eq 'Load' -or $TestType -eq 'All') {
    $loadPlan = Join-Path $ScriptDir 'SmartRetailX_Load_Test.jmx'
    $loadProps = @{
        'host' = $HostName
        'port' = $Port
        'protocol' = $Protocol
        'threads' = 25
        'rampup' = 10
        'duration' = 60
    }
    Run-JMeterPlan -PlanFile $loadPlan -ResultFile 'load_test_results.jtl' -ReportFolder 'load_test_report' -Properties $loadProps
}

if ($TestType -eq 'Stress' -or $TestType -eq 'All') {
    $stressPlan = Join-Path $ScriptDir 'SmartRetailX_Stress_Test.jmx'
    $stressProps = @{
        'host' = $HostName
        'port' = $Port
        'protocol' = $Protocol
        'stage_duration' = 60
    }
    Run-JMeterPlan -PlanFile $stressPlan -ResultFile 'stress_test_results.jtl' -ReportFolder 'stress_test_report' -Properties $stressProps
}
