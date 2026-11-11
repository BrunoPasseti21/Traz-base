# Fluxos de operação

Este documento descreve os fluxos **como estão implementados** na versão 0.3.0.

## Estrutura da janela e navegação

Janela mínima 1020 × 760, inicial 1180 × 900, DPI `PerMonitorV2`. Layout em duas colunas: menu lateral (recolhido por padrão, 68 px; expandido com o botão sanduíche, 300 px) e área de conteúdo. Ao alternar para uma tela (e ao abrir o sistema), o foco vai automaticamente para o primeiro campo editável dela: **Transferir** → campo SL; **Alterar base** → lista de bases, com a base indicada pelo Registro (`HKCU\SIC\CONEXAO\BASE1`) já pré-selecionada quando encontrada; **Redefinir base** → campo SL da redefinição; **Restaurar base** → campo da pasta selecionada; **Atualizar SIC** → campo Origem dos executáveis; **Comandos ARQEMP/ARQSIST** → editor de comandos SQL; **Configuração** → campo "Origem das solicitações". Em todas as telas, a tecla **Enter** move o foco para o próximo campo editável, terminando no botão/controle principal da tela. Itens do menu:

1. **Transferir** (visão inicial) — trazer e preparar a base por número de SL.
2. **Alterar base** — apontar o Registro para uma base local já existente.
3. **Redefinir base** — validar/preparar uma base local existente para a SL.
4. **Restaurar base** — restaurar os `.FBK` da pasta selecionada (ou da subpasta `BKP`, se existir) para `.FDB` ao lado deles.
5. **Atualizar SIC** — copiar e extrair os executáveis do SIC da versão configurada (ação equivalente à do `Atualiza_Tudo.cmd`).
6. **Configuração** — pastas, senha Firebird e tema.
7. **Comandos ARQEMP** — editor SQL executável na base `ARQEMP.FDB`.
8. **Comandos ARQSIST** — editor SQL executável na base `ARQSIST.FDB`.

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
5. **Extração**: procura `*.rar` e `*.zip` na pasta destino (recursivo). Com mais de um arquivo, o operador seleciona quais extrair numa lista (padrão: todos marcados; **Pular extração** cancela o diálogo). Com um único arquivo, extrai direto. A estrutura de pastas recebidas (ex.: `ARQ01`, `ARQ05`) é mantida. ZIP usa `ZipArchive`; RAR usa `SharpCompress` (`ReaderFactory`) com relatório de progresso por entrada. Entradas com caminhos absolutos ou `..` são rejeitadas (`ArchivePathSafety`).
6. **Escolha do caminho**: após a extração, o fluxo detecta pastas `ARQ##` (dois dígitos, na raiz da SL). Se existirem, aplica o **tratamento multi-base** (ver abaixo); caso contrário, todo `*.fdb` encontrado em subpastas é movido para a raiz do destino (se já existir um arquivo com o mesmo nome na raiz, o fluxo falha com mensagem explícita).
6a. **Garantir bases obrigatórias (modo simples)**: `ARQSIST.FDB` ausente → abre `OpenFileDialog` para o operador escolher o arquivo (se cancelar, o fluxo é cancelado); o arquivo escolhido é **copiado** para a raiz. `ARQEMP.FDB`/`ARQOCOR.FDB` ausentes são copiados da pasta de apoio para a raiz; se também faltarem lá, o fluxo falha.
6b. **Garantir bases obrigatórias (multi-base)**: `ARQEMP.FDB`/`ARQOCOR.FDB` vindos na raiz são **movidos** para a pasta `ARQ00\` (criada se necessário); se faltarem, são copiados da pasta de apoio **para `ARQ00\`**. A `ARQSIST` usada é sempre a de `ARQ01\ARQSIST.FDB` (criada via `OpenFileDialog` se não existir). Conflito entre raiz e `ARQ00` interrompe a preparação.
7. **Comandos SQL**: se o editor **Comandos ARQEMP** tiver conteúdo, executa em `ARQEMP.FDB` (raiz no modo simples, `ARQ00\ARQEMP.FDB` no multi-base). Conexão `localhost`/`SYSDBA`, dialeto 3, charset UTF8. Se os editores estiverem vazios, a etapa é ignorada com registro no log.
7a. **Comandos ARQSIST**: com conteúdo, no modo multi-base e havendo **mais de uma** base ARQSIST entre as pastas `ARQ##` que possuem `ARQSIST.FDB`, o fluxo **pergunta** se o operador deseja executar os comandos em todas elas (Sim = executa em todas, sequencialmente; Não = somente na `ARQ01`). Pastas `ARQ##` sem `ARQSIST.FDB` são ignoradas e aparecem no log; a pergunta só é feita quando há mais de uma ARQSIST e o editor tem conteúdo. Fora do multi-base (ou sem conteúdo), executa somente na ARQSIST do modo ativo.
8. **Registro**: modo simples → atualiza `HKCU\SIC\CONEXAO` (ver abaixo). Multi-base → ver "Registro multi-base".
9. **Versão**: consulta `SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE` na ARQSIST do modo ativo e exibe "Versão atual: …" ao lado do campo SL.
10. Mensagem de conclusão. O painel de progresso some; o log permanece.

