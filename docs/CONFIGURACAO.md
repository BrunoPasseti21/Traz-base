# Configuração e dados locais

Onde o Traz Base guarda preferências, comandos SQL e credenciais.

## Local dos dados

Tudo fica no perfil do usuário atual:

```
%APPDATA%\TrazBase\settings.json
```

Ex.: `C:\Users\<usuário>\AppData\Roaming\TrazBase\settings.json`. O arquivo é JSON indentado, legível e editável manualmente (com o aplicativo fechado). Não há arquivo de log ainda; os logs ficam apenas na tela.

## Chaves do `settings.json`

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
| `TempPath` | string | `""` | Pasta temporária usada no Registro (`TEMP`). Vazio → `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()` do Windows. |
| `UpdateSicSourceRoot` / `UpdateSicDestinationRoot` / `UpdateSicSicVersion` | legado | `""` | Configuração legada da ação " (pasta-base, destino e versão). Usada apenas para migrar para `UpdateSicEntries` na primeira abertura após a atualização |
| `UpdateSicSingleDestinationEnabled` | bool | `false` | Se `true`, todos os executáveis vão para o mesmo destino (campo `UpdateSicSingleDestinationRoot`); a coluna `Destino` da tabela fica oculta |
| `UpdateSicSingleDestinationRoot` | string | `""` | Destino único aplicado a todos os executáveis quando `UpdateSicSingleDestinationEnabled` estiver marcado |
| `UpdateSicEntries` | lista de objetos | `[]` | Linhas da tabela "Executáveis a baixar" (tela **Atualização de executáveis**). Cada entrada tem `SourceRoot` (pasta-base), `Version` (subpasta, ex.: `146`), `FileName` (nome sem extensão, ex.: `SIC`), `DestinationRoot` (quando destino único não está marcado) e `RenameWithVersion` (bool, por linha) |
| `SicRenameWithVersion` | legado | `false` | Flag legada de renomeação; usada apenas para a migração |
| `RenameSlFolderToClientName` | bool | `false` | Renomeia a pasta da solicitação para **`<primeiro nome>_<SL>`**: consulta `SELECT FIRST 1 C.NOME, C.NFANTASIA FROM ARQCAD C WHERE C.TIPOC = 'N'` na **ARQSIST** e prioriza o NFANTASIA (se vazio/nulo, usa o NOME; sem nenhum dos dois, mantém a SL). Ex.: `JOAO_123456`. A verificação inicial de "pasta já existe" no fluxo Transferir considera `<SL>` exato ou qualquer pasta que termina em `_<SL>`. |
| `FolderShortcuts` | lista de objetos | `[]` | Atalhos de pasta exibidos na tela **Atalhos de pasta** (menu acima de Configuração). Cada atalho tem `Name`, `Path`, `ShowAllFiles`, `ShowExeFiles`, `ShowBatFiles`; filtros inclusivos. |
| `ForceMultiBase` | bool | `false` | Sempre criar a estrutura multi-base (`ARQ01`/`ARQ00`), mesmo em SLs sem pastas `ARQ##`. |
| `BaseCount` | number | `1` | Quantidade de chaves `BASE1`…`BASE<n>` gravadas no Registro no modo simples (pasta sem a estrutura `ARQ##`). Entre 1 e 99. |
| `DefaultScreen` | string | `"Transferir"` | Tela aberta ao iniciar a aplicação. Valores (em ordem alfabética): `AlterarBase`, `Atalhos`, `AtualizarSic`, `ConfigurarSic`, `RedefinirBase`, `RestaurarBase`, `Transferir`. (Valores antigos `Configuracao`, `ComandosArqEmp`, `ComandosArqSist` caem na Transferir.) |
| `LastChangelogVersion` | string? | `null` | Última versão exibida na tela "Novidades". `null` → mostrar na próxima abertura (atualização/versão nova). |
| `ProtectedFirebirdPassword` | string? | omitida | Senha Firebird protegida por DPAPI (Base64), gravada somente quando "Salvar senha" está marcado. |
| `ProtectedSicPassword` | string? | omitida | Senha padrão do SIC, com a mesma proteção DPAPI e a mesma caixa "Salvar senha" do campo Firebird. Habilitada sob demanda no menu da bandeja ("Copiar senha padrão SIC"). |
| `ProtectedSicDynamicPassword` | string? | omitida | **"Senha do sistema Empresa"**: senha usada pelo atalho global **Ctrl+Alt+X** — ao copiar, o app compõe a senha salva + (hora × dia × 13), usando a hora local (0–23) e o dia do mês (1–31), sem minutos nem segundos (ex.: dia 17, 16h → `SUPRA3536`). Mesma proteção DPAPI e a mesma caixa "Salvar senha". A chave mantém o nome técnico para compatibilidade com settings.json existentes. |

Campos nulos marcados com `[JsonIgnore(WhenWritingNull)]` (`ScriptArqEmp`, `ScriptArqSist`, `ProtectedFirebirdPassword`) desaparecem do arquivo quando vazios/limpos.

## Senha Firebird e DPAPI

- A senha é **sempre informada em tela** para conectar; o campo "Salvar senha" é opcional.
- Quando marcado, a senha é cifrada com `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)` (DPAPI do Windows, conta atual) e gravada em `ProtectedFirebirdPassword` como Base64.
- A decifragem só funciona na mesma conta Windows e na mesma máquina. Em outro perfil, `GetFirebirdPassword()` falha, descarta o valor e o aplicativo passa a exigir a senha novamente.
- A senha nunca aparece em logs, diálogos ou no Registro.
- O usuário Firebird é fixo como `SYSDBA`; dialeto 3; charset `UTF8`; sem pooling. O servidor é sempre `localhost` (serviço local).

## Registro do Windows gravado pela aplicação

Chave: `HKCU\SIC\CONEXAO` (somente usuário atual; não requer admin para `HKCU`).

| Valor | Conteúdo |
|---|---|
| `BASE1`, `BASE2`, `BASE3`, `BASE4` | Caminho completo da pasta da base, com `\` final |
| `EMPRESA` | Mesmo caminho da base |
| `SERVIDOR` | `LOCALHOST` |
| `TEMP` | Pasta temporária configurada; se vazia, o valor de `HKCU\SIC\CONEXAO\TEMP` quando existir; senão `Path.GetTempPath()` do usuário |

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
