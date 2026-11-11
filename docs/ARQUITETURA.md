# Arquitetura

## Plataforma

- C# com .NET **10** (`net10.0-windows`), WinForms, DPI `PerMonitorV2`.
- Aplicação desktop para Windows 10+, distribuída como executável **self-contained win-x64** dentro de um instalador Inno Setup (`TrazBase-Setup.exe`), com autoatualização via GitHub Releases.
- `AssemblyName` = `TrazBase`; versão atual **0.3.0** (`TrazBase.App.csproj`), espelhada em `Instalador/TrazBase.iss`.

## Organização do código

Solução única, projeto único (`TrazBase.App`); não há separação em assemblies de UI/Application/Infrastructure. As responsabilidades estão separadas por arquivo, e a lógica pura está isolada em classes `internal static` para permitir testes (`InternalsVisibleTo("TrazBase.Tests")`):

| Arquivo | Responsabilidade |
|---|---|
| `Program.cs` | Ponto de entrada: carrega `AppSettings`, aplica o modo de cor (claro/escuro) antes de `ApplicationConfiguration.Initialize()` e abre `MainForm`. |
| `MainForm.cs` | Janela principal: layout, menu lateral, tema claro/escuro, orquestração dos fluxos (Transferir, Alterar base, Redefinir base, Comandos SQL), cópia/extração, Registro, SQL, versão, atualização. É um arquivo grande (~1 700 linhas) e concentra a UI e o fluxo. |
| `AppSettings.cs` | Configuração persistida em JSON (`%APPDATA%\TrazBase\settings.json`), proteção da senha com DPAPI (`CurrentUser`), migração de scripts legados. |
| `BaseOperationRules.cs` | Regras puras de operação: resolução do destino local com proteção de path traversal, lista de bases de apoio a copiar (`ARQEMP.FDB`, `ARQOCOR.FDB`), interpretação do diálogo de conflito de destino (`ExistingDestinationAction`). |
| `ArchivePathSafety` (em `BaseOperationRules.cs`) | Normalização e validação do caminho de saída de entradas de arquivos compactados (rejeita caminhos absolutos e `..`). |
| `RestoreOperationRules.cs` | Regras puras da restauração: resolução da pasta dos backups (`BKP` quando existe, senão a própria pasta selecionada), reconhecimento do tipo de base pelo nome do arquivo (`ARQEMP`/`ARQOCOR`/`ARQSIST`, sem distinção de maiúsculas/minúsculas), mapeamento para o `.FDB` ao lado do `.FBK`, detecção de destinos duplicados. |
| `MultiBaseOperationRules.cs` | Regras puras do tratamento multi-base: detecção de pastas `ARQ##` (dois dígitos, na raiz da SL), caminho da `ARQ00`, ARQSIST como `ARQ01\ARQSIST.FDB`, cálculo das chaves `BASE<n>` a gravar e a remover. |
| `UpdateSicOperationRules.cs` | Regras puras da "Atualizar SIC": a lista dos quatro executáveis `.zip` e onde ficam origem (com versão) e destino. |
| `GitHubReleaseUpdater.cs` | Consulta a API de releases do GitHub, escolhe o asset `*Setup.exe` mais recente e baixa o instalador com progresso. `ParseLatestRelease` é testável. |
| `AppMessageBox.cs` | Diálogos próprios (`AppMessageDialog`) que substituem o `MessageBox` nativo, incluindo o diálogo de conflito de destino (`ShowFolderConflict`). |
| `SvgIconLoader.cs` | Renderiza os ícones SVG (apenas `<path fill>`, com `translate()`) para `Bitmap` — os ícones do menu não usam biblioteca externa. |
| `ThemedScrollingControls.cs` | Controles com tema próprio: `ThemedProgressBar` (barra/marquee), `ThemedListView`, `ThemedTextBox`, `ThemedRichTextBox`, `PlainNavigationButton` e a interface `IThemedScrollingControl`. |
| `UiTypography.cs` | Tipografia centralizada (Segoe UI Variable/Segoe UI com fallback), usada por toda a UI. |

Projeto de testes: `TrazBase.Tests` (MSTest 4, `net10.0-windows`, referencia `TrazBase.App`). Cobre a lógica pura: `BaseOperationRules` (resolução de destino, arquivos de apoio, ação de conflito), `ArchivePathSafety` (path traversal), `RestoreOperationRules` (reconhecimento/mapeamento de `.FBK`), `GitHubReleaseUpdater.ParseLatestRelease` e a proteção DPAPI de `AppSettings`. `MainForm` e a UI não têm cobertura. Detalhes em [Testes](TESTES.md).

