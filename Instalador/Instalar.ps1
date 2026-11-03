Add-Type -AssemblyName System.Windows.Forms

$ErrorActionPreference = 'Stop'
$sourceDirectory = Join-Path $PSScriptRoot 'Aplicativo'

if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
    [System.Windows.Forms.MessageBox]::Show(
        'A pasta Aplicativo não foi encontrada. Extraia o pacote completo antes de instalar.',
        'Instalador Traz Base',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}

$defaultDirectory = Join-Path $env:LOCALAPPDATA 'Programs\TrazBase'
New-Item -ItemType Directory -Path $defaultDirectory -Force | Out-Null

$dialog = New-Object System.Windows.Forms.FolderBrowserDialog
$dialog.Description = 'Escolha a pasta onde os arquivos do Traz Base serão instalados.'
$dialog.SelectedPath = $defaultDirectory
$dialog.ShowNewFolderButton = $true

if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
    exit 0
}

$installDirectory = $dialog.SelectedPath
$application = Join-Path $installDirectory 'TrazBase.exe'

try {
    New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $sourceDirectory -Force | Copy-Item -Destination $installDirectory -Recurse -Force

    if (-not (Test-Path -LiteralPath $application -PathType Leaf)) {
        throw 'O executável TrazBase.exe não foi encontrado no pacote.'
    }

    $startMenuDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
    New-Item -ItemType Directory -Path $startMenuDirectory -Force | Out-Null
    $shortcutPath = Join-Path $startMenuDirectory 'Traz Base.lnk'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $application
    $shortcut.WorkingDirectory = $installDirectory
    $shortcut.IconLocation = $application
    $shortcut.Description = 'Preparação e transferência de bases Firebird'
    $shortcut.Save()

    [System.Windows.Forms.MessageBox]::Show(
        "Instalação concluída.`n`nLocal: $installDirectory`nAtalho criado no menu Iniciar.",
        'Instalador Traz Base',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        "Não foi possível concluir a instalação:`n$($_.Exception.Message)",
        'Instalador Traz Base',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}
