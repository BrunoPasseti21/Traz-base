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

| `ArqEmpCommands` | string | `""` | Comandos SQL executados em `ARQEMP.FDB` durante o fluxo de preparação/redefinição (e testáveis na tela ARQEMP). |
| `ArqSistCommands` | string | `""` | Idem para `ARQSIST.FDB`. |
| `CommandScriptsMigrated` | bool | `false` | `true` após a migração automática dos scripts legados para os comandos inline. |
| `DarkMode` | bool | `false` | Preferência de tema escuro (reaplicada a cada início). |
| `TempPath` | string | `""` | Pasta temporária usada no Registro (`TEMP`). Vazio → `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()` do Windows. |
| `UpdateSicSourceRoot` | string | `""` | Pasta base da origem dos executáveis SIC (ex.: `...\Sistemas\SIC`); a versão vem de `UpdateSicSicVersion`. |
| `UpdateSicDestinationRoot` | string | `""` | Pasta que recebe os executáveis SIC extraídos (ex.: `C:\projetos\SIC\sic\desenvolvimento`). |
| `UpdateSicSicVersion` | string | `""` | Versão do SIC usada como subpasta na origem (ex.: `s126`); vazio → `s126` no runtime. |
| `ForceMultiBase` | bool | `false` | Sempre criar a estrutura multi-base (`ARQ01`/`ARQ00`), mesmo em SLs sem pastas `ARQ##`. |
| `BaseCount` | number | `4` | Quantidade de chaves `BASE1`…`BASE<n>` gravadas no Registro no modo simples (quando "Sempre criar estrutura multi-base" não está marcada). Entre 1 e 99. |
| `ProtectedFirebirdPassword` | string? | omitida | Senha Firebird protegida por DPAPI (Base64), gravada somente quando "Salvar senha" está marcado. |

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