Recursos: `Imagens/*.svg`, `Imagens/Baixando.gif`, `Imagens/icone/icone_supra.ico` (copiados para a saída). O instalador reside em `Instalador/` (`TrazBase.iss`, `Compilar-Instalador.ps1`, `Instalar.ps1`/`Instalar.bat` para a alternativa ZIP, `LEIA-ME.txt`).

## Integrações

### Notificações da bandeja

Com a janela minimizada, qualquer diálogo que peça atenção dispara sempre (sem configuração) um balloon da bandeja (`NotifyIcon`, clicável para restaurar a janela). Notificações em primeiro plano não são exibidas. Falhas no mecanismo de notificação nunca interrompem operações (tudo falha silenciosamente para o usuário).

### Processos externos

Não há processos externos no caminho crítico de negócio. O único processo iniciado é o instalador durante a atualização (`Verb=runas`, argumentos silenciosos do Inno Setup). Toda leitura/escrita de banco é via provider ADO.NET; toda extração é via bibliotecas gerenciadas.

### Arquivos e rede

- Cópia recursiva própria (`FileStream`, buffer de 1 MB, `FileOptions.Asynchronous | SequentialScan`), com progresso por bytes e cancelamento por `CancellationToken`. Não usa `robocopy`.
- Extração ZIP: `System.IO.Compression.ZipArchive`. Extração RAR: SharpCompress (`ReaderFactory.OpenAsyncReader`). Entradas são validadas contra path traversal antes de gravar.
- Timestamps (`SetLastWriteTimeUtc`) e atributos (`SetAttributes`) dos arquivos copiados são preservados.
- Pastas temporárias e de download usam `Path.GetTempPath()` por padrão; o `TEMP` gravado no Registro pode ser customizado (`TempPath`, vazio = Registro `HKCU\SIC\CONEXAO\TEMP` se existir, senão padrão do Windows); nada é gravado no diretório corrente.

### Firebird

`FirebirdSql.Data.FirebirdClient` (10.3.4):

- Conexão: `localhost`, usuário `SYSDBA`, dialeto 3, charset UTF8, `Pooling=false`, `ServerType=Default`, banco por caminho absoluto. Depende do **serviço Firebird local** (não do executável `isql`/`gbak`).
- Execução de scripts: `FbScript` + `FbBatchExecution` (mesmo comportamento de `isql -i`, sem processo externo). O cancelamento fecha a conexão via `token.Register(() => connection.Close())`.
- Consulta de versão: `SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE` em `ARQSIST.FDB`, `CommandTimeout=0`.
- Restauração de `.FBK`: `FirebirdSql.Data.Services.FbRestore` com `FbRestore.ExecuteAsync(CancellationToken)` — anexada ao gerenciador de serviços (`service_mgr` em `localhost`, equivalente a `-se service_mgr`), com a mesma senha informada pelo operador. O destino é o `Database=` da connection string de serviço (`isc_spb_dbname`); a saída verbose do serviço é encaminhada para o log; as três restaurações rodam em paralelo (`Task.WhenAll`). Destino existente → `FbRestoreFlags.Replace` (equivalente a `-REP`); destino inexistente → `FbRestoreFlags.Create`. **Nenhum `gbak.exe`, `cmd` ou PowerShell é iniciado.**

### Registro do Windows

