<#
  Gera a imagem de teste da equipe (ilustracao propria, sem direitos de terceiros) usando System.Drawing
  do Windows PowerShell 5.1. Cena: rua noturna com buraco cheio de agua e metade dos postes apagados
  (problema relatado na escuta de 14/11/2026, Mooca). Saida: amostras\buraco_poste_apagado_mooca.jpg
  Uso: .\amostras\Gerar-Amostras.ps1
#>
Add-Type -AssemblyName System.Drawing
$destino = Join-Path $PSScriptRoot 'buraco_poste_apagado_mooca.jpg'
$w = 1024; $h = 640
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
function Cor($a, $r, $gg, $b) { [System.Drawing.Color]::FromArgb($a, $r, $gg, $b) }
function Pincel($c) { New-Object System.Drawing.SolidBrush $c }

# Ceu noturno (degrade)
$ceu = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Rectangle 0, 0, $w, 300), (Cor 255 10 14 35), (Cor 255 38 48 80), 90
$g.FillRectangle($ceu, 0, 0, $w, 300)
# Predios ao fundo
$rnd = New-Object System.Random 14112026
for ($x = 0; $x -lt $w; $x += 90) {
    $alt = 120 + $rnd.Next(0, 120)
    $g.FillRectangle((Pincel (Cor 255 25 28 40)), $x, 300 - $alt, 86, $alt)
    for ($j = 300 - $alt + 15; $j -lt 290; $j += 28) { for ($i = $x + 10; $i -lt $x + 76; $i += 24) {
        if ($rnd.Next(0, 3) -eq 0) { $g.FillRectangle((Pincel (Cor 255 200 170 90)), $i, $j, 12, 14) } } }
}
# Calcada e rua em perspectiva
$g.FillRectangle((Pincel (Cor 255 95 95 100)), 0, 300, $w, 40)
$rua = [System.Drawing.Point[]]@((New-Object System.Drawing.Point 0, 340), (New-Object System.Drawing.Point $w, 340), (New-Object System.Drawing.Point $w, $h), (New-Object System.Drawing.Point 0, $h))
$g.FillPolygon((Pincel (Cor 255 48 48 52)), $rua)
# Faixa central tracejada
for ($x = 20; $x -lt $w; $x += 120) { $g.FillRectangle((Pincel (Cor 255 220 200 80)), $x, 470, 70, 8) }
# Buraco com agua (elipses sobrepostas) e rachaduras
$g.FillEllipse((Pincel (Cor 255 22 20 18)), 330, 500, 360, 110)
$agua = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Rectangle 345, 512, 330, 90), (Cor 255 40 70 110), (Cor 255 15 30 55), 90
$g.FillEllipse($agua, 345, 512, 330, 88)
$g.FillEllipse((Pincel (Cor 120 255 230 150)), 470, 535, 70, 14)   # reflexo do unico poste aceso
$caneta = New-Object System.Drawing.Pen (Cor 255 25 25 25), 3
$g.DrawLines($caneta, [System.Drawing.Point[]]@((New-Object System.Drawing.Point 330, 555), (New-Object System.Drawing.Point 280, 540), (New-Object System.Drawing.Point 240, 560), (New-Object System.Drawing.Point 200, 548)))
$g.DrawLines($caneta, [System.Drawing.Point[]]@((New-Object System.Drawing.Point 690, 560), (New-Object System.Drawing.Point 745, 575), (New-Object System.Drawing.Point 790, 560), (New-Object System.Drawing.Point 840, 580)))
# Postes: um aceso (com halo) e dois apagados
function Poste($x, $aceso) {
    $g.FillRectangle((Pincel (Cor 255 70 70 75)), $x, 140, 10, 200)
    $g.FillRectangle((Pincel (Cor 255 70 70 75)), $x, 140, 55, 8)
    if ($aceso) {
        $g.FillEllipse((Pincel (Cor 60 255 220 130)), $x - 30, 110, 170, 170)
        $g.FillEllipse((Pincel (Cor 255 255 235 160)), $x + 40, 146, 26, 14)
    } else { $g.FillEllipse((Pincel (Cor 255 35 35 40)), $x + 40, 146, 26, 14) }
}
Poste 120 $true; Poste 520 $false; Poste 900 $false
# Legenda de autoria
$fonte = New-Object System.Drawing.Font 'Segoe UI', 15, ([System.Drawing.FontStyle]::Bold)
$g.FillRectangle((Pincel (Cor 170 0 0 0)), 0, 0, $w, 34)
$g.DrawString('Ilustracao propria - Zeladoria API Urbana (GS 2026.2, 3ESPH): buraco com agua e postes apagados', $fonte, (Pincel (Cor 255 255 255 255)), 10, 5)

$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
$params = New-Object System.Drawing.Imaging.EncoderParameters 1
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 88L
$bmp.Save($destino, $codec, $params)
$g.Dispose(); $bmp.Dispose()
$f = Get-Item $destino
"Gerado: $($f.FullName) | $($f.Length) bytes | SHA-256 $((Get-FileHash $destino -Algorithm SHA256).Hash)"
