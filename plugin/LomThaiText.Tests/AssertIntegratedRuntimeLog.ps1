param(
    [Parameter(Mandatory = $true)]
    [string]$LogPath
)

$content = Get-Content -LiteralPath $LogPath -Raw
$required = @(
    '[tick] Ticker.Update is running',
    '[thai-shape] initialized',
    '[thai-shape] shaped',
    '[thai-mesh] /UI/Layer_1/Buttons/Setting/Text codes=0E15 0E31 0E49 0E07 0E04 0E48 0E32',
    '[thai-mesh] raisedStackedTones=1'
)
$missing = @($required | Where-Object { $content.IndexOf($_, [StringComparison]::Ordinal) -lt 0 })
if ($missing.Count -gt 0) {
    throw "Integrated Thai shaping evidence missing: $($missing -join ', ')"
}
if ($content -match '\[thai-shape\].*(failed|error)') {
    throw 'Integrated Thai shaping logged a failure.'
}
if ($content -match '\[thai-mesh\].*Setting/Text codes=.*F7[0-9A-F]{2}') {
    throw 'Setting label still contains a substituted PUA Thai mark.'
}
Write-Output 'PASS: integrated Thai shaping runtime log'