Cancelamento (botão **Cancelar operação** ou fechamento da janela): define o `CancellationToken`; cópias/extrações/SQL abortam cooperativamente, o SQL fecha a conexão; a tela registra "Operação cancelada." O fechamento da janela durante a execução é bloqueado até a operação terminar.

## Fluxo 2 — Alterar base

1. Lista, na pasta `BasesRoot`, as subpastas existentes (ordenadas, sem navegação recursiva).
2. O operador seleciona uma pasta; o caminho completo aparece no campo inferior.
3. **Trocar base** (ou duplo clique) grava no Registro e confirma com mensagem; **Abrir pasta** abre o Explorer.
4. A mesma etapa de Registro do Fluxo 1 é reutilizada (`UpdateRegistryAsync`).

Valores gravados em `HKCU\SIC\CONEXAO` (modo simples, pelas trocas/preparações):

| Valor    | Conteúdo                                        |
|----------|-------------------------------------------------|
| `BASE1`…`BASE<n>` | caminho completo da pasta da base, com `\` final — quantidade definida em **Quantidade de bases** nas Configurações (padrão 4) |
| `EMPRESA` | idem (mesmo caminho)                           |
| `SERVIDOR` | `LOCALHOST`                                   |
| `TEMP`   | `TempPath` configurada, ou `HKCU\SIC\CONEXAO\TEMP` se existir; senão `Path.GetTempPath()` |

## Registro — modo multi-base

Quando a SL traz pastas `ARQ##`:

| Valor | Conteúdo |
|---|---|
| `BASE<n>` | caminho completo da pasta `ARQ##`, com `\` final — uma chave por pasta (ex.: `ARQ01`→`BASE1`, `ARQ05`→`BASE5`) |
| `EMPRESA` | caminho completo da pasta `ARQ00` (onde ficam `ARQEMP.FDB` e `ARQOCOR.FDB`), com `\` final |
| `SERVIDOR` | `LOCALHOST` |
| `TEMP` | `TempPath` configurada, ou `HKCU\SIC\CONEXAO\TEMP` se existir; senão `Path.GetTempPath()` |

Além de gravar, as chaves `BASE1`…`BASE99` **sem pasta correspondente são removidas** (ex.: só `ARQ01` e `ARQ05` → `BASE2`, `BASE3`, `BASE4`, `BASE6`… são apagadas). `ARQ00` não gera chave `BASE` — alimenta apenas `EMPRESA`. SLs sem pastas `ARQ##` mantêm o comportamento anterior (`BASE1`…`BASE4` e `EMPRESA` = pasta da SL).

Observações: os valores anteriores **não** são salvos antes da gravação (a proposta original previa isso; não foi implementado). O Registro é do usuário atual; não exige elevação para `HKCU`. As exclusões no modo multi-base só apagam chaves `BASE1`…`BASE99` inexistentes na SL.

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

## Fluxo 5 — Atualizar SIC

Porta a ideia do script `Atualiza_Tudo.cmd` (já no repositório é chamado a partir do app, não do `.cmd`) para uma tela própria. Ação **Atualizar executáveis** (requer origem e destino configurados), equivalente ao `.cmd`:

1. Origem: a pasta **base** (ex.: `...\Sistemas\SIC`) e a **versão** (ex.: `s126`) → pasta `base\<versao>`. A versão vazia usa `s126` como fallback. Se a pasta não existir, avisa e interrompe.
2. Destino: pasta que recebe os arquivos extraídos. Se não existir, é criada.
3. Pede confirmação explícita (**Sim/Não**) mostrando a origem, o destino e que o destino será sobrescrito.
4. Para cada um dos quatro executáveis — `OSAtualizaSIC.exe.zip`, `SIC.exe.zip`, `Prj_Obra.exe.zip`, `Prj_PCP_FA.exe.zip` (`UpdateSicOperationRules.Executables`):
   - se o `.zip` não existir na origem, registra o aviso no log e ignora (os demais seguem e são reportados no final);
   - copia o `.zip` para o destino (sobrescreve, como o `Copy /y` do `.cmd`);
   - extrai via `ZipArchive` nativo com validação de caminho (nunca grava fora do destino);
   - apaga o `.zip` no destino (como o `-df` do `.cmd`);
   - registra tamanho, destino e resultado de cada arquivo no log ("Andamento da atualização").
