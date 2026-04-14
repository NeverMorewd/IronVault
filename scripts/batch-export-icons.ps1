# batch-export-icons.ps1
# Requires Inkscape installed (https://inkscape.org, free)

$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
$svg      = "..\src\IronVault\Assets\images\noun-tank-97020.svg"
$out      = "..\src\IronVault\Assets\images"

$sizes = @(
    @{name="Square44x44Logo";    w=44;  h=44},
    @{name="Square71x71Logo";    w=71;  h=71},
    @{name="Square150x150Logo";  w=150; h=150},
    @{name="Square310x310Logo";  w=310; h=310},
    @{name="Wide310x150Logo";    w=310; h=150},
    @{name="StoreLogo";          w=50;  h=50}
)

foreach ($s in $sizes) {
    $outFile = "$out\$($s.name).png"
    # Export with exact pixel dimensions
    & $inkscape $svg --export-filename=$outFile --export-width=$($s.w) --export-height=$($s.h)
    Write-Host "Exported: $($s.name).png ($($s.w)x$($s.h))"
}