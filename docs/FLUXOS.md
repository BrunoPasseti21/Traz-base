# Fluxos de operação

Este documento descreve os fluxos **como estão implementados** na versão 0.3.0.

## Estrutura da janela e navegação

Janela mínima 1020 × 760, inicial 1180 × 900, DPI `PerMonitorV2`. Layout em duas colunas: menu lateral (recolhido por padrão, 68 px; expandido com o botão sanduíche, 300 px) e área de conteúdo. Itens do menu:

1. **Transferir** (visão inicial) — trazer e preparar a base por número de SL.
2. **Alterar base** — apontar o Registro para uma base local já existente.
3. **Redefinir base** — validar/preparar uma base local existente para a SL.
4. **Restaurar base** — restaurar os `.FBK` da pasta selecionada (ou da subpasta `BKP`, se existir) para `.FDB` ao lado deles.
5. **Configuração** — pastas, senha Firebird e tema.
6. **Comandos ARQEMP** — editor SQL executável na base `ARQEMP.FDB`.
7. **Comandos ARQSIST** — editor SQL executável na base `ARQSIST.FDB`.

O rodapé do menu mostra a versão do aplicativo, o crédito "Criado por Bruno Passeti" e, quando há versão mais nova publicada no GitHub, um botão de atualização.

## Fluxo 1 — Transferir (tela inicial)

Pré-condições validadas ao clicar em **Trazer e preparar base** (ou Enter no campo SL):

1. SL informada e contendo somente dígitos.
2. Pastas local das bases (`BasesRoot`) e de apoio (`SupportRoot`) configuradas e existentes.
3. Pasta de origem das solicitações (`SolicitationRoot`) acessível.
4. Senha Firebird informada (não exige salvar).
5. Configuração persistida antes de começar; falha ao salvar aborta o fluxo.

Execução:

1. Origem = `SolicitationRoot\<SL>`; destino = `BasesRoot\<SL>` (resolvido por `BaseOperationRules.ResolveLocalBaseDirectory`, com proteção contra path traversal).
2. Se a origem não existe → aviso e fim.
3. **Conflito de destino**: se `BasesRoot\<SL>` já existe, diálogo próprio com três opções — **Cancelar**, **Criar backup** (renomeia para `<destino>.backup-yyyyMMdd-HHmmss`) ou **Substituir** (`Directory.Delete` recursivo). Sem escolha explícita, nada é apagado.
4. Cria a pasta de destino e copia recursivamente com barra de progresso por bytes (`Transferindo: N% | MB copiados / total | arquivos copiados / total`), preservando timestamps e atributos.
5. **Extração**: procura `*.rar` e `*.zip` na pasta destino (recursivo). Com mais de um arquivo, o operador seleciona quais extrair numa lista (padrão: todos marcados; **Pular extração** cancela o diálogo). Com um único arquivo, extrai direto. ZIP usa `ZipArchive`; RAR usa `SharpCompress` (`ReaderFactory`) com relatório de progresso por entrada. Entradas com caminhos absolutos ou `..` são rejeitadas (`ArchivePathSafety`).
6. **Bases na raiz**: todo `*.fdb` encontrado em subpastas é movido para a raiz do destino; se já existe um arquivo com o mesmo nome na raiz, o fluxo falha com mensagem explícita.
7. **Garantir bases obrigatórias**: `ARQSIST.FDB` ausente → abre `OpenFileDialog` para o operador escolher o arquivo (se cancelar, o fluxo é cancelado); o arquivo escolhido é **copiado** (não renomeado no lugar) para `ARQSIST.FDB`. `ARQEMP.FDB`/`ARQOCOR.FDB` ausentes são copiados da pasta de apoio; se também faltarem lá, o fluxo falha.
8. **Comandos SQL**: se o editor **Comandos ARQEMP** tiver conteúdo, executa em `ARQEMP.FDB`; idem **Comandos ARQSIST** em `ARQSIST.FDB`. Conexão `localhost`/`SYSDBA`, dialeto 3, charset UTF8. Se os editores estiverem vazios, a etapa é ignorada com registro no log.
9. **Registro**: atualiza `HKCU\SIC\CONEXAO` (ver abaixo).
10. **Versão**: consulta `SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE` em `ARQSIST.FDB` e exibe "Versão atual: …" ao lado do campo SL.
11. Mensagem de conclusão. O painel de progresso some; o log permanece.

