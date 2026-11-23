# Configuração e dados locais

Onde o Traz Base guarda preferências, comandos SQL e credenciais.

## Local dos dados

Tudo fica no perfil do usuário atual:

```
%APPDATA%\TrazBase\settings.json
```

## Logs de erro

Erros de operação (e falhas inesperadas da aplicação) são gravados no arquivo:

```
%APPDATA%\TrazBase\logs\TrazBase.log
```

Cada linha tem data/hora, contexto, mensagem e o stack trace da exceção. O arquivo cresce até **2 MB** e, ao atingir esse limite, é renomeado para `TrazBase-yyyyMMdd-HHmmss.log` e um novo é iniciado. A caixa de erro exibida ao operador também indica o caminho do arquivo para facilitar o chamado para o suporte.

Ex.: `C:\Users\<usuário>\AppData\Roaming\TrazBase\settings.json`. O arquivo é JSON indentado, legível e editável manualmente (com o aplicativo fechado). Não há arquivo de log ainda; os logs ficam apenas na tela.

## Chaves do `settings.json`

A tela **Configuração** é dividida em três abas: **Geral** (pastas e comportamento), **Senhas** (Firebird, SIC e Empresa) e **VPN** (conexão antes da transferência).

| Chave | Tipo | Padrão | Descrição |
|---|---|---|---|
| `SolicitationRoot` | string | `""` | Pasta de origem das solicitações na rede (ex.: `\\servidor\solicitacoes`). Campo "Origem das solicitações". |
| `BasesRoot` | string | `""` | Pasta local onde as bases da SL são criadas/gerenciadas (`BasesRoot\<SL>`). |
| `SupportRoot` | string | `""` | Pasta de bases de apoio de onde `ARQEMP.FDB` e `ARQOCOR.FDB` são copiados quando ausentes. |
| `ScriptArqEmp` | string? | omitida | **Legado.** Caminho de um `.sql` para ARQEMP; migrado para `ArqEmpCommands` na primeira execução. |
| `ScriptArqSist` | string? | omitida | **Legado.** Caminho de um `.sql` para ARQSIST; migrado para `ArqSistCommands` na primeira execução. |

