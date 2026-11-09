# Desenvolvimento

Guia para quem compila, publica e mantém o Traz Base.

## Pré-requisitos

- Windows 10 ou superior (x64).
- .NET 10 SDK.
- Serviço Firebird local ativo (apenas para executar a aplicação; não é necessário para compilar).
- Inno Setup 6 ou 7 (apenas para gerar o instalador).
- Opcional: GitHub CLI/git para publicar releases no repositório `BrunoPasseti21/Traz-base`.

## Comandos

```powershell
# Restaurar, compilar e rodar em desenvolvimento
dotnet run --project TrazBase.App/TrazBase.App.csproj

# Compilar (Debug por padrão; use -c Release)
dotnet build TrazBase.App/TrazBase.App.csproj -c Release

# Compilar e rodar testes (MSTest; lógica pura de TrazBase.App — ver docs/TESTES.md)
dotnet test TrazBase.Tests/TrazBase.Tests.csproj

# Publicar pacote autocontido win-x64 em Instalador/Aplicativo
dotnet publish TrazBase.App/TrazBase.App.csproj -c Release -r win-x64 --self-contained true -o Instalador/Aplicativo

# Gerar o instalador (exige Inno Setup 6/7; localiza ISCC.exe automaticamente)
powershell -ExecutionPolicy Bypass -File Instalador/Compilar-Instalador.ps1
```

`Instalador/Aplicativo/` e `Instalador/TrazBase-Setup.exe` são artefatos locais e estão no `.gitignore`; o instalador distribuído é o asset `TrazBase-Setup.exe` da GitHub Release.

## Lançar uma atualização

1. Incremente `<Version>` em `TrazBase.App/TrazBase.App.csproj`.
2. Atualize `AppVersion` e `AppFileVersion` em `Instalador/TrazBase.iss` (manter iguais; `AppFileVersion` no formato `X.Y.Z.0`).
3. Publique novamente em `Instalador/Aplicativo` (comando acima).
4. Rode `Instalador/Compilar-Instalador.ps1` para gerar `Instalador/TrazBase-Setup.exe`.
5. Crie a release no GitHub com tag `vX.Y.Z` e anexe `TrazBase-Setup.exe`.
6. Os clientes detectam a release pela API `releases/latest` na próxima abertura; o asset precisa terminar em `Setup.exe` e a URL precisa ser HTTPS.
7. O instalador bloqueia downgrades e permite reinstalar a mesma versão.

A versão exibida no aplicativo vem do `AssemblyVersion` (`GetName().Version`), no formato `X.Y.Z`.

## Estrutura do repositório

```
TrazBase.App/          Código da aplicação (WinForms, net10.0-windows)
TrazBase.Tests/        Testes (MSTest; cobre a lógica pura exportada via InternalsVisibleTo)
Imagens/               SVGs dos ícones, Baixando.gif, icone/icone_supra.ico
Instalador/            TrazBase.iss, Compilar-Instalador.ps1, Instalar.ps1/.bat, LEIA-ME.txt
docs/                  Esta documentação
Traz-base-sl.bat       BAT legado (fora do Git; mantido localmente pelo dono)
README.md              Visão geral e instruções básicas
```

Arquivos gerados por publish/instalação não devem ser commitados (ver `.gitignore`). Bases `.fdb`/`.fbk` e compactados também estão fora do Git por poderem conter dados da empresa.

## Convenções de código

- Nullable habilitado; `ImplicitUsings` habilitado.
- Lógica pura, sem dependência de UI, fica em classes `internal static` (`BaseOperationRules`, `ArchivePathSafety`, `GitHubReleaseUpdater.ParseLatestRelease`) para permitir testes unitários.
- Componentes visuais novos seguem [Padrões de interface](PADROES-DE-INTERFACE.md): cores via paleta documentada, tipografia via `UiTypography`, botões com o padrão único, foco em tema claro **e** escuro.
- O acesso ao Registro fica restrito a `HKCU\SIC\CONEXAO`; qualquer nova chave precisa ser documentada em [Configuração](CONFIGURACAO.md).
- Senhas e segredos nunca em configuração, logs ou mensagens. Use DPAPI (`AppSettings.SetFirebirdPassword`) para persistir senha.
- Operações longas: `async`/`await` + `CancellationToken`; nunca bloquear a thread da UI.
- Diálogos: usar sempre `AppMessageBox`/`MessageBox` interno (que roteia para o diálogo próprio), nunca `System.Windows.Forms.MessageBox` nativo — a classe interna homônima existe justamente para isso.

## Diagnóstico

- Logs da aplicação existem apenas na tela (painéis "Andamento da operação" / "Andamento da redefinição"); não há arquivo de log ainda.
- Erros de conexão Firebird indicam, em geral: serviço parado, caminho da base incorreto, usuário/senha inválidos ou base corrompida.
- Falhas de Registro (`HKCU`) indicam permissão do usuário na chave ou política de máquina.
- A atualização automática registra falhas apenas na UI do botão de atualização; indisponibilidade do GitHub é ignorada silenciosamente por design.
- O instalador gera log próprio do Inno Setup (`SetupLogging=yes`) em `%TEMP%`.
