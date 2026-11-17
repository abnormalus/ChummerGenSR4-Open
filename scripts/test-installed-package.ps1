# Exercise the actual release artifact, including per-user MSI lifecycle on Windows.
# Run only on disposable CI hosts: the Windows branch installs/uninstalls Chummer.
param(
    [Parameter(Mandatory=$true)][string]$Assets,
    [Parameter(Mandatory=$true)][string]$ExpectedSource,
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Use a disposable GitHub Actions host for package lifecycle verification.' }
$Assets = (Resolve-Path $Assets).Path
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$Output = (Resolve-Path $Output).Path
$checks = [System.Collections.Generic.List[string]]::new()
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $checks.Add($Message)
    Write-Host "PASS $Message"
}
function Run-Process([string]$Exe, [string[]]$Arguments, [string]$Label, [int]$Seconds = 180) {
    $start = [Diagnostics.ProcessStartInfo]::new($Exe)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    if (-not $process.Start()) { throw "Could not start $Label" }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    try {
        if (-not $process.WaitForExit($Seconds * 1000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "$Label exceeded its $Seconds second deadline."
        }
        Check ($process.ExitCode -eq 0) "$Label exits successfully"
    } finally {
        [IO.File]::WriteAllText((Join-Path $Output "$Label.stdout.log"), $stdout.GetAwaiter().GetResult())
        [IO.File]::WriteAllText((Join-Path $Output "$Label.stderr.log"), $stderr.GetAwaiter().GetResult())
        $process.Dispose()
    }
}
function Check-Sums([string]$Directory) {
    foreach ($line in (Get-Content (Join-Path $Directory 'SHA256SUMS.txt'))) {
        if ($line -notmatch '^([0-9a-fA-F]{64})  ([^/\\]+)$') { throw 'Malformed package checksum entry.' }
        $expected = $Matches[1]; $name = $Matches[2]
        Check ((Get-FileHash (Join-Path $Directory $name) -Algorithm SHA256).Hash -eq $expected) "Checksum matches $name"
    }
}
function Check-Identity([string]$Exe, [string]$Root) {
    $manifest = Get-Content (Join-Path $Root 'build-identity.json') -Raw | ConvertFrom-Json
    Check ($manifest.sourceCommit -eq $ExpectedSource -and -not $manifest.dirty) 'Installed package has the expected clean source identity'
    $actual = & $Exe --version-json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed version command failed.' }
    Check ($actual.sourceCommit -eq $ExpectedSource -and $actual.version -eq $manifest.informationalVersion -and $actual.channel -eq $manifest.channel) 'Installed executable agrees with its package manifest'
}
function Native-Gate([string]$Exe, [string]$Flag, [string]$Name, [int]$Minimum, [int]$Seconds) {
    $report = Join-Path $Output "$Name.json"
    Run-Process $Exe @($Flag, (Join-Path $Output 'checks/creation.chum'), $report) $Name $Seconds
    $result = Get-Content $report -Raw | ConvertFrom-Json
    Check ($result.passed -and $result.checks.Count -ge $Minimum) "$Name retains its required assertion gate ($Minimum)"
}
$installed = $false
$msi = Join-Path $Assets 'ChummerSR4-Windows-x64.msi'
try {
    Check-Sums $Assets
    Check ((Get-Content (Join-Path $Assets 'BUILD.txt') -Raw) -match [regex]::Escape("Source commit: $ExpectedSource")) 'BUILD.txt identifies this source revision'
    if ($IsWindows) {
        $root = Join-Path $env:LOCALAPPDATA 'Programs/ChummerSR4'
        $exe = Join-Path $root 'ChummerMac.exe'
        Check (-not (Test-Path $exe)) 'Disposable host has no pre-existing Chummer installation'
        # Install the previous public Nightly first so a newer candidate exercises
        # the real UpgradeCode path, not merely installation into an empty folder.
        $previous = Join-Path $Output 'previous-nightly'
        New-Item -ItemType Directory -Path $previous | Out-Null
        foreach ($name in @('BUILD.txt', 'SHA256SUMS.txt', 'ChummerSR4-Windows-x64.msi')) {
            Invoke-WebRequest "https://github.com/abnormalus/ChummerGenSR4-Open/releases/download/Nightly/$name" -OutFile (Join-Path $previous $name)
        }
        $msiLine = Get-Content (Join-Path $previous 'SHA256SUMS.txt') | Where-Object { $_ -match '  ChummerSR4-Windows-x64.msi$' }
        Check ($msiLine.Count -eq 1 -and (Get-FileHash (Join-Path $previous 'ChummerSR4-Windows-x64.msi') -Algorithm SHA256).Hash -eq $msiLine.Substring(0,64)) 'Previous Nightly installer matches its published checksum'
        Run-Process 'msiexec.exe' @('/i', (Join-Path $previous 'ChummerSR4-Windows-x64.msi'), '/qn', '/norestart', '/l*v', (Join-Path $Output 'previous-install.log')) 'previous-install'
        $installed = $true
        Check (Test-Path $exe) 'Previous Nightly installs to the documented per-user location'
        $userDirectory = Join-Path $env:APPDATA 'ChummerGenSR4Mac'
        New-Item -ItemType Directory -Path (Join-Path $userDirectory 'settings') -Force | Out-Null
        $marker = Join-Path $userDirectory 'settings/package-acceptance-sentinel.xml'
        $markerText = '<settings><source>SR4</source><pdf>untouched-qa-reference.pdf</pdf></settings>'
        [IO.File]::WriteAllText($marker, $markerText)
        $characterMarker = Join-Path $Output 'preserved-character.chum'
        [IO.File]::WriteAllText($characterMarker, '<character><name>Preserve this QA file</name></character>')
        Run-Process 'msiexec.exe' @('/i', $msi, '/qn', '/norestart', '/l*v', (Join-Path $Output 'candidate-install.log')) 'candidate-upgrade'
        Check-Identity $exe $root
        $shortcut = Join-Path $env:APPDATA 'Microsoft/Windows/Start Menu/Programs/Chummer SR4/Chummer SR4.lnk'
        Check (Test-Path $shortcut) 'Installed package registers its Start menu shortcut'
        $shell = New-Object -ComObject WScript.Shell
        Check ($shell.CreateShortcut($shortcut).TargetPath -eq $exe) 'Start menu shortcut targets the installed executable'
        Check ((Get-ItemProperty 'HKCU:/Software/ChummerSR4/Installer').Installed -eq 1) 'Installer uses its per-user registry key'
        Run-Process 'msiexec.exe' @('/i', $msi, 'REINSTALL=ALL', 'REINSTALLMODE=vomus', '/qn', '/norestart', '/l*v', (Join-Path $Output 'repair.log')) 'same-version-repair'
        Check-Identity $exe $root
        Check ((Get-Content $marker -Raw) -eq $markerText) 'Upgrade and replacement preserve user settings/source associations'
    } elseif ($IsMacOS) {
        $stage = Join-Path $Output 'extracted'
        Run-Process '/usr/bin/ditto' @('-x', '-k', (Join-Path $Assets 'ChummerSR4-macOS-arm64.zip'), $stage) 'extract'
        $app = Join-Path $stage 'ChummerSR4/ChummerSR4.app'
        Run-Process '/usr/bin/codesign' @('--verify', '--deep', '--strict', $app) 'bundle-signature'
        $root = Join-Path $app 'Contents/MacOS'
        $exe = Join-Path $root 'ChummerMac'
        Check-Identity $exe $root
    } else { throw 'Unsupported acceptance host.' }
    Run-Process $exe @('--smoke-test', (Join-Path $Output 'checks')) 'installed-headless' 600
    Check (Test-Path (Join-Path $Output 'checks/creation.chum')) 'Installed application generated its synthetic character'
    Native-Gate $exe '--check-bulk' 'installed-native-bulk' 1069 300
    Native-Gate $exe '--check-sensor-transactions' 'installed-native-sensors' 17 180
    Native-Gate $exe '--check-focus-command' 'installed-native-focus' 66 180
    Native-Gate $exe '--check-pdf-reader' 'installed-native-reader' 10 180
    if ($IsWindows) {
        Run-Process 'msiexec.exe' @('/x', $msi, '/qn', '/norestart', '/l*v', (Join-Path $Output 'uninstall.log')) 'uninstall'
        $installed = $false
        Check (-not (Test-Path $exe) -and -not (Test-Path $shortcut)) 'Uninstall removes the application and Start menu shortcut'
        Check ($null -eq (Get-ItemProperty -Path 'HKCU:/Software/ChummerSR4/Installer' -Name Installed -ErrorAction SilentlyContinue)) 'Uninstall removes its installer registration marker'
        Check ((Get-Content $marker -Raw) -eq $markerText -and (Test-Path $characterMarker)) 'Uninstall preserves external character files and user settings'
    }
    @{ passed=$true; source=$ExpectedSource; checks=$checks.ToArray() } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $Output 'package-acceptance.json')
} catch {
    @{ passed=$false; source=$ExpectedSource; failedCheck=$_.Exception.Message; checks=$checks.ToArray() } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $Output 'package-acceptance.json')
    throw
} finally {
    # Failures retain all evidence and clean up only the test-created installation.
    if ($installed -and (Test-Path $msi)) {
        try { Run-Process 'msiexec.exe' @('/x', $msi, '/qn', '/norestart', '/l*v', (Join-Path $Output 'cleanup.log')) 'cleanup-uninstall' } catch { Write-Warning $_ }
    }
}
