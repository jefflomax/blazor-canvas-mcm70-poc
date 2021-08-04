Param(
    [string]$file,
    [int]$width,
    [int]$height
)

Add-Type -AssemblyName System.Windows.Forms
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$inputFile = "$file.data"

# Read binary
$infile = Get-Content $inputFile -Encoding Byte -ReadCount 0
$off = 0
$a = 255

$bmp = New-Object System.Drawing.Bitmap($width, $height)

# Input is RGB with no Alpha
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

#fully qualified path seems needed
$currPath = Get-Location
$saveFile = "$currPath\$file.png"
$bmp.Save($saveFile,"png")
Write-Host "saved $saveFile"
