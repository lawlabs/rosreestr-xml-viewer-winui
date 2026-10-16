param([Parameter(Mandatory)][int]$AppPid, [string]$ArtifactDirectory = (Join-Path $PSScriptRoot '..\artifacts\ui-cadastral-lists'))
$ErrorActionPreference = 'Stop'
$env:WINAPP_UI_WORKFLOW_ID = 'rosreestr-lists-' + [guid]::NewGuid().ToString('N')
$ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
$results = [Collections.Generic.List[object]]::new()
function Ui([string[]]$Arguments) {
    $output = @(& winapp ui @Arguments -a $AppPid)
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    $output
}
function Report {
    $tree = Ui @('inspect','RootWebArea','--depth','3','--json') | ConvertFrom-Json
    $tree.windows[0].elements[0]
}
function Compare-Lists([string]$First, [string]$Second, [object[]]$Expected) {
    $previous = (Report).value
    Ui @('invoke','MoreButton') | Out-Null
    Ui @('invoke','CadLists') | Out-Null
    Ui @('wait-for','FirstCadList','-t','5000') | Out-Null
    Ui @('set-value','FirstCadList',$First) | Out-Null
    Ui @('set-value','SecondCadList',$Second) | Out-Null
    Ui @('invoke','PrimaryButton','--action','invoke') | Out-Null
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    do { $report = Report; if ($report.value -ne $previous) { break }; Start-Sleep -Milliseconds 100 }
    while ([DateTime]::UtcNow -lt $deadline)
    if ($report.value -eq $previous) { throw 'Report did not refresh' }
    $nodes = @($report.children[0].children)
    $titles = @('Первый список','Второй список','Только в первом','Только во втором','Общие')
    for ($i = 0; $i -lt $titles.Count; $i++) {
        $values = @($Expected[$i])
        $heading = $titles[$i] + ' · ' + $values.Count
        $index = [Array]::IndexOf([string[]]@($nodes.name), $heading)
        if ($index -lt 0) { throw "Missing heading: $heading" }
        $text = if ($values.Count -eq 0) { 'Нет номеров' } else { $values -join "`n" }
        if ($nodes[$index + 1].name -ne $text) { throw "Wrong members under $heading" }
    }
}
function Case([string]$Name, [scriptblock]$Action) {
    try { & $Action | Out-Null; $results.Add(@{name=$Name;status='PASS'}) }
    catch { $results.Add(@{name=$Name;status='FAIL';detail="$_"}) }
}
$a='50:21:0010203:456'; $b='77:01:0001001:1234'; $c='78:01:0000001:100'
try {
    Ui @('wait-for','DemoButton','-t','10000') | Out-Null
    Case 'Arbitrary text, duplicate removal and all three comparison groups' {
        Compare-Lists "Участки: $a; $b; повтор $a" "$b; $c" @(@($a,$b),@($b,$c),@($a),@($c),@($b))
        Ui @('screenshot','--output',(Join-Path $ArtifactDirectory 'comparison.png'),'--json') | Out-Null
    }
    Case 'Extraction with the second list empty' { Compare-Lists $a '' @(@($a),@(),@($a),@(),@()) }
    Case 'Comparison with the first list empty' { Compare-Lists '' $b @(@(),@($b),@(),@($b),@()) }
    Case 'Identical lists with whitespace and duplicate numbers' {
        Compare-Lists "50 : 21 : 0010203 : 456`n$a" $a @(@($a),@($a),@(),@(),@($a))
    }
    Case 'No false match inside a five-block chain or unrelated numbers' {
        Compare-Lists '50:21:0010203:456:789; дата 15.10.2026; 1200 м²' '' @(@(),@(),@(),@(),@())
    }
    $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ArtifactDirectory 'results.json') -Encoding utf8
    $results | ForEach-Object { Write-Output ($_.status + ' ' + $_.name + ' ' + $_.detail) }
    if (@($results | Where-Object status -eq 'FAIL').Count -gt 0) { exit 1 }
} finally { & winapp ui yield | Out-Null }
