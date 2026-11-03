$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'TrazBase.iss'
$appExecutable = Join-Path $PSScriptRoot 'Aplicativo\TrazBase.exe'
$compilerCandidates = @(
    (Join-Path $env:USERPROFILE 'AppData\Local\Programs\Inno Setup 7\ISCC.exe'),
    (Join-Path $env:USERPROFILE 'AppData\Local\Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1

if (-not $compiler) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $compiler = $command.Source }
}
if (-not $compiler) {
    throw 'Inno Setup não foi encontrado. Instale o compilador Inno Setup 7 ou 6 e execute este script novamente.'
}
if (-not (Test-Path -LiteralPath $appExecutable -PathType Leaf)) {
    throw 'Os arquivos publicados não foram encontrados em Instalador\Aplicativo. Gere primeiro o pacote autocontido.'
}

& $compiler $scriptPath
if ($LASTEXITCODE -ne 0) {
    throw "A compilação do instalador falhou (código $LASTEXITCODE)."
}