5. Ao final: mensagem de sucesso ou de atualização incompleta (lista dos arquivos não encontrados na origem). **Cancelar** interrompe o loop entre blocos; navegação desabilitada enquanto roda.
6. As configurações da ação são **próprias e separadas**: `UpdateSicSourceRoot`, `UpdateSicDestinationRoot`, `UpdateSicSicVersion` — nada a ver com as configurações das bases (SL); salvas como as demais (debounce + ao fechar/iniciar operações). A parte EDE do `.cmd` está comentada e não foi portada.

## Fluxo 6 — Comandos ARQEMP / Comandos ARQSIST

Duas telas idênticas para os dois bancos. Cada uma tem:

- Campo "Base Firebird (.FDB)" com botão **Procurar** (filtro `*.fdb;*.gdb`).
- Editor SQL com realce de sintaxe (comentários, strings, palavras-chave, números) em paleta escura fixa.
- **Testar comando**: exige base existente, SQL não vazio e senha; pede confirmação explícita (avisa que comandos de alteração modificam dados); executa via `FbScript`/`FbBatchExecution` e mostra o resultado em painel de saída ("Resultado do teste").
- Comandos são salvos automaticamente ~700 ms após a última digitação (`_commandSaveTimer`) e na saída do aplicativo. A base Firebird (.FDB) escolhida para testar **não é persistida** — vale apenas para a sessão atual.
- Migração automática: configurações antigas que apontavam para arquivos `.sql` externos (`ScriptArqEmp`/`ScriptArqSist`) são incorporadas aos comandos inline na primeira execução após a atualização (`CommandScriptsMigrated`).

## Fluxo 7 — Configuração

Campos persistidos em `%APPDATA%\TrazBase\settings.json`:

- **Origem das solicitações** (`SolicitationRoot`)
- **Pasta local das bases** (`BasesRoot`)
- **Pasta de bases de apoio** (`SupportRoot`)
- **Pasta temporária** (`TempPath` — usada no Registro `TEMP`; vazio = `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()` do Windows)
- **Senha Firebird** (campo protegido; caixa **Salvar senha** grava protegida por DPAPI)
- **Usar modo escuro** (persiste `DarkMode` e reaplica o tema na hora)
- **Sempre criar estrutura multi-base (pastas ARQ01/ARQ00)** (`ForceMultiBase`) — mesmo quando a SL não traz pastas `ARQ##`: o fluxo cria `ARQ01\` e move a `ARQSIST.FDB` para lá, cria `ARQ00\` e move `ARQEMP.FDB`/`ARQOCOR.FDB` para lá (ou copia da pasta de apoio/aninhadas), e usa o Registro no formato multi-base (`BASE1`…`BASE99` apenas com correspondente + `EMPRESA` = `ARQ00`). Os arquivos `*.fdb` são procurados também em subpastas (fora das pastas `ARQ##`); mais de uma candidata interrompe com aviso.
- **Quantidade de bases** (`BaseCount`, padrão 4) — define quantas chaves `BASE1`…`BASE<n>` são gravadas no Registro no modo simples (`ARQsist` fora do modelo multi-base). Limitado a 1…99.
- **Notificações de perguntas/mensagens** — com a aplicação **minimizada**, qualquer pergunta/mensagem pendente dispara sempre um balloon da bandeja ("Traz Base") com o título do diálogo e a primeira linha do texto; clicar no balloon restaura a janela.

Alterações de tema, senha, multi-base forçado e quantidade de bases são salvas imediatamente; os demais campos são salvos ao iniciar operações, ao fechar o app e na saída.

## Fluxo 8 — Atualização do aplicativo

Ao abrir a janela, em segundo plano:

1. Consulta `https://api.github.com/repos/BrunoPasseti21/Traz-base/releases/latest` (timeout 15 s).
2. Se `tag_name` (sem o `v` opcional) for maior que a versão do assembly e existir asset cujo nome termina em `Setup.exe` com URL `https`, mostra o botão "Atualização disponível · vX.Y.Z" no rodapé do menu (ou uma animação/barra de progresso durante o download).
3. Ao clicar: confirmação → download para `%TEMP%\TrazBase-Setup-<guid>.exe` com progresso → execução com `Verb=runas` e `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CLOSEAPPLICATIONS` → o app fecha.
4. Falha na verificação/download é silenciosa (o uso normal não é afetado); o usuário pode tentar de novo.
5. A atualização em `Program Files` exige UAC; versões antigas são bloqueadas pelo instalador.

## Estados de uma operação

`Aguardando entrada → Validando → Confirmando alterações → Executando → Concluída | Falhou | Cancelada`

Cada etapa produz mensagem no log/status. Ao falhar, o fluxo não avança automaticamente e a exceção é exibida com a etapa afetada.
