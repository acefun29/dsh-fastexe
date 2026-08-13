# Rebuild dsh.exe from launcher.cs using the system .NET Framework compiler (C# 5).
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc (requires .NET Framework 4.x, built into Windows 10/11)."
}
& $csc /nologo /optimize+ /target:exe /out:"$here\dsh.exe" "$here\launcher.cs"
if ($LASTEXITCODE -ne 0) { throw "compile failed with exit code $LASTEXITCODE" }
Write-Host "OK: $here\dsh.exe"
