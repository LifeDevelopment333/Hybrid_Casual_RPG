Add-Type -AssemblyName PresentationCore
$previewRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Art/Characters/FantasyAdventurer'))
foreach ($clip in @('Idle','Walk')) {
    $encoder = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
    foreach ($framePath in (Get-ChildItem -LiteralPath (Join-Path $previewRoot 'PreviewFrames') -Filter ($clip+'_*.png') | Sort-Object Name)) {
        $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
        $bitmap.BeginInit(); $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
        $bitmap.UriSource = [Uri]::new($framePath.FullName); $bitmap.EndInit()
        $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('gif')
        $delay = if ($clip -eq 'Idle') { 19 } else { 7 }
        $metadata.SetQuery('/grctlext/Delay', [UInt16]$delay); $metadata.SetQuery('/grctlext/Disposal', [Byte]2)
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap,$null,$metadata,$null))
    }
    $outputPath = Join-Path $previewRoot ($clip+'.gif')
    $stream = [System.IO.File]::Open($outputPath,[System.IO.FileMode]::Create)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    $bytes = [System.IO.File]::ReadAllBytes($outputPath)
    $tableLength = if ($bytes[10] -band 128) { 3 * [Math]::Pow(2, ($bytes[10] -band 7) + 1) } else { 0 }
    $headerLength = [int](13 + $tableLength)
    [byte[]]$loopExtension = 0x21,0xFF,0x0B,0x4E,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2E,0x30,0x03,0x01,0x00,0x00,0x00
    $looped = [System.IO.MemoryStream]::new()
    $looped.Write($bytes,0,$headerLength); $looped.Write($loopExtension,0,$loopExtension.Length); $looped.Write($bytes,$headerLength,$bytes.Length-$headerLength)
    [System.IO.File]::WriteAllBytes($outputPath,$looped.ToArray()); $looped.Dispose()
    Get-Item -LiteralPath $outputPath | Select-Object FullName,Length
}
