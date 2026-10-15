$ErrorActionPreference = 'Stop'
# Official detection/install procedure:
# https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution
function Get-WebView2Version {
    $client = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}'
    foreach ($root in @('HKLM:\SOFTWARE\WOW6432Node', 'HKLM:\SOFTWARE', 'HKCU:\Software')) {
        $value = (Get-ItemProperty -Path "$root\Microsoft\EdgeUpdate\Clients\$client" -Name pv -ErrorAction SilentlyContinue).pv
        if ($value -and $value -ne '0.0.0.0') { return $value }
    }
    return $null
}
$version = Get-WebView2Version
if (-not $version) {
    $installer = Join-Path $env:RUNNER_TEMP 'MicrosoftEdgeWebview2Setup.exe'
    # Microsoft's documented Evergreen bootstrapper, downloaded only on the disposable CI runner.
    Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $installer
    $signature = Get-AuthenticodeSignature -FilePath $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw 'WebView2 installer signature did not validate as Microsoft.'
    }
    $process = Start-Process -FilePath $installer -ArgumentList '/silent', '/install' -PassThru
    if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'WebView2 installation timed out.' }
    $version = Get-WebView2Version
    if (-not $version) { throw "WebView2 is unavailable after installation (exit $($process.ExitCode))." }
}
Write-Output "WebView2 Runtime: $version"
