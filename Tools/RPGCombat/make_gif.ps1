Add-Type -AssemblyName PresentationCore
$combatPreviewRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Art/RPGCharacter'))
$combatFrames = Get-ChildItem -LiteralPath (Join-Path $combatPreviewRoot 'PreviewFrames') -Filter 'Combat_*.png' | Sort-Object Name
if ($combatFrames.Count -ne 60) { throw "Expected 60 frames, found $($combatFrames.Count)" }
$combatEncoder = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
foreach ($combatFrame in $combatFrames) {
    $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
    $bitmap.BeginInit(); $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bitmap.UriSource = [Uri]::new($combatFrame.FullName); $bitmap.EndInit()
    $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('gif')
    $metadata.SetQuery('/grctlext/Delay', [UInt16]7)
    $metadata.SetQuery('/grctlext/Disposal', [Byte]2)
    $combatEncoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap,$null,$metadata,$null))
}
$combatGif = Join-Path $combatPreviewRoot 'Combat_Combo.gif'
$stream = [IO.File]::Open($combatGif,[IO.FileMode]::Create)
try { $combatEncoder.Save($stream) } finally { $stream.Dispose() }
$bytes = [IO.File]::ReadAllBytes($combatGif)
$tableLength = if ($bytes[10] -band 128) { 3 * [Math]::Pow(2, ($bytes[10] -band 7) + 1) } else { 0 }
$headerLength = [int](13 + $tableLength)
[byte[]]$loopExtension = 0x21,0xFF,0x0B,0x4E,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2E,0x30,0x03,0x01,0x00,0x00,0x00
$looped = [IO.MemoryStream]::new()
$looped.Write($bytes,0,$headerLength); $looped.Write($loopExtension,0,$loopExtension.Length); $looped.Write($bytes,$headerLength,$bytes.Length-$headerLength)
[IO.File]::WriteAllBytes($combatGif,$looped.ToArray()); $looped.Dispose()
Get-Item -LiteralPath $combatGif | Select-Object FullName,Length
