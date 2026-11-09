# Testes automatizados

O projeto `TrazBase.Tests` (MSTest 4, `net10.0-windows`) cobre a **lógica pura** da aplicação. A UI (`MainForm`) e os efeitos externos (Registro, Firebird, sistema de arquivos de produção, GitHub) não são cobertos — ver *Escopo e limitações*.

## Como executar

Na raiz do repositório:

```powershell
dotnet test TrazBase.Tests/TrazBase.Tests.csproj
```

Saída esperada: `Aprovado! – Com falha: 0, Aprovado: 55, Ignorado: 0`.

Filtros úteis:

```powershell
# Uma classe
dotnet test TrazBase.Tests/TrazBase.Tests.csproj --filter "ClassName~BaseOperationRulesTests"

# Especificando o projeto e sem recompilar desnecessariamente
dotnet test TrazBase.Tests/TrazBase.Tests.csproj --no-restore

# No Visual Studio / VS Code: painel Test Explorer (ícone de frasco) lista e executa os testes
```

## Estrutura

- `TrazBase.Tests.csproj` referencia `TrazBase.App` (`ProjectReference`). O app expõe seus tipos `internal` à assembly de testes via `<InternalsVisibleTo Include="TrazBase.Tests" />` no `TrazBase.App.csproj`.
- `MSTestSettings.cs` ativa paralelismo no nível de método (`ExecutionScope.MethodLevel`) — cada teste é independente.
- Testes que precisam de diretórios reais usam pastas temporárias em `%TEMP%` com nome único (`TrazBaseTests-<guid>`), criadas no construtor de um `TempDirectory : IDisposable` e apagadas recursivamente no `Dispose`. Nada é escrito em pastas do usuário.
- O caminho do `settings.json` real (`%APPDATA%\TrazBase\`) é fixo no código, então os testes de `AppSettings` exercitam apenas a API de senha (em memória), nunca `Save()`/`Load()` — o arquivo real do usuário não é tocado.
- Os testes de senha usam DPAPI de verdade (`DataProtectionScope.CurrentUser`), portanto só passam em Windows com perfil de usuário.

## Cobertura por arquivo

### `BaseOperationRulesTests.cs` — 16 casos de teste (10 métodos, um deles parametrizado com 7 `DataRow`)

Testa `BaseOperationRules` (regras puras de operação):

| Teste | Verifica |
|---|---|
| `ResolveLocalBaseDirectory_CombinesRootAndSolicitationNumber` | Destino = `<BasesRoot>\<SL>` com caminhos normalizados (`Path.GetFullPath`). |
| `ResolveLocalBaseDirectory_AcceptsRootWithTrailingSeparator` | Raiz configurada com `\` final produz o mesmo destino. |
| `ResolveLocalBaseDirectory_RejectsEmptyBasesRoot` | `""` e `"   "` lançam `ArgumentException`. |
| `ResolveLocalBaseDirectory_RejectsInvalidSolicitationNumbers` | Casos parametrizados (`DataRow`): vazio, espaços, letras (`12a3`, `SLC123`), espaços internos, hífen, `../123` — todos lançam `ArgumentException`. |
| `ResolveLocalBaseDirectory_ResultStaysInsideBasesRoot` | Resultado sempre começa com a raiz normalizada + separador (defesa contra saída da pasta configurada). |
| `GetSupportFilesToCopy_ReturnsBothWhenDestinationIsMissingAndSourceHasBoth` | Com destino sem `ARQEMP.FDB`/`ARQOCOR.FDB` e apoio com ambos, retorna os 2 itens com origem/destino corretos. |
| `GetSupportFilesToCopy_SkipsFilesThatAlreadyExistInDestination` | Destino com `ARQEMP.FDB`: só `ARQOCOR.FDB` é listado. |
| `GetSupportFilesToCopy_ReturnsEmptyWhenEverythingExistsLocally` | Ambas as bases presentes no destino: lista vazia. |
| `GetSupportFilesToCopy_ThrowsWhenFileIsMissingInBothLocations` | `ARQOCOR.FDB` ausente nos dois lados: `FileNotFoundException`. |
| `GetDestinationAction_MapsDialogResults` | `Yes`→`CreateBackup`; `No`→`Replace`; `Cancel`, `OK`, `None`→`Cancel`. |

### `RestoreOperationRulesTests.cs` — 18 casos de teste (10 métodos, um deles parametrizado com 9 `DataRow`)

Testa `RestoreOperationRules` (regras puras da restauração — tela Restaurar base):

| Teste | Verifica |
|---|---|
| `MatchKnownDatabase_MatchesCaseInsensitively` | Casos parametrizados (`DataRow`): `ARQEMP.fbk`, `arqemp_2024.fbk`, `backup_ARQOCOR.FBK`, `ARQOCOR_19.fbk`, `arqsist_10.fbk`, `ARQSIST.fbk`, `ARQSIST_10.FBK` → tipo correspondente; `HISTORICO.fbk` e `""` → `null`. |
| `GetDestinationPath_UsesFolderOfFbkAndCanonicalName` | `arqemp_10.fbk` em `C:\bases\SL-123\BKP` → `C:\bases\SL-123\BKP\ARQEMP.FDB`. |
| `GetBackupDirectory_AppendsBkp` | `<base>` → `<base>\BKP`. |
| `ResolveBackupDirectory_ReturnsBkpWhenItExists` | Com `BKP\` existente na pasta temporária, retorna `<pasta>\BKP`. |
| `ResolveBackupDirectory_ReturnsSelectedFolderWhenBkpDoesNotExist` | Sem `BKP\`, retorna a própria pasta selecionada. |
| `BuildPlan_MapsKnownFilesAndLeavesUnknownsWithoutDestination` | Mistura de conhecidos/desconhecidos: conhecidos com destino calculado, desconhecido com `Database = null` e destino `""`; ordenação alfabética. |
| `BuildPlan_OrdersByFilePath` | Plano ordenado por caminho (case-insensitive). |
| `GetDuplicateDestinations_ReportsFilesMappedToTheSameTarget` | Dois `.FBK` que mapeiam para o mesmo `.FDB` → o destino duplicado é listado. |
| `GetDuplicateDestinations_ReportsNothingWhenAllTargetsAreDistinct` | Três bases distintas (`ARQEMP`, `ARQOCOR`, `ARQSIST`) → sem duplicados. |
| `GetDuplicateDestinations_DetectsDuplicatesAgainstManualChoices` | Destino escolhido manualmente pelo operador colidindo com outro mapeado → duplicata detectada. |

### `ArchivePathSafetyTests.cs` — 6 testes

Testa `ArchivePathSafety.GetSafeEntryPath` (proteção de entradas de compactados):

| Teste | Verifica |
|---|---|
| `GetSafeEntryPath_AcceptsSimpleRelativeEntry` | `base.fdb` resolve para `<destino>\base.fdb` normalizado. |
| `GetSafeEntryPath_PreservesSubdirectoryStructure` | `sub/pasta/arquivo.fdb` preserva a hierarquia. |
| `GetSafeEntryPath_NormalizesBackslashes` | `sub\pasta\arquivo.fdb` resolve igual à versão com `/`. |
| `GetSafeEntryPath_RejectsParentDirectoryTraversal` | `../evil.fdb`, `sub/../../evil.fdb`, `sub\..\..\evil.fdb` lançam `InvalidOperationException`. |
| `GetSafeEntryPath_RejectsAbsolutePaths` | `C:\Windows\evil.fdb`, `\\servidor\share\evil.fdb`, `\evil.fdb` lançam `InvalidOperationException`. |
| `GetSafeEntryPath_ResultStaysInsideDestinationRoot` | `a/b/c.txt` resulta em caminho dentro da raiz de destino. |

### `GitHubReleaseUpdaterTests.cs` — 9 testes

Testa `GitHubReleaseUpdater.ParseLatestRelease(json, currentVersion)` (decididor de "há atualização disponível?" a partir do JSON da API `releases/latest`). `currentVersion` fixo = `0.3.0`; os JSONs são montados por um helper `ReleaseJson(...)`:

| Teste | Verifica |
|---|---|
| `ParseLatestRelease_ReturnsUpdateWhenNewerVersionHasSetupAsset` | Caso feliz: tag `v0.4.0` + asset `TrazBase-Setup.exe` HTTPS → retorna `ReleaseUpdate` com versão `0.4.0` e URI HTTPS. |
| `ParseLatestRelease_AcceptsTagWithoutVPrefix` | Tag `0.4.0` (sem `v`) também é aceita. |
| `ParseLatestRelease_MatchesSetupExtensionCaseInsensitively` | `TrazBase-SETUP.EXE` é reconhecido. |
| `ParseLatestRelease_ReturnsNullWhenVersionIsNotNewer` | Tag igual (`v0.3.0`) ou anterior (`v0.2.9`) → `null`. |
| `ParseLatestRelease_ReturnsNullWhenTagIsMissingOrInvalid` | JSON sem `tag_name` ou com tag não-numérica (`versao-nova`) → `null`. |
| `ParseLatestRelease_ReturnsNullWhenThereIsNoAssetsArray` | JSON sem `assets` → `null`. |
| `ParseLatestRelease_ReturnsNullWhenNoAssetIsTheSetupInstaller` | Asset `TrazBase.exe` (não termina em `Setup.exe`) → `null`. |
| `ParseLatestRelease_RejectsNonHttpsAssetUrl` | Asset `TrazBase-Setup.exe` com URL `http://` → `null`. |
| `ParseLatestRelease_SkipsInvalidUrlsAndKeepsLooking` | URL malformada do primeiro asset é ignorada; o segundo asset válido é escolhido. |

### `AppSettingsTests.cs` — 6 testes

Testa a proteção DPAPI da senha Firebird em `AppSettings` (sem tocar em disco):

| Teste | Verifica |
|---|---|
| `GetFirebirdPassword_ReturnsNullWhenNoPasswordWasSaved` | Instância nova retorna `null` e não tem `ProtectedFirebirdPassword`. |
| `FirebirdPassword_RoundtripThroughDpapi` | `SetFirebirdPassword("s3nh@-secreta")` → valor protegido não nulo, diferente do texto puro e sem contê-lo; `GetFirebirdPassword()` devolve o original. |
| `FirebirdPassword_RoundtripsUnicodeCharacters` | Senha com acentos e emoji (`senha-çãõ-🔒`) sobrevive ao roundtrip. |
| `SetFirebirdPassword_ClearsPasswordWhenNullOrEmpty` | `null` e `""` apagam `ProtectedFirebirdPassword` e a leitura retorna `null`. |
| `GetFirebirdPassword_DiscardsValueWhenBase64IsInvalid` | Base64 inválido → retorna `null` **e** zera a propriedade (estado autocurável). |
| `GetFirebirdPassword_DiscardsValueWhenDpapiPayloadIsInvalid` | Base64 válido mas bytes não-DPAPI → idem. |

## Escopo e limitações

- **Coberto:** regras de destino/apoio/conflito, proteção de path traversal em compactados, reconhecimento/mapeamento de `.FBK` na restauração, decisão de atualização via GitHub Releases, proteção DPAPI da senha.
- **Não coberto:** `MainForm` (orquestração e UI: cópia, extração via `ZipArchive`/SharpCompress, Registro `HKCU\SIC\CONEXAO`, conexão SQL Firebird via `FbConnection`, execução do `FbRestore` em si, fluxo de atualização em si), `AppMessageBox` (diálogos) e `SvgIconLoader`. Testar esses exigiria extrair lógica para classes puras ou automação de UI — mesma ressalva registrada em [Riscos e decisões](RISCOS-E-DECISOES.md).
- Testes que dependem de DPAPI/perfil Windows não rodam em Linux/macOS nem em CI sem Windows.
- O paralelismo entre métodos (`MSTestSettings.cs`) é seguro porque nenhum teste escreve em local compartilhado (cada um usa `%TEMP%` único ou estado em memória).

## Convenções ao adicionar testes novos

1. Teste lógica pura; se a lógica estiver presa a `MainForm`, extraia-a primeiro para `internal static` (como `BaseOperationRules`) e mantenha `InternalsVisibleTo("TrazBase.Tests")`.
2. Nunca escreva em `%APPDATA%\TrazBase` ou no Registro real — use `%TEMP%` com nome único e `IDisposable` para limpeza.
3. Prefira `Assert.ThrowsExactly<T>` (comportamento exibido na suíte atual) a capturar exceções manualmente.
4. Evite depender de `CultureInfo`/fuso horário: strings esperadas devem ser independentes de cultura local.
5. Rode `dotnet test` antes de commitar; os analyzers MSTEST rodam junto com o build (são eles que sugerem `Assert.HasCount`/`Assert.IsEmpty`).
