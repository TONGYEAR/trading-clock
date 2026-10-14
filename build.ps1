$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '未找到 .NET Framework C# 编译器。' }
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:winexe /out:TradingClock.exe /win32icon:TradingClock.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll TradingClock.cs
    if ($LASTEXITCODE -ne 0) { throw '构建失败。请关闭正在运行的时钟后再试。' }
} finally { Pop-Location }
