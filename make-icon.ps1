Add-Type -AssemblyName System.Drawing
$iconPath = Join-Path $PSScriptRoot 'TradingClock.ico'
$stream = [IO.File]::Create($iconPath)
$writer = New-Object IO.BinaryWriter($stream)
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $bmp = New-Object Drawing.Bitmap($size, $size)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform($size / 256.0, $size / 256.0)
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc(8,8,64,64,180,90)
    $path.AddArc(184,8,64,64,270,90)
    $path.AddArc(184,184,64,64,0,90)
    $path.AddArc(8,184,64,64,90,90)
    $path.CloseFigure()
    $background = New-Object Drawing.Drawing2D.LinearGradientBrush([Drawing.Point]::new(0,0),[Drawing.Point]::new(256,256),[Drawing.Color]::FromArgb(33,52,72),[Drawing.Color]::FromArgb(8,17,29))
    $g.FillPath($background,$path)
    $edge = New-Object Drawing.Pen([Drawing.Color]::FromArgb(65,92,114),2)
    $g.DrawPath($edge,$path)
    $ring = New-Object Drawing.Pen([Drawing.Color]::FromArgb(53,229,183),12)
    $g.DrawEllipse($ring,48,40,160,160)
    $ticks = New-Object Drawing.Pen([Drawing.Color]::FromArgb(164,197,209),5)
    for ($i=0; $i -lt 12; $i++) {
        $a = $i * [Math]::PI / 6
        $r = 64; if ($i % 3 -eq 0) { $r = 58 }
        $g.DrawLine($ticks,[single](128 + $r * [Math]::Sin($a)),[single](120 - $r * [Math]::Cos($a)),[single](128 + 68 * [Math]::Sin($a)),[single](120 - 68 * [Math]::Cos($a)))
    }
    $hour = New-Object Drawing.Pen([Drawing.Color]::FromArgb(238,247,250),12)
    $minute = New-Object Drawing.Pen([Drawing.Color]::FromArgb(255,199,92),9)
    $hour.StartCap = $hour.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $minute.StartCap = $minute.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($hour,128,120,100,93)
    $g.DrawLine($minute,128,120,164,79)
    $dot = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(255,199,92))
    $g.FillEllipse($dot,119,111,18,18)
    foreach ($x in @(100,124,148)) { $g.FillEllipse($dot,$x,218,8,8) }
    $memory = New-Object IO.MemoryStream
    $bmp.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $images += ,$memory.ToArray()
    if ($size -eq 256) { $bmp.Save((Join-Path $PSScriptRoot 'TradingClock-icon.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $memory.Dispose(); $g.Dispose(); $bmp.Dispose(); $path.Dispose(); $background.Dispose(); $edge.Dispose(); $ring.Dispose(); $ticks.Dispose(); $hour.Dispose(); $minute.Dispose(); $dot.Dispose()
}
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i=0; $i -lt $sizes.Count; $i++) {
    $dimension = $sizes[$i] % 256
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($data in $images) { $writer.Write([byte[]]$data) }
$writer.Dispose()