Cancelamento (botão **Cancelar operação** ou fechamento da janela): define o `CancellationToken`; cópias/extrações/SQL abortam cooperativamente, o SQL fecha a conexão; a tela registra "Operação cancelada." O fechamento da janela durante a execução é bloqueado até a operação terminar.

## Fluxo 2 — Alterar base

1. Lista, na pasta `BasesRoot`, as subpastas existentes (ordenadas, sem navegação recursiva).
2. O operador seleciona uma pasta; o caminho completo aparece no campo inferior.
3. **Trocar base** (ou duplo clique) grava no Registro e confirma com mensagem; **Abrir pasta** abre o Explorer.
4. A mesma etapa de Registro do Fluxo 1 é reutilizada (`UpdateRegistryAsync`).

Valores gravados em `HKCU\SIC\CONEXAO` (em todas as trocas/preparações):

| Valor    | Conteúdo                                        |
|----------|-------------------------------------------------|
| `BASE1`…`BASE4` | caminho completo da pasta da base, com `\` final |
| `EMPRESA` | idem (mesmo caminho)                           |
| `SERVIDOR` | `LOCALHOST`                                   |
| `TEMP`   | `Path.GetTempPath()` do usuário                 |

Observações: os valores anteriores **não** são salvos antes da gravação (a proposta original previa isso; não foi implementado). O Registro é do usuário atual; não exige elevação para `HKCU`.

## Fluxo 3 — Redefinir base

Tela com instruções e campo de SL. Ao clicar em **Redefinir base**:

1. SL somente dígitos; `BasesRoot` e `SupportRoot` configuradas e existentes; senha Firebird informada.
2. Pasta alvo = `BasesRoot\<SL>` — precisa existir (a redefinição não copia nem substitui a pasta inteira).
3. `ARQSIST.FDB` precisa existir na pasta alvo; caso contrário, o fluxo é interrompido com aviso.
4. `ARQEMP.FDB`/`ARQOCOR.FDB` ausentes são copiados da pasta de apoio.
5. Comandos SQL dos editores ARQEMP/ARQSIST são executados (como no Fluxo 1, etapas 8).
6. Registro atualizado (idem Fluxo 2). Log próprio ("Andamento da redefinição") com carimbo de hora.
7. Não consulta a versão do banco.

Cancelamento disponível durante a execução; navegação desabilitada enquanto roda.

## Fluxo 4 — Restaurar base

Porta a opção 4 do `Traz-base-sl.bat` para uma tela própria. Interface sem processo externo: a restauração usa os serviços de gerenciamento do Firebird via provider ADO.NET (`FirebirdSql.Data.Services.FbRestore`), com `ExecuteAsync`, portanto nada é invocado por `cmd`/`PowerShell`/`gbak.exe`, e as bases são restauradas **em paralelo**.

Ação **Restaurar bases** (requer a pasta selecionada e a senha Firebird):

1. Resolve a pasta de backups: a subpasta `BKP` da pasta selecionada quando existe; caso contrário, a própria pasta selecionada (`RestoreOperationRules.ResolveBackupDirectory`). Se não houver `*.fbk` nela, avisa e interrompe.
2. Monta o plano a partir dos nomes dos arquivos (`RestoreOperationRules`): contém `ARQEMP`/`ARQOCOR`/`ARQSIST` → restaurado como `ARQEMP.FDB`/`ARQOCOR.FDB`/`ARQSIST.FDB` **na mesma pasta do .FBK** (em `BKP\` ou na pasta selecionada); correspondência sem distinção de maiúsculas/minúsculas.
3. `.FBK` com nome não reconhecido: o operador escolhe o destino em um diálogo (**ARQEMP** / **ARQOCOR** / **ARQSIST** / **Pular** — pular ignora o arquivo).
4. Se dois arquivos forem parar no mesmo destino (nome reconhecido duplicado ou escolhas manuais repetidas), o fluxo lista os conflitos e **não inicia nada**.
5. Se algum `.FDB` de destino já existir, um único diálogo lista os arquivos e pede confirmação (padrão **Não**); sem confirmação, nada é sobrescrito.
6. Restaura as bases em paralelo, uma tarefa por `.FBK` (`Task.WhenAll`). Cada tarefa registra no log a origem, o destino e a saída do serviço (modo verbose); uma falha em uma base não impede as demais.
7. Destino existente → `FbRestoreFlags.Replace` (como o `-REP` do BAT); destino inexistente → `FbRestoreFlags.Create`.
8. Ao final, se ao menos uma base foi restaurada, o operador escolhe (**Sim/Não**) se deseja atualizar `HKCU\SIC\CONEXAO` para apontar para a pasta onde os `.FDB` foram restaurados (mesmo caminho do passo 2). Sem restauração bem-sucedida, o Registro não é tocado e nenhum diálogo é exibido.
9. Log próprio ("Andamento da restauração") com carimbo de hora; **Cancelar** encerra a espera — o cancelamento fecha as conexões de serviço e a etapa é abortada (as bases podem ficar em estado intermediário, como no `gbak`). Navegação desabilitada enquanto roda.

## Fluxo 5 — Comandos ARQEMP / Comandos ARQSIST

Duas telas idênticas para os dois bancos. Cada uma tem:

- Campo "Base Firebird (.FDB)" com botão **Procurar** (filtro `*.fdb;*.gdb`).
- Editor SQL com realce de sintaxe (comentários, strings, palavras-chave, números) em paleta escura fixa.
- **Testar comando**: exige base existente, SQL não vazio e senha; pede confirmação explícita (avisa que comandos de alteração modificam dados); executa via `FbScript`/`FbBatchExecution` e mostra o resultado em painel de saída ("Resultado do teste").
- Comandos e caminho da base são salvos automaticamente ~700 ms após a última digitação (`_commandSaveTimer`) e na saída do aplicativo.
- Migração automática: configurações antigas que apontavam para arquivos `.sql` externos (`ScriptArqEmp`/`ScriptArqSist`) são incorporadas aos comandos inline na primeira execução após a atualização (`CommandScriptsMigrated`).

## Fluxo 6 — Configuração

Campos persistidos em `%APPDATA%\TrazBase\settings.json`:

- **Origem das solicitações** (`SolicitationRoot`)
- **Pasta local das bases** (`BasesRoot`)
- **Pasta de bases de apoio** (`SupportRoot`)
- **Senha Firebird** (campo protegido; caixa **Salvar senha** grava protegida por DPAPI)
- **Usar modo escuro** (persiste `DarkMode` e reaplica o tema na hora)

Alterações de tema e senha são salvas imediatamente; os demais campos são salvos ao iniciar operações, ao fechar o app e na saída.

## Fluxo 7 — Atualização do aplicativo

Ao abrir a janela, em segundo plano:

1. Consulta `https://api.github.com/repos/BrunoPasseti21/Traz-base/releases/latest` (timeout 15 s).
2. Se `tag_name` (sem o `v` opcional) for maior que a versão do assembly e existir asset cujo nome termina em `Setup.exe` com URL `https`, mostra o botão "Atualização disponível · vX.Y.Z" no rodapé do menu (ou uma animação/barra de progresso durante o download).
3. Ao clicar: confirmação → download para `%TEMP%\TrazBase-Setup-<guid>.exe` com progresso → execução com `Verb=runas` e `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CLOSEAPPLICATIONS` → o app fecha.
4. Falha na verificação/download é silenciosa (o uso normal não é afetado); o usuário pode tentar de novo.
5. A atualização em `Program Files` exige UAC; versões antigas são bloqueadas pelo instalador.

## Estados de uma operação

`Aguardando entrada → Validando → Confirmando alterações → Executando → Concluída | Falhou | Cancelada`

Cada etapa produz mensagem no log/status. Ao falhar, o fluxo não avança automaticamente e a exceção é exibida com a etapa afetada.
