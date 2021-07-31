Param(
    [string]$file,
    [int]$width,
    [int]$height
)

Add-Type -AssemblyName System.Windows.Forms
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$inputFile = "$file.data"

$infile = Get-Content $inputFile -Encoding Byte -ReadCount 0
$off = 0

$bmp = New-Object System.Drawing.Bitmap($width, $height)

for ($y = 0; $y -lt $height; $y++)
{
   for ($x = 0; $x -lt $width; $x++)
   {
     $r = $infile[$off]
     $off++
     $g = $infile[$off]
     $off++
     $b = $infile[$off]
     $off++
     $co = [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
     $bmp.SetPixel($x, $y, $co)
   }
}

#original code used fully qualified path
$saveFile = "$file.png"
$bmp.Save($saveFile,"png")
Write-Host "saved $saveFile"
