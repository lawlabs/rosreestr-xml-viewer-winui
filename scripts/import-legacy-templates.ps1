param([Parameter(Mandatory)][string]$LegacyProject)
$ErrorActionPreference = 'Stop'
$source = Join-Path $LegacyProject 'RosreestrXMLViewer\Resources\XLT\XSLRC'
if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "Legacy template cache not found: $source" }
$target = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Templates'))
$archive = Join-Path $target 'Archive'
New-Item -ItemType Directory -Force -Path $archive | Out-Null
$resources = [Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object { $_.Length -gt 0 -and $_.Extension -in '.xsl','.xsd','.xml' }) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\','/')
    # A folder named ZU is interpreted as a Zulu language qualifier by MSIX PRI.
    # Keep the source URI unchanged; use a neutral name on disk.
    $storedRelative = $relative.Replace('/ZU/','/Land/')
    $destination = Join-Path $archive $storedRelative
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
    $resources.Add([ordered]@{ uri='https://' + $relative; file='Archive/' + $storedRelative; sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash })
}
# The old cache contains references to sibling copies of the same dictionaries.
# Resolve only a unique, byte-identical filename; keep every alias in the manifest.
$missing = [Collections.Generic.List[string]]::new()
foreach ($resource in @($resources.ToArray() | Where-Object { $_.file.EndsWith('.xsl') })) {
    $text = [IO.File]::ReadAllText((Join-Path $target $resource.file))
    $references = @([regex]::Matches($text, '<xsl:(?:include|import)\s+href\s*=\s*[''"]([^''"]+)[''"]') | ForEach-Object { $_.Groups[1].Value })
    $references += @([regex]::Matches($text, 'document\(\s*[''"]([^''"]+)[''"]') | ForEach-Object { $_.Groups[1].Value })
    foreach ($reference in $references) {
        $uri = [uri]::new([uri]$resource.uri,$reference).AbsoluteUri
        if ($resources | Where-Object uri -eq $uri) { continue }
        $name = [IO.Path]::GetFileName(([uri]$uri).AbsolutePath)
        $candidates = @($resources | Where-Object { [IO.Path]::GetFileName($_.file) -eq $name })
        if ($candidates.Count -gt 0 -and @($candidates.sha256 | Select-Object -Unique).Count -eq 1) {
            $resources.Add([ordered]@{uri=$uri;file=$candidates[0].file;sha256=$candidates[0].sha256;alias=$true})
        } else { $missing.Add($uri) }
    }
}
$portal='https://portal.rosreestr.ru/xsl/'
$forms = @(
    @{id='egrp-list-07';name='ЕГРП: переход прав · 07';root='Extract';namespace='';version='07';source=($portal+'EGRP/Reestr_Extract_List/07/Common.xsl');origin='RosreestrArchive'},
    @{id='kpoks-04';name='Кадастровый паспорт ОКС · 4.0.1';root='KPOKS';namespace='urn://x-artefacts-rosreestr-ru/outgoing/kpoks/4.0.1';version='';source=($portal+'EGRP/Reestr_Extract_Big/ROOM/07/Common.xsl');origin='RosreestrArchive';aliases=@($portal+'EGRP/Reestr_Extract_Gkn/OKS/07/Common.xsl')},
    @{id='kvoks-03';name='Кадастровая выписка ОКС · 3.0.1';root='KVOKS';namespace='urn://x-artefacts-rosreestr-ru/outgoing/kvoks/3.0.1';version='';source=($portal+'EGRP/Reestr_Extract_Big/OKS/07/Common.xsl');origin='RosreestrArchive'},
    @{id='kvzu-07';name='Кадастровая выписка участка · 7.0.1';root='KVZU';namespace='urn://x-artefacts-rosreestr-ru/outgoing/kvzu/7.0.1';version='';source=($portal+'EGRP/Reestr_Extract_Big/ZU/07/Common.xsl');origin='RosreestrArchive'}
)
foreach($kind in 'extract_about_property_build','extract_about_property_room','extract_about_property_car_parking_space','extract_transfer_rights_property') {
    $forms += @{id=$kind;name=($kind+' · архивный сторонний XSL');root=$kind;namespace='';version='';source=('https://legalcalc.ru/xslt/'+$kind+'.xsl');origin='ThirdPartyArchive'}
}
@{version=1;importedFrom='rosreestr-xml-viewer-delphi/Resources/XLT/XSLRC';resources=$resources.ToArray();forms=$forms} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $target 'catalog.json') -Encoding utf8
@{resources=$resources.Count;forms=$forms.Count;unresolved=@($missing | Select-Object -Unique)} | ConvertTo-Json -Depth 3
