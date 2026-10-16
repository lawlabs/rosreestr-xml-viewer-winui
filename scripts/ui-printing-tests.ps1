param([Parameter(Mandatory)][int]$AppPid, [string]$ArtifactDirectory = (Join-Path $PSScriptRoot '..\artifacts\ui-printing'))
$ErrorActionPreference = 'Stop'
$env:WINAPP_UI_WORKFLOW_ID = 'rosreestr-printing-' + [guid]::NewGuid().ToString('N')
$ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
$results = [Collections.Generic.List[object]]::new()
$stage = 'Setup'
function Ui([string[]]$Arguments) {
    $output = @(& winapp ui @Arguments -a $AppPid)
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    $output
}
function Invoke-DialogPrimary([long]$Window) {
    $found = Ui @('search','1','--json','-w',"$Window") | ConvertFrom-Json
    $button = @($found.matches | Where-Object { $_.type -eq 'Button' -and $_.automationId -eq '1' })
    if ($button.Count -ne 1) { throw 'Native dialog primary button is ambiguous' }
    Ui @('invoke',$button[0].selector,'--action','invoke','-w',"$Window") | Out-Null
}
function Save-FileDialog([string]$Destination) {
    Ui @('wait-for','File name:','--type','Edit','-t','10000') | Out-Null
    $windows = Ui @('list-windows','--json') | ConvertFrom-Json
    $dialog = $windows | Where-Object className -eq '#32770' | Select-Object -First 1
    if (-not $dialog) { throw 'Save dialog HWND not found' }
    $fields = Ui @('search','File name:','--json','-w',"$($dialog.hwnd)") | ConvertFrom-Json
    $edit = @($fields.matches | Where-Object type -eq 'Edit')
    if ($edit.Count -ne 1) { throw 'File name edit is ambiguous' }
    Ui @('set-value',$edit[0].selector,$Destination,'-w',"$($dialog.hwnd)") | Out-Null
    Invoke-DialogPrimary $dialog.hwnd
}
function Assert-File([string]$Path) {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path -LiteralPath $Path) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path -LiteralPath $Path)) { throw "File not created: $Path" }
    if ((Get-Item -LiteralPath $Path).Length -lt 100) { throw 'Empty export' }
}
try {
    Ui @('invoke','78:01:0000001:100 · Помещение · 04-print-form.xml','--action','select') | Out-Null
    Ui @('invoke','ModePrintForm') | Out-Null
    Ui @('wait-for','Кадастровый номер:','--type','DataItem','-t','5000') | Out-Null

    $stage = 'Export XSL form PDF through native save dialog'
    $pdf = Join-Path $ArtifactDirectory 'printed-form.pdf'
    Ui @('invoke','ExportPdf') | Out-Null
    Ui @('wait-for','PdfFormat') | Out-Null
    if (((Ui @('get-value','PdfFormat')) -join '') -notmatch 'Печатная форма XML') { throw 'Wrong representation' }
    Ui @('invoke','PrimaryButton','--action','invoke') | Out-Null
    Save-FileDialog $pdf
    Ui @('wait-for','PDF сохранён','--type','Text','-t','10000') | Out-Null
    Assert-File $pdf
    $results.Add(@{name=$stage;status='PASS'})

    $stage = 'Export standalone HTML with the original table structure'
    $html = Join-Path $ArtifactDirectory 'printed-form.html'
    Ui @('invoke','MoreButton') | Out-Null
    Ui @('invoke','SavePrintFormHtml') | Out-Null
    Save-FileDialog $html
    Ui @('wait-for','HTML сохранён','--type','Text','-t','5000') | Out-Null
    Assert-File $html
    $text = [IO.File]::ReadAllText($html)
    if ($text -notmatch '<table' -or $text -notmatch 'rowspan' -or $text -match 'rowspan="0"') { throw 'Broken table export' }
    $results.Add(@{name=$stage;status='PASS'})

    $stage = 'Reimport identical XML discovers paired PDF without duplicating the row'
    $fixture = Join-Path $ArtifactDirectory 'fixture'
    New-Item -ItemType Directory -Force -Path $fixture | Out-Null
    $xml = Join-Path $fixture 'paired.xml'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\Samples\04-print-form.xml') -Destination $xml
    Copy-Item -LiteralPath $pdf -Destination (Join-Path $fixture 'paired.pdf')
    Ui @('invoke','OpenFiles') | Out-Null
    Ui @('wait-for','File name:','--type','Edit','-t','10000') | Out-Null
    $windows = Ui @('list-windows','--json') | ConvertFrom-Json
    $dialog = $windows | Where-Object className -eq '#32770' | Select-Object -First 1
    $fields = Ui @('search','File name:','--json','-w',"$($dialog.hwnd)") | ConvertFrom-Json
    $edit = @($fields.matches | Where-Object type -eq 'Edit')
    if ($edit.Count -ne 1) { throw 'Open file name edit is ambiguous' }
    Ui @('set-value',$edit[0].selector,$xml,'-w',"$($dialog.hwnd)") | Out-Null
    Invoke-DialogPrimary $dialog.hwnd
    Ui @('wait-for','ModeOriginalPdf','-p','IsEnabled','--value','True','-t','10000') | Out-Null
    Ui @('wait-for','PDF из полученного пакета','--type','Text','--root','PrintFormInfo','-t','5000') | Out-Null
    Ui @('wait-for','LibraryCount','--value','4 из 4 выписок') | Out-Null
    Ui @('screenshot','--output',(Join-Path $ArtifactDirectory 'original-pdf.png'),'--json') | Out-Null
    $results.Add(@{name=$stage;status='PASS'})

    $stage = 'Save original PDF byte for byte'
    $original = Join-Path $ArtifactDirectory 'original-copy.pdf'
    Ui @('invoke','ExportPdf') | Out-Null
    Ui @('wait-for','PdfFormat') | Out-Null
    if (((Ui @('get-value','PdfFormat')) -join '') -notmatch 'PDF из пакета') { throw 'Original PDF not selected by default' }
    Ui @('invoke','PrimaryButton','--action','invoke') | Out-Null
    Save-FileDialog $original
    Ui @('wait-for','PDF сохранён','--type','Text','-t','5000') | Out-Null
    Assert-File $original
    if ((Get-FileHash -LiteralPath $pdf).Hash -ne (Get-FileHash -LiteralPath $original).Hash) { throw 'Original PDF changed' }
    $results.Add(@{name=$stage;status='PASS'})
} catch {
    $results.Add(@{name=$stage;status='FAIL';detail="$_"})
} finally {
    & winapp ui yield | Out-Null
    $env:WINAPP_UI_WORKFLOW_ID = $null
}
$report = @{passed=@($results | Where-Object status -eq 'PASS').Count;failed=@($results | Where-Object status -eq 'FAIL').Count;results=$results.ToArray()}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ArtifactDirectory 'results.json') -Encoding utf8
$report | ConvertTo-Json -Depth 5
if ($report.failed -gt 0) { exit 1 }
