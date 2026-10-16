param([Parameter(Mandatory)][int]$AppPid, [string]$ArtifactDirectory = (Join-Path $PSScriptRoot '..\artifacts\ui'))
$ErrorActionPreference = 'Stop'
$env:WINAPP_UI_WORKFLOW_ID = 'rosreestr-ui-' + [guid]::NewGuid().ToString('N')
$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()
$ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
function Ui([string[]]$Arguments) {
    $out = @(& winapp ui @Arguments -a $AppPid)
    if ($LASTEXITCODE -ne 0) { throw ($out -join "`n") }
    $out
}
function Case([string]$Name, [scriptblock]$Action) {
    try { & $Action | Out-Null; $results.Add(@{name=$Name; status='PASS'}) }
    catch { $results.Add(@{name=$Name; status='FAIL'; detail="$_"}) }
}
function Shot([string]$Name) {
    $path = Join-Path $ArtifactDirectory $Name
    Ui @('screenshot','--output',$path,'--json') | Out-Null
    if (-not (Test-Path -LiteralPath $path)) { throw 'Screenshot not saved' }
    $screenshots.Add($path)
}
try {
    Case 'Visible responsive WinUI window' {
        $process = Get-Process -Id $AppPid
        if (-not $process.Responding -or $process.MainWindowHandle -eq 0) { throw 'No responsive window' }
        Ui @('wait-for','DemoButton','-t','5000')
    }
    Case 'Import demonstration fixtures' {
        Ui @('invoke','DemoButton')
        Ui @('wait-for','StatusText','--contains','--value','Документов: 4','-t','10000')
        Ui @('wait-for','ExportPdf','-p','IsEnabled','--value','True')
        Ui @('invoke','ModeSummary')
        Shot '01-card.png'
    }
    Case 'Live filter' {
        Ui @('set-value','SearchDocuments','77:01')
        Ui @('wait-for','LibraryCount','--value','1 из 4 выписок','-t','5000')
    }
    Case 'Empty search and recovery' {
        Ui @('set-value','SearchDocuments','no-such-object')
        Ui @('wait-for','LibraryCount','--value','0 из 4 выписок')
        Ui @('set-value','SearchDocuments','')
        Ui @('wait-for','LibraryCount','--value','4 из 4 выписок')
    }
    Case 'Select land and render geometry' {
        Ui @('invoke','50:21:0010203:456 · Земельный участок · 01-land.xml','--action','select')
        Ui @('invoke','ModeGeometry')
        Ui @('wait-for','DocumentSubtitle','--contains','--value','1 контур','-t','5000')
        Ui @('wait-for','Система координат:','--type','Text','-t','5000')
        Shot '02-geometry.png'
    }
    Case 'Fields and source modes' {
        Ui @('invoke','ModeFields')
        Ui @('wait-for','Поля XML','--type','Text','-t','5000')
        Ui @('invoke','ModeSource')
        Ui @('wait-for','Оригинальные байты доступны','--type','Text','-t','5000')
    }
    Case 'Attachments state' {
        Ui @('invoke','ModeAttachments')
        Ui @('wait-for','AttachmentsList')
        Ui @('invoke','DownloadGraphics')
        Ui @('wait-for','В XML нет незагруженных','--type','Text','-t','5000')
    }
    Case 'Compare two selected extracts' {
        Ui @('invoke','ModeSummary')
        Ui @('invoke','50:21:0010203:456 · Земельный участок · 01-land.xml','--action','select')
        Ui @('invoke','CompareDocs')
        Ui @('wait-for','CompareFirst')
        Ui @('invoke','Primary')
        Ui @('wait-for','Сравнение выписок','--type','Text','-t','5000')
        Shot '03-comparison.png'
    }
    Case 'Theme change and geometry' {
        Ui @('invoke','MoreButton')
        Ui @('wait-for','SwitchTheme')
        Ui @('invoke','SwitchTheme')
        Ui @('invoke','ModeGeometry')
        Ui @('wait-for','Система координат:','--type','Text','-t','5000')
        Shot '04-theme.png'
    }
    Case 'Archived print form has real tables and provenance' {
        Ui @('invoke','78:01:0000001:100 · Помещение · 04-print-form.xml','--action','select')
        Ui @('invoke','ModePrintForm')
        Ui @('wait-for','Архивный XSL Росреестра · ЕГРП','--type','Text','-t','10000')
        Ui @('wait-for','Кадастровый номер:','--type','DataItem','-t','5000')
        Shot '05-print-form.png'
    }
    Case 'PDF export exposes the XSL form choice' {
        Ui @('invoke','ExportPdf')
        Ui @('wait-for','PdfFormat')
        try {
            $format = (Ui @('get-value','PdfFormat')) -join ''
            if ($format -notmatch 'Печатная форма XML') { throw 'Wrong default PDF representation' }
        } finally { Ui @('invoke','Отмена','--action','invoke') }
    }
    Case 'Unsupported print form does not fall back to a card' {
        Ui @('invoke','77:01:0001001:1234 · Помещение · 03-room.xml','--action','select')
        Ui @('invoke','ModePrintForm')
        Ui @('wait-for','Пакет печатной формы для extract_base_params_room','--type','Text','--root','PrintFormInfo','-t','5000')
        Shot '06-unsupported-form.png'
    }
} finally {
    & winapp ui yield | Out-Null
    if ($LASTEXITCODE -ne 0) { $results.Add(@{name='Release UI';status='FAIL'}) }
    $env:WINAPP_UI_WORKFLOW_ID = $null
}
$report = @{passed=@($results|Where-Object status -eq 'PASS').Count;failed=@($results|Where-Object status -eq 'FAIL').Count;results=$results.ToArray();screenshots=$screenshots.ToArray()}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $ArtifactDirectory 'results.json') -Encoding utf8
$report | ConvertTo-Json -Depth 6
if ($report.failed -gt 0) { exit 1 }
