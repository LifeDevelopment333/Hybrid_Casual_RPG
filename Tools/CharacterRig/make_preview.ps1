Add-Type -AssemblyName PresentationCore
$previewDir = Join-Path $PSScriptRoot '../../Art/Characters/TripoAdventurer/AnimationFix'
$encoder = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
foreach ($framePath in (Get-ChildItem -LiteralPath $previewDir -Filter 'Unity_Motion_*.png' | Sort-Object Name)) {
    $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
    $bitmap.BeginInit()
    $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bitmap.UriSource = [Uri]::new($framePath.FullName)
    $bitmap.EndInit()
    $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('gif')
    $metadata.SetQuery('/grctlext/Delay', [UInt16]8)
    $metadata.SetQuery('/grctlext/Disposal', [Byte]2)
    $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap, $null, $metadata, $null)
    $encoder.Frames.Add($frame)
}
$outputPath = Join-Path $previewDir 'Unity_Animation_Fixed.gif'
$stream = [System.IO.File]::Open($outputPath,[System.IO.FileMode]::Create)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
# Add the standard Netscape infinite-loop extension after the global color table.
$bytes = [System.IO.File]::ReadAllBytes($outputPath)
$tableLength = if ($bytes[10] -band 128) { 3 * [Math]::Pow(2, ($bytes[10] -band 7) + 1) } else { 0 }
$headerLength = [int](13 + $tableLength)
[byte[]]$loopExtension = 0x21,0xFF,0x0B,0x4E,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2E,0x30,0x03,0x01,0x00,0x00,0x00
$looped = [System.IO.MemoryStream]::new()
$looped.Write($bytes,0,$headerLength);$looped.Write($loopExtension,0,$loopExtension.Length);$looped.Write($bytes,$headerLength,$bytes.Length-$headerLength)
[System.IO.File]::WriteAllBytes($outputPath,$looped.ToArray());$looped.Dispose()
Get-Item -LiteralPath $outputPath | Select-Object FullName,Length
