# Convert original MCM Emulator *.data RGB files to PNG
foreach( $file in Get-ChildItem -filter *.data ){

$width = 0
$height = 0
$skip = $false

    switch -Regex ($file.basename){

        '^spin'
        {
            $width = 172
            $height = 32
        }
        '^tape_empty'
        {
            $width = 409
            $height = 256
        }
        '^tape_loaded'
        {
            $width = 409
            $height = 256
        }
        '^tape_running'
        {
            $skip = $true;
        }
        '^printer'
        {
            $width = 944;
            $height = 700
        }
        '^panel'
        {
            $width = 932
            $height = 722
        }
        '^pr_error'
        {
            $width = 52
            $height = 24
        }
    }

   if( -Not $skip )
   {
        Write-Host "$($file.basename) $width $height"
        .\rgbToPng.ps1 -file $file.basename -width $width -height $height
   }
   else
   {     
        Write-Host "SKIP $($file.basename)"

        $skip = $false
   }

}