- Escopo: `HKCU\SIC\CONEXAO` apenas (`Registry.CurrentUser.CreateSubKey(..., writable: true)`).
- Modo simples: grava `BASE1`…`BASE<n>` (quantidade configurável em `BaseCount`, padrão 4, limite 99) e `EMPRESA` com o caminho completo da base + barra final; `SERVIDOR=LOCALHOST`; `TEMP=` pasta temporária configurada, ou `HKCU\SIC\CONEXAO\TEMP` se existir (quando o campo está vazio), senão `Path.GetTempPath()`.
- Modo multi-base (pastas `ARQ##` na raiz da SL **ou** opção **Sempre criar estrutura multi-base** ligada): grava `BASE<n>` (com `\` final) por pasta existente (exceto `ARQ00`), `EMPRESA` com a pasta `ARQ00`; `SERVIDOR` e `TEMP` como acima; **remove** as chaves `BASE1`–`BASE99` sem pasta correspondente. A etapa de "mover `.FDB` para a raiz" não é executada para que as pastas `ARQ##` sejam preservadas. No multi-base, os `*.fdb` são procurados na raiz e em subpastas (excluindo as pastas `ARQ##` (incluindo `ARQ00`/`ARQ01`)) e movidos para `ARQ00\` (EMP/OCOR) ou `ARQ01\` (ARQSIST); mais de uma candidata interrompe com aviso.
- Os valores anteriores **não** são lidos/salvos antes da gravação (sem backup do Registro).
- Erros de acesso são propagados como exceção e exibidos em diálogo próprio.

### Configuração e segredos

- JSON indentado em `%APPDATA%\TrazBase\settings.json`: pastas (`SolicitationRoot`, `BasesRoot`, `SupportRoot`), pasta temporária configurável (`TempPath`), preferência de estrutura (`ForceMultiBase`), quantidade de bases do Registro no modo simples (`BaseCount`), tela inicial (`DefaultScreen`), última novidade exibida (`LastChangelogVersion`), configuração própria da ação "Atualizar SIC" (`UpdateSicSourceRoot`, `UpdateSicDestinationRoot`, `UpdateSicSicVersion`, `SicRenameWithVersion`), comandos SQL inline (`ArqEmpCommands`, `ArqSistCommands`), preferências (`DarkMode`) e senha protegida (`ProtectedFirebirdPassword`). A base `.FDB` escolhida para testar comandos **não é persistida** (vale só para a sessão).
- Senha opcional ("Salvar senha"): protegida com `ProtectedData` (DPAPI, `DataProtectionScope.CurrentUser`), armazenada em Base64; nunca em texto puro; nunca registrada em log. Se `Unprotect` falhar, o valor é descartado e a opção volta a desmarcada.
- Migração: chaves antigas `ScriptArqEmp`/`ScriptArqSist` (arquivos `.sql` externos) são incorporadas aos comandos inline na primeira execução após a atualização.
- Comandos SQL são editados e salvos dentro do aplicativo; nenhum arquivo `.sql` externo é exigido.

### GitHub Releases (autoatualização)

`GET https://api.github.com/repos/BrunoPasseti21/Traz-base/releases/latest` → `tag_name` (prefixo `v` ignorado) maior que a versão do assembly → asset cujo nome termina em `Setup.exe` (URL precisa ser HTTPS) → download para `%TEMP%` → execução elevada com `/VERYSILENT ...`. O instalador (`TrazBase.iss`) bloqueia versões mais antigas que as já instaladas e permite reinstalar a mesma versão.

## Segurança e integridade operacional

- Destino resolvido e validado antes de qualquer escrita (`BaseOperationRules.ResolveLocalBaseDirectory`); saída de arquivos compactados validada (`ArchivePathSafety.GetSafeEntryPath`).
- Operações destrutivas (apagar/sobrescrever pasta existente, substituir `.FDB`) exigem escolha explícita: na transferência, três opções — **Cancelar**, **Criar backup** e **Substituir**; na restauração, um diálogo lista os `.FDB` que serão substituídos e só prossegue com confirmação. Sem confirmação, nada é removido.
- `ARQSIST.FDB` ausente nunca é "adivinhado" nem renomeado: o operador escolhe o arquivo.
- Movimentação de `.fdb` para a raiz falha explicitamente em caso de colisão de nomes.
- Senhas não aparecem em logs, diálogos ou Registro.
- A aplicação falha fechada: erros de conexão Firebird, Registro ou cópia interrompem o fluxo com mensagem; a etapa seguinte não é executada.
- Limitações conhecidas: sem log em arquivo; sem verificação de espaço em disco; valores anteriores do Registro não são preservados; senha mantida em memória durante a execução.

## Compilação e distribuição (resumo)

```powershell
dotnet build TrazBase.App/TrazBase.App.csproj -c Release
dotnet publish TrazBase.App/TrazBase.App.csproj -c Release -r win-x64 --self-contained true -o Instalador/Aplicativo
powershell -ExecutionPolicy Bypass -File Instalador/Compilar-Instalador.ps1   # gera Instalador/TrazBase-Setup.exe
```

O `.iss` publica em `Program Files\TrazBase` (padrão, exigindo admin/UAC), com ícone e atalho opcional na área de trabalho. Detalhes em [Desenvolvimento](DESENVOLVIMENTO.md).