| `UseSupportRootAsCompany` | bool | `false` | Checkbox **"Apontar a EMPRESA do Regedit para a pasta de bases de apoio"**, abaixo do campo de apoio na Configuração. Ligado: o valor `EMPRESA` gravado em `HKCU\SIC\CONEXAO` aponta para a pasta de apoio (com `\` final), no lugar do caminho padrão (a pasta da base, no modo simples, ou `ARQ00`, no multi-base). Se a pasta de apoio estiver vazia/inacessível, vale o caminho padrão (com aviso no log da operação). |
| `ArqEmpCommands` | string | `""` | Comandos SQL executados em `ARQEMP.FDB` durante o fluxo de preparação/redefinição (e testáveis na tela ARQEMP). |
| `ArqSistCommands` | string | `""` | Idem para `ARQSIST.FDB`. |
| `CommandScriptsMigrated` | bool | `false` | `true` após a migração automática dos scripts legados para os comandos inline. |
| `DarkMode` | bool | `false` | Preferência de tema escuro (reaplicada a cada início). |
| `MinimizeToTrayOnClose` | bool | `false` | Fechar a janela minimiza para a bandeja do Windows (segundo plano) em vez de encerrar o aplicativo. Sem essa opção, × / Alt+F4 perguntam "Deseja realmente fechar a aplicação?" antes de encerrar (botão padrão Não). "Fechar" no menu da bandeja encerra de verdade, sem perguntar. |
| `StartInBackground` | bool | `false` | **Iniciar o TrazBase em segundo plano (na bandeja)** — ao abrir o aplicativo, a janela não é exibida e o TrazBase inicia escondido na bandeja do Windows. Os atalhos **Ctrl+Alt+C** e **Ctrl+Alt+X**, o menu da bandeja e a verificação silenciosa de atualização continuam funcionando. A janela pode ser restaurada com o duplo clique no ícone da bandeja ou pelo menu (Transferir, etc.). Padrão: desmarcada. |
| `StartWithWindows` | bool | `false` | **Iniciar o TrazBase com o Windows** — o aplicativo é aberto automaticamente no login do Windows (chave `TrazBase` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, com o caminho do executável entre aspas). No arranque, a chave é reparada com o caminho atual caso o executável tenha mudado de pasta. Pode ser combinada com `StartInBackground` para iniciar já na bandeja. Padrão: desmarcada. |
| `TempPath` | string | `""` | Pasta temporária usada no Registro (`TEMP`) — sempre gravada com barra final. Vazio → `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()` do Windows. |
| `UpdateSicSourceRoot` / `UpdateSicDestinationRoot` / `UpdateSicSicVersion` | legado | `""` | Configuração legada da ação " (pasta-base, destino e versão). Usada apenas para migrar para `UpdateSicEntries` na primeira abertura após a atualização |
| `UpdateSicSingleDestinationEnabled` | bool | `false` | Se `true`, todos os executáveis vão para o mesmo destino (campo `UpdateSicSingleDestinationRoot`); a coluna `Destino` da tabela fica oculta |
| `UpdateSicSingleDestinationRoot` | string | `""` | Destino único aplicado a todos os executáveis quando `UpdateSicSingleDestinationEnabled` estiver marcado |
| `UpdateSicEntries` | lista de objetos | `[]` | Linhas da tabela "Executáveis a baixar" (tela **Atualização de executáveis**). Cada entrada tem `SourceRoot` (pasta-base), `Version` (subpasta, ex.: `146`), `FileName` (nome sem extensão, ex.: `SIC`), `DestinationRoot` (quando destino único não está marcado) e `RenameWithVersion` (bool, por linha) |
| `SicRenameWithVersion` | legado | `false` | Flag legada de renomeação; usada apenas para a migração |
| `RenameSlFolderToClientName` | bool | `false` | Renomeia a pasta da solicitação para **`<primeiro nome>_<SL>`**: consulta `SELECT FIRST 1 C.NOME, C.NFANTASIA FROM ARQCAD C WHERE C.TIPOC = 'N'` na **ARQSIST** e prioriza o NFANTASIA (se vazio/nulo, usa o NOME; sem nenhum dos dois, mantém a SL). Ex.: `Teste_123456`. A verificação inicial de "pasta já existe" no fluxo Transferir considera `<SL>` exato ou qualquer pasta que termina em `_<SL>`. |
| `FolderShortcuts` | lista de objetos | `[]` | Atalhos de pasta exibidos na tela **Atalhos de pasta** (menu acima de Configuração). Cada atalho tem `Name`, `Path`, `ShowAllFiles`, `ShowExeFiles`, `ShowBatFiles`; filtros inclusivos. |
| `SqlShortcuts` | lista de objetos | `[]` | Comandos salvos na tela **Copiar SQL** (aba abaixo de Atalhos de pasta) e listados no submenu da bandeja. Cada entrada tem `Id`, `Title` (nome) e `Content` (SQL montado no editor). O comando é copiado para a área de transferência; a edição usa o mesmo editor SQL com realce das telas ARQEMP/ARQSIST. |
| `ForceMultiBase` | bool | `false` | Sempre criar a estrutura multi-base (`ARQ01`/`ARQ00`), mesmo em SLs sem pastas `ARQ##`. |
| `BaseCount` | number | `1` | Quantidade de chaves `BASE1`…`BASE<n>` gravadas no Registro no modo simples (pasta sem a estrutura `ARQ##`). Entre 1 e 99. |
| `DefaultScreen` | string | `"Inicio"` | Tela aberta ao iniciar a aplicação. Valores (em ordem alfabética): `AlterarBase`, `Atalhos`, `AtualizarSic`, `ConfigurarSic`, `CopiarSql`, `Inicio`, `RedefinirBase`, `RestaurarBase`, `Transferir`. (Valores antigos `Configuracao`, `ComandosArqEmp`, `ComandosArqSist` caem na Transferir.) Instalações existentes com `Transferir` são migradas uma vez para `Inicio`; a escolha posterior do usuário é preservada. |
| `LastChangelogVersion` | string? | `null` | Última versão exibida na tela "Novidades". `null` → mostrar na próxima abertura (atualização/versão nova). |
| `ProtectedVpnPassword` | string? | omitida | Senha da VPN, protegida por DPAPI, gravada somente quando "Informar usuário e senha manualmente" e "Salvar senha" estão marcados. |
| `VpnConnectScriptEnabled` | bool | `false` | **"Executar script ao conectar a VPN"** (aba VPN) — liga a execução do script abaixo assim que a VPN for conectada pelo aplicativo. Não executa se a VPN já estava conectada antes. |
| `VpnConnectScript` | string | `""` | Script (conteúdo de um .bat/.cmd) executado após a conexão da VPN (somente quando o aplicativo a conectou). Saída registrada no log; se falhar (exit code ≠ 0, timeout ou exceção), a transferência é abortada e a VPN é desconectada de volta. |
| `ProtectedFirebirdPassword` | string? | omitida | Senha Firebird protegida por DPAPI (Base64), gravada somente quando "Salvar senha" está marcado. |
| `ProtectedSicPassword` | string? | omitida | Senha padrão do SIC, com a mesma proteção DPAPI e a mesma caixa "Salvar senha" do campo Firebird. Habilitada sob demanda no menu da bandeja ("Copiar senha padrão SIC"). |
| `ProtectedSicDynamicPassword` | string? | omitida | **"Senha do sistema Empresa"**: senha usada pelo atalho global **Ctrl+Alt+X** — ao copiar, o app compõe a senha salva + (hora × dia × 13), usando a hora local (0–23) e o dia do mês (1–31), sem minutos nem segundos (ex.: dia 17, 16h → `SUPRA3536`). Mesma proteção DPAPI e a mesma caixa "Salvar senha". A chave mantém o nome técnico para compatibilidade com settings.json existentes. |
| `UseVpn` | bool | `false` | **"Usar VPN para transferir bases"** (aba VPN) — com a opção ligada, a transferência conecta a VPN informada antes de acessar a pasta de origem e desconecta ao final somente se ela não estava conectada antes. |
| `VpnConnectionName` | string | `""` | Nome da conexão VPN configurada no Windows (aba VPN). Preenchido pelo botão "Atualizar lista" (enumera as conexões com `rasapi32.DLL`) ou digitado livremente. |
| `VpnUseManualCredentials` | bool | `false` | **"Informar usuário e senha manualmente"** (aba VPN) — ligado: o app passa o usuário/senha informados ao `rasdial`; desmarcado: usa as credenciais salvas no perfil do Windows. |
| `VpnUsername` | string | `""` | Usuário da VPN (aba VPN), usado somente quando `VpnUseManualCredentials` está ligado. |
| `VpnReconnectAttempts` | number | `3` | **"Máx. tentativas de reconexão"** (aba VPN, 1–10) — quantas vezes a transferência reconecta à VPN e refaz a cópia quando a conexão cai durante a cópia dos arquivos. |

Campos nulos marcados com `[JsonIgnore(WhenWritingNull)]` (`ScriptArqEmp`, `ScriptArqSist`, `ProtectedFirebirdPassword`) desaparecem do arquivo quando vazios/limpos.

## Senha Firebird e DPAPI

- A senha é **sempre informada em tela** para conectar; o campo "Salvar senha" é opcional.
- Quando marcado, a senha é cifrada com `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)` (DPAPI do Windows, conta atual) e gravada em `ProtectedFirebirdPassword` como Base64.
- A decifragem só funciona na mesma conta Windows e na mesma máquina. Em outro perfil, `GetFirebirdPassword()` falha, descarta o valor e o aplicativo passa a exigir a senha novamente.
- A senha nunca aparece em logs, diálogos ou no Registro.
- O usuário Firebird é fixo como `SYSDBA`; dialeto 3; charset `UTF8`; sem pooling. O servidor é sempre `localhost` (serviço local).

## VPN para transferência

A aba **VPN** da Configuração controla a conexão usada pelo fluxo **Transferir** quando a pasta de origem das solicitações só está acessível através de uma VPN:

- Com **"Usar VPN para transferir bases"** marcado, o aplicativo conecta a conexão escolhida (`rasdial`) antes de acessar a origem e **desconecta ao final somente se ela não estava conectada antes** — se já estava conectada, a conexão é mantida.
- Se a conexão cair durante a cópia, a transferência reconecta e **refaz a cópia substituindo o que já tinha sido baixado** (conforme `VpnReconnectAttempts`, padrão 3). As verificações iniciais (origem e pasta da SL) também reconectam automaticamente.
- Por padrão as credenciais vêm do **perfil do Windows** (as salvas para a conexão); marcando **"Informar usuário e senha manualmente"**, o app passa o usuário/senha informados (a senha pode ser salva protegida por DPAPI).
- A conexão VPN é desfeita apenas se o aplicativo a conectou. Se o operador conectou manualmente antes, a conexão permanece ao final.
- A conexão usa `rasdial.exe` (parte do Windows) — nenhuma conexão nova é criada; ela precisa existir no Windows em **Configurações → Rede e Internet → VPN**.
- O botão **Atualizar lista** preenche o combo com as conexões existentes (via `rasapi32`); o campo também aceita digitação livre.

## Registro do Windows gravado pela aplicação

Chave: `HKCU\SIC\CONEXAO` (somente usuário atual; não requer admin para `HKCU`).

| Valor | Conteúdo |
|---|---|
| `BASE1`, `BASE2`, `BASE3`, `BASE4` | Caminho completo da pasta da base, com `\` final |
| `EMPRESA` | Mesmo caminho da base |
| `SERVIDOR` | `LOCALHOST` |
| `TEMP` | Pasta temporária configurada (com `\` final); se vazia, o valor de `HKCU\SIC\CONEXAO\TEMP` quando existir; senão `Path.GetTempPath()` do usuário |

Os valores anteriores não são preservados; a aplicação confirma a ação e informa os caminhos envolvidos, mas não faz backup do Registro.

## Migração de scripts legados

Versões anteriores permitiam apontar para arquivos `.sql` externos (`ScriptArqEmp`/`ScriptArqSist`). Na primeira abertura após a atualização:

1. Se o comando inline estiver vazio e a chave legada apontar para um arquivo existente, o conteúdo do arquivo é copiado para `ArqEmpCommands`/`ArqSistCommands`.
2. As chaves legadas são removidas e `CommandScriptsMigrated=true` é salvo.
3. Em caso de erro, a migração é refeita na próxima abertura.

## Restaurar a configuração padrão

1. Feche o Traz Base.
2. Apague `%APPDATA%\TrazBase\settings.json` (ou apenas as chaves desejadas).
3. Abra novamente: o aplicativo recria o arquivo com os padrões (tudo vazio; tema claro).
4. O Registro gravado anteriormente (`HKCU\SIC\CONEXAO`) não é restaurado — ajuste-o pela própria aplicação (altere para qualquer base) ou manualmente.

## Backup e portabilidade

- Para levar a configuração a outra máquina: copie `settings.json` e marque novamente "Salvar senha" no destino — a senha protegida por DPAPI **não** é portátil entre máquinas/contas (grave-a novamente).
- `settings.json` pode conter comandos SQL de negócio: trate-o como dado interno.
- Nunca commite `settings.json` (o `.gitignore` não o cobre porque ele fica fora do repositório, em `%APPDATA%`).
