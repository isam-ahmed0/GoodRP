# Generates branded BMP images for the GoodRP Inno Setup wizard.
# No external dependencies (no System.Drawing needed).
param(
    [string]$OutDir = $PSScriptRoot
)

$bg     = @(26, 26, 46)    # #1a1a2e
$bg2    = @(42, 42, 78)    # slightly lighter for gradient
$accent = @(88, 101, 242)  # #5865F2
$white  = @(255, 255, 255)

function New-PixelBuffer {
    param([int]$w, [int]$h, [int[]]$fill)
    $buf = New-Object 'object[]' $h
    for ($y = 0; $y -lt $h; $y++) {
        $row = New-Object 'object[]' $w
        for ($x = 0; $x -lt $w; $x++) { $row[$x] = @($fill[0], $fill[1], $fill[2]) }
        $buf[$y] = $row
    }
    return $buf
}

function Set-Px {
    param($buf, [int]$x, [int]$y, $c)
    if ($y -ge 0 -and $y -lt $buf.Length -and $x -ge 0 -and $x -lt $buf[0].Length) {
        $buf[$y][$x] = @($c[0], $c[1], $c[2])
    }
}

function Fill-Rect {
    param($buf, [int]$x0, [int]$y0, [int]$x1, [int]$y1, $c)
    for ($y = $y0; $y -le $y1; $y++) {
        for ($x = $x0; $x -le $x1; $x++) { Set-Px $buf $x $y $c }
    }
}

function Save-Bmp {
    param($buf, [string]$path)
    $h = $buf.Length
    $w = $buf[0].Length
    $rowSize = [Math]::Floor(($w * 3 + 3) / 4) * 4
    $pixSize = $rowSize * $h
    $fileSize = 54 + $pixSize

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    # BITMAPFILEHEADER
    $bm = [System.Text.Encoding]::ASCII.GetBytes("BM")
    $bw.Write($bm, 0, 2)
    $bw.Write([uint32]$fileSize)
    $bw.Write([uint16]0); $bw.Write([uint16]0)
    $bw.Write([uint32]54)
    # BITMAPINFOHEADER
    $bw.Write([uint32]40)
    $bw.Write([int32]$w)
    $bw.Write([int32]$h)
    $bw.Write([uint16]1)
    $bw.Write([uint16]24)
    $bw.Write([uint32]0)
    $bw.Write([uint32]$pixSize)
    $bw.Write([int32]2835); $bw.Write([int32]2835)
    $bw.Write([uint32]0); $bw.Write([uint32]0)

    for ($y = $h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $w; $x++) {
            $c = $buf[$y][$x]
            $bw.Write([byte]$c[2]); $bw.Write([byte]$c[1]); $bw.Write([byte]$c[0])  # BGR
        }
        # padding
        $pad = $rowSize - $w * 3
        for ($p = 0; $p -lt $pad; $p++) { $bw.Write([byte]0) }
    }
    [System.IO.File]::WriteAllBytes($path, $ms.ToArray())
    $bw.Close()
}

# --- Small logo (square) : dark rounded-ish square + accent border + "G" ---
$size = 120
$small = New-PixelBuffer $size $size $bg
# accent border (inset frame)
Fill-Rect $small 4 4 ($size-5) ($size-5) $bg          # clear inside to bg in case
Fill-Rect $small 4 4 ($size-5) 6 $accent
Fill-Rect $small 4 ($size-7) ($size-5) ($size-5) $accent
Fill-Rect $small 4 4 6 ($size-5) $accent
Fill-Rect $small ($size-7) 4 ($size-5) ($size-5) $accent

# 5x7 "G" bitmap font, scaled x10
$g = @(
    "01110",
    "10001",
    "10000",
    "10111",
    "10001",
    "10001",
    "01110"
)
$scale = 10
$gw = 5 * $scale
$gh = 7 * $scale
$ox = [Math]::Floor(($size - $gw) / 2)
$oy = [Math]::Floor(($size - $gh) / 2)
for ($r = 0; $r -lt 7; $r++) {
    for ($c = 0; $c -lt 5; $c++) {
        if ($g[$r][$c] -eq '1') {
            Fill-Rect $small ($ox + $c*$scale) ($oy + $r*$scale) ($ox + $c*$scale + $scale - 1) ($oy + $r*$scale + $scale - 1) $white
        }
    }
}
Save-Bmp $small (Join-Path $OutDir "goodrp-small.bmp")

# --- Sidebar (used by classic style fallback): vertical gradient + wordmark-ish block ---
$sw = 164; $sh = 314
$side = New-Object 'object[]' $sh
for ($y = 0; $y -lt $sh; $y++) {
    $t = $y / ($sh - 1)
    $R = [int]($bg[0] + ($bg2[0] - $bg[0]) * $t)
    $G = [int]($bg[1] + ($bg2[1] - $bg[1]) * $t)
    $B = [int]($bg[2] + ($bg2[2] - $bg[2]) * $t)
    $row = New-Object 'object[]' $sw
    for ($x = 0; $x -lt $sw; $x++) { $row[$x] = @($R, $G, $B) }
    $side[$y] = $row
}
# accent rounded block near top-center as a logo mark
Fill-Rect $side 52 40 112 100 $accent
$g2 = @(
    "01110",
    "10001",
    "10000",
    "10111",
    "10001",
    "10001",
    "01110"
)
$gs = 8
$gx0 = 64; $gy0 = 52
for ($r = 0; $r -lt 7; $r++) {
    for ($c = 0; $c -lt 5; $c++) {
        if ($g2[$r][$c] -eq '1') {
            Fill-Rect $side ($gx0 + $c*$gs) ($gy0 + $r*$gs) ($gx0 + $c*$gs + $gs - 1) ($gy0 + $r*$gs + $gs - 1) $white
        }
    }
}
Save-Bmp $side (Join-Path $OutDir "goodrp-sidebar.bmp")

Write-Host "Generated goodrp-small.bmp and goodrp-sidebar.bmp in $OutDir"
