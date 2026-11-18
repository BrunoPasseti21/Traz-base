# Fluxos de operação

Este documento descreve os fluxos **como estão implementados** na versão 0.3.0.

## Estrutura da janela e navegação

**Instância única** — abrir o `TrazBase.exe` com a aplicação já em execução **não abre outra janela**: o segundo processo detecta a instância em execução (mutex `Local\TrazBase.SingleInstance`), sinaliza-a (evento `Local\TrazBase.ShowWindow`) e encerra na hora. A instância em execução então **restaura a janela** (se estava oculta/minimizada na bandeja) e a traz para frente. O escopo é a sessão do usuário — cada sessão do Windows mantém a sua instância.

Janela mínima 1020 × 760, inicial 1180 × 900, DPI `PerMonitorV2`. Layout em duas colunas: menu lateral (recolhido por padrão, 68 px; expandido com o botão sanduíche, 300 px) e área de conteúdo. Ao alternar para uma tela (e ao abrir o sistema), o foco vai automaticamente para o primeiro campo editável dela: **Transferir** → campo SL; **Alterar base** → lista de bases, com a base indicada pelo Registro (`HKCU\SIC\CONEXAO\BASE1`) já pré-selecionada quando encontrada; **Redefinir base** → lista de bases (com a base indicada pelo Registro pré-selecionada e a SL preenchida pela seleção); **Restaurar base** → campo da pasta selecionada; **Atualização de executáveis** → botão "Atualizar executáveis"; **Configurar caminhos executáveis** → grade de executáveis; **Comandos ARQEMP/ARQSIST** → editor de comandos SQL; **Configuração** → campo "Origem das solicitações". Em todas as telas, a tecla **Enter** move o foco para o próximo campo editável, terminando no botão/controle principal da tela. Itens do menu:
1. **Transferir** — copiar/extrair e preparar base por solicitação.
2. **Alterar base** — apontar o Registro para uma base local já existente.
3. **Redefinir base** — validar/preparar uma base local existente para a SL.
4. **Restaurar base** — restaurar os `.FBK` da pasta selecionada (standalone).
5. **Atualização de executáveis** — rodar a atualização dos executáveis configurados (ação equivalente ao `Atualiza_Tudo.cmd`).
6. **Configurar caminhos executáveis** — editar a tabela de executáveis (origem, versão, destino, nome e renomear por linha).
7. **Configuração** — pastas, senha Firebird e tema.
8. **Comandos ARQEMP** — editor SQL executável na base `ARQEMP.FDB`.
9. **Comandos ARQSIST** — editor SQL executável na base `ARQSIST.FDB`.

O rodapé do menu mostra a versão do aplicativo (clicar exibe a tela **Novidades** com as alterações da versão), o crédito "Criado por Bruno Passeti" e, quando há versão mais nova publicada no GitHub, um botão de atualização. A tela **Novidades** aparece automaticamente na primeira vez que a aplicação abre após cada release (quando o `DefaultScreen` é diferente de `Transferir`, a tela escolhida abre primeiro, seguida da tela de Novidades) — ou seja, uma vez por versão instalada; clicar na versão do rodapé a reabre manualmente.

**Tela Novidades**: diálogo com o cabeçalho do app (arrastável, com o hover igual ao do Windows) e uma lista legível das mudanças da versão instalada. O botão **Entendi** (padrão) fecha — e registra que a versão foi exibida em `LastChangelogVersion` (configuração).

## Fluxo 1 — Transferir (tela inicial)

Pré-condições validadas ao clicar em **Trazer e preparar base** (ou Enter no campo SL):

1. SL informada e contendo somente dígitos.
2. Pastas local das bases (`BasesRoot`) e de apoio (`SupportRoot`) configuradas e existentes.
3. Pasta de origem das solicitações (`SolicitationRoot`) acessível.
4. Senha Firebird informada (não exige salvar).
5. Configuração persistida antes de começar; falha ao salvar aborta o fluxo.

Ao iniciar a transferência efetiva (passadas todas as validações/confirmações), a aplicação dispara uma notificação **"Transferência iniciada"** (toast do Windows) — apenas quando o operador não está vendo a janela (iniciada pela bandeja, ou com a janela oculta/minimizada); com a janela aberta o progresso já aparece na tela.

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

Cancelamento (botão **Cancelar** ou fechamento da janela): define o `CancellationToken`; cópias/extrações/SQL abortam cooperativamente, o SQL fecha a conexão; a tela registra "Operação cancelada." O fechamento da janela durante a execução é bloqueado até a operação terminar.

## Fluxo 2 — Alterar base

1. Lista, na pasta `BasesRoot`, as subpastas existentes (ordenadas, sem navegação recursiva).
2. O operador seleciona uma pasta; o caminho completo aparece no campo inferior, e a tela exibe no canto superior direito **"Versão atual: …"** (consulta `SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE` na `ARQSIST.FDB` da pasta selecionada — raiz no modo simples, `ARQ01` no modelo multi-base). A versão é atualizada ao exibir a tela, ao alterar a seleção e após a troca da base; falhas (base ilegível, senha ausente) só alteram o texto do rótulo.
3. **Trocar base** (ou duplo clique) grava no Registro, atualiza a versão exibida e confirma com mensagem; **Abrir pasta** abre o Explorer.
4. A seleção do modo de Registro (simples × multi-base) é **deduzida da própria pasta**: havendo pastas `ARQ##` (ex.: `ARQ01` + `ARQ00`), o Registro aponta para elas — `BASE<n>` = caminho de cada `ARQ<n>` (exceto `ARQ00`), chaves `BASE1`…`BASE99` sem pasta correspondente são removidas e `EMPRESA` = `ARQ00\` —, **independente** da opção **Sempre criar estrutura multi-base** (que atua só na preparação da SL pela Transferir). Sem pastas `ARQ##`, mantém-se o comportamento anterior (`BASE1`…`BASE<n>` + `EMPRESA` = a pasta escolhida). Se a estrutura multi-base estiver incompleta (pasta `ARQ00` ausente), a troca é recusada com aviso e o Registro não é alterado. A pré-seleção pela base atual do Registro também reconhece a estrutura multi-base (a base é pré-selecionada quando `BASE1` aponta para uma pasta `ARQ##` dela).

Valores gravados em `HKCU\SIC\CONEXAO` (modo simples, pelas trocas/preparações):

| Valor    | Conteúdo                                        |
|----------|-------------------------------------------------|
| `BASE1`…`BASE<n>` | caminho completo da pasta da base, com `\` final — quantidade definida em **Quantidade de bases** nas Configurações (padrão 1) |
| `EMPRESA` | idem (mesmo caminho) — ou a **pasta de apoio**, quando `UseSupportRootAsCompany` está ligado |
| `SERVIDOR` | `LOCALHOST`                                   |
| `TEMP`   | `TempPath` configurada, ou `HKCU\SIC\CONEXAO\TEMP` se existir; senão `Path.GetTempPath()` |

## Registro — modo multi-base

Quando a SL traz pastas `ARQ##` (detectado automaticamente, inclusive na troca de base pela tela **Alterar base** e no apontamento do Registro oferecido ao final da **Restaurar base**):

| Valor | Conteúdo |
|---|---|
| `BASE<n>` | caminho completo da pasta `ARQ##`, com `\` final — uma chave por pasta (ex.: `ARQ01`→`BASE1`, `ARQ05`→`BASE5`) |
| `EMPRESA` | caminho completo da pasta `ARQ00` (onde ficam `ARQEMP.FDB` e `ARQOCOR.FDB`), com `\` final — ou a **pasta de apoio**, quando `UseSupportRootAsCompany` está ligado |
| `SERVIDOR` | `LOCALHOST` |
| `TEMP` | `TempPath` configurada, ou `HKCU\SIC\CONEXAO\TEMP` se existir; senão `Path.GetTempPath()` |

Além de gravar, as chaves `BASE1`…`BASE99` **sem pasta correspondente são removidas** (ex.: só `ARQ01` e `ARQ05` → `BASE2`, `BASE3`, `BASE4`, `BASE6`… são apagadas). `ARQ00` não gera chave `BASE` — alimenta apenas `EMPRESA`. SLs sem pastas `ARQ##` mantêm o comportamento anterior (`BASE1`…`BASE4` e `EMPRESA` = pasta da SL).

Observações: os valores anteriores **não** são salvos antes da gravação (a proposta original previa isso; não foi implementado). O Registro é do usuário atual; não exige elevação para `HKCU`. As exclusões no modo multi-base só apagam chaves `BASE1`…`BASE99` inexistentes na SL.

## Fluxo 3 — Redefinir base

Tela com instruções, **listagem idêntica à da tela Alterar base** (pastas encontradas em `BasesRoot`, com a base indicada pelo Registro — `HKCU\SIC\CONEXAO\BASE1` — já pré-selecionada) e campo de **SL/nome da pasta**. Selecionar uma pasta na lista faz a redefinição atuar sobre **a pasta escolhida** (mesmo sem SL no nome), copia o nome da pasta para o campo e exibe o caminho em "Selecionada:"; em pastas renomeadas na transferência (`<nome>_<SL>`), o nome completo também é copiado. Editar o campo manualmente limpa a seleção e devolve o usuário ao modo "digitar o nome"; o campo aceita texto livre (ex.: `123456`, `JOAO`, `JOAO_123456`). Duplo clique na lista inicia a redefinição. A tela mantém o log próprio ("Andamento da redefinição", com carimbo de hora). Ao clicar em **Redefinir base**:

1. Texto digitado: nome simples de pasta dentro de `BasesRoot` — sem separadores, sem ".." nem caracteres inválidos (resolvido sem diferenciar maiúsculas/minúsculas); pasta selecionada precisa existir; `BasesRoot` e `SupportRoot` configuradas e existentes; senha Firebird informada.
2. Pasta alvo = pasta selecionada na lista, ou a resolução do texto digitado em `BasesRoot` (pastas cujo nome **começa** com o texto — ex.: digitar `AL` lista `ALLES` e `ALLES_34742`; e, com o último segmento só dígitos, também pastas terminadas em `_<dígitos>`, p. ex. `123456` → `JOAO_123456`) — a pasta precisa existir (a redefinição não copia nem substitui a pasta inteira). Quando mais de uma pasta corresponde ao texto (ex.: `ALLES` e `ALLES_34742` ao digitar `AL`), o app abre uma tela de seleção ("Várias bases encontradas") para o operador escolher; cancelar a seleção interrompe a operação sem alterar nada.
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

## Fluxo 5 — Atualização de executáveis e Configurar caminhos executáveis

Dois itens no menu lateral (grupo **Atualização**):

### Atualização de executáveis

Botão único **Atualizar executáveis** para rodar todas as linhas configuradas; mostra um resumo (executáveis configurados) e avisa caso nenhum esteja configurado. Segue o comportamento do `Atualiza_Tudo.cmd` para cada linha:

1. Pede confirmação explícita (**Sim/Não**) mostrando origem(ns) e destino(s) e avisando que os destinos serão sobrescritos.
2. Durante a cópia/extração mostra uma **barra de progresso** com percentual real (baseada nos bytes extraídos) e o status ("Extraindo SIC.exe.zip…", etc.); some após a conclusão.
3. Para cada linha, abre o `.exe.zip`/`.zip` **direto da pasta de versão** na origem (somente leitura; o arquivo nunca é copiado para o destino) e extrai os arquivos sobre o destino:
   - se o `.zip` não existir na origem, registra um aviso no log e **segue para o próximo**;
   - **se uma etapa do arquivo falhar, avisa pelo log e continua com o próximo arquivo** (não aborta os demais). Cancelamento pelo operador encerra a operação.
4. Ao final, o resultado é resumido: sucesso total ou um aviso listando os arquivos que falharam (e o motivo de cada um); os demais foram atualizados normalmente. **Cancelar** interrompe o loop entre blocos; navegação desabilitada enquanto roda.
5. **Renomear com a versão** (por linha): a extração já grava o `.exe` com o sufixo `_<versão>` no destino raiz (ex.: `SIC.exe` → `SIC_146.exe`), sem estado intermediário com o nome original. Vale só para exes diretos na raiz do destino (arquivos aninhados em pastas não são renomeados). O arquivo final é sobrescrito quando já existe. A versão usada é a **da própria linha** (somente letras/dígitos/`_`; caracteres inválidos são descartados).

### Configurar caminhos executáveis

Tela de **gerenciamento da tabela** (mesma estrutura do Excel): o operador edita as linhas direto na grade (`DataGridView`):

- **Caminho de sincronização** — pasta-base de onde o `.exe`/`.zip` vem (ex.: `...\Sistemas\SIC`). Botão com três pontos (`…`) ao lado do campo para escolher a pasta.
- **Versão/Nome da pasta** — subpasta dentro da origem (ex.: `146`). A pasta efetiva de origem é `<origem>\<versão>` (versão vazia usa `s126` apenas como verificação de compatibilidade).
- **Destino** — pasta que recebe os arquivos extraídos. Tem botão com três pontos (`…`) ao lado. **Oculta** quando a checkbox "Usar um único destino para todos os executáveis" está marcada.
- **nome arquivo** — nome do arquivo a baixar, sem a extensão (ex.: `SIC` → `SIC.exe.zip`); `.exe.zip` é adicionado automaticamente se o nome não terminar em `.zip`; também aceita `.zip` puro. O botão com três pontos (`…`) ao lado abre o seletor de arquivos e aceita **qualquer arquivo** (qualquer extensão), gravando apenas o nome sem a extensão.
- **Renomeia com a versão** — checkbox **por linha**; já durante a extração, os `.exe` daquela linha gravados direto na raiz do destino recebem o sufixo `_<versão>`.
- **Botão direito** na grade mostra o menu contextual com as opções **Renomear todos os exes** (marca todas as checkboxes) e **Não renomear os exes** (desmarca todas).
- Botões **Adicionar executável** / **Remover selecionado** no topo da tabela manipulam as linhas.
- **Destino único** (card no topo da aba): marcada a checkbox, usa um único destino para todos; desmarcada, cada linha tem o seu próprio destino (coluna Destino volta a aparecer).

As configurações são próprias e separadas (`UpdateSicSingleDestinationEnabled`, `UpdateSicSingleDestinationRoot`, `UpdateSicEntries`); nada a ver com as configurações das bases (SL); salvas como as demais (debounce + ao fechar/iniciar operações). Na primeira abertura após a atualização, se a tabela estiver vazia e existirem `UpdateSicSourceRoot`/`UpdateSicDestinationRoot`/`UpdateSicSicVersion` antigas, o app semeia uma linha por executável padrão (`UpdateSicExecutables` em `UpdateSicOperationRules.cs`); chaves legadas ficam preservadas só para o fallback de compatibilidade. Aspectos demais do `.cmd` (EDE) não foram portados.

## Fluxo 6 — Comandos ARQEMP / Comandos ARQSIST

Duas telas idênticas para os dois bancos. Cada uma tem:

- Campo "Base Firebird (.FDB)" com botão **Procurar** (filtro `*.fdb;*.gdb`).
- Editor SQL com realce de sintaxe (comentários, strings, palavras-chave, números) em paleta escura fixa.
- **Testar comando**: exige base existente, SQL não vazio e senha; pede confirmação explícita (avisa que comandos de alteração modificam dados); executa via `FbScript`/`FbBatchExecution` e mostra o resultado em painel de saída ("Resultado do teste").
- Comandos são salvos automaticamente ~700 ms após a última digitação (`_commandSaveTimer`) e na saída do aplicativo. A base Firebird (.FDB) escolhida para testar **não é persistida** — vale apenas para a sessão atual.
- Migração automática: configurações antigas que apontavam para arquivos `.sql` externos (`ScriptArqEmp`/`ScriptArqSist`) são incorporadas aos comandos inline na primeira execução após a atualização (`CommandScriptsMigrated`).

## Fluxo 10 — Atalhos de pasta

Nova tela (criada na barra lateral como **Atalhos de pasta**, acima de **Configuração**) para cadastrar atalhos para pastas com nome livre, caminho e filtro por tipo de arquivo:

- Duas áreas: **Pastas cadastradas** (lista de colunas Nome / Caminho / Filtro; duplo clique abre a pasta no Explorer) e **Adicionar / editar atalho** (campos Nome, Caminho, filtro **Apenas .exe** / **Apenas .bat** / **Todos os arquivos**, com botões **Adicionar / atualizar**, **Remover**).
- Editar: selecione um item na lista para preencher o formulário. **Adicionar / atualizar** salva o item e limpa o formulário automaticamente para o próximo cadastro (equivalente a um "Novo"); **Remover** exclui o selecionado.
- O filtro é exibida entre as colunas e controla quais arquivos aparecem nas listagens filtradas (use no submenu que a tela **Atalhos de pasta** expõe para abrir a lista; ou programaticamente via `FolderShortcut.FilterLabel`).
- Atalhos ficam em `FolderShortcuts` dentro do `settings.json` (persistidos a cada alteração, como as demais configurações).
- O mesmo submenu também fica disponível na **bandeja** (ícone de segundo plano → menu de contexto → **Atalhos**). O submenu é gerado ao abrir, lê sempre do `settings.json`, e oferece: **Abrir esta pasta no Explorer** (sempre), **abrir arquivos** com o filtro da pasta ativo, **navegar subpastas**, e **Gerenciar atalhos...** para abrir este cadastro.


## Fluxo 7 — Configuração

Campos persistidos em `%APPDATA%\TrazBase\settings.json`:

- **Origem das solicitações** (`SolicitationRoot`)
- **Pasta local das bases** (`BasesRoot`)
- **Pasta de bases de apoio** (`SupportRoot`) — acompanhada de um ícone de ajuda (`Ajuda.svg`). Ao passar o mouse sobre ele, abre um popup próprio (até 500 px de largura, texto quebrado automaticamente) explicando que a pasta deve conter `ARQEMP.FDB` e `ARQOCOR.FDB` e que a aplicação copia automaticamente esses arquivos para a pasta local da base sempre que eles não vierem na solicitação. O popup acompanha o tema (claro/escuro) e se fecha ao mover o mouse para fora do ícone ou quando a janela perde o foco.
- **Apontar a EMPRESA do Regedit para a pasta de bases de apoio** (`UseSupportRootAsCompany`, padrão `false`) — checkbox logo abaixo do campo de apoio. Ligado: toda troca/preparação de base grava `EMPRESA` = a pasta de apoio (com `\` final), no lugar do caminho padrão (a pasta da base, no modo simples; `ARQ00`, no multi-base). Se a pasta de apoio estiver vazia ou inacessível, a aplicação usa o caminho padrão e registra um aviso no log da operação.
- **Pasta temporária** (`TempPath` — usada no Registro `TEMP`; vazio = `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()` do Windows)
- **Senha Firebird** (campo protegido; caixa **Salvar senha** grava protegida por DPAPI; botão de olho ao lado mostra/oculta o conteúdo)
- **Senha padrão do SIC** (campo protegido, logo abaixo da senha Firebird; caixa **Salvar senha** grava protegida por DPAPI; botão de olho ao lado mostra/oculta o conteúdo) — com uma senha salva, o menu da bandeja exibe "Copiar senha padrão SIC" e o atalho **Ctrl+Alt+C** copia a senha para a área de transferência em qualquer tela do aplicativo (também quando minimizado para a bandeja).
- **Senha do sistema Empresa** (campo logo abaixo do "Senha padrão do SIC"; ícone de ajuda (`Ajuda.svg`) à esquerda do rótulo — ao passar o mouse sobre ele, abre popup próprio explicando que a senha digitada é a parte fixa e que o aplicativo concatena automaticamente o resultado do cálculo ao copiar (atalho Ctrl+Alt+X), com o exemplo "TESTE" + (Dia × Hora × 13); caixa **Salvar senha** grava protegida por DPAPI; botão de olho ao lado mostra/oculta o conteúdo) — senha digitada, por exemplo `TESTE`. O atalho global **Ctrl+Alt+X** copia para a área de transferência o resultado da conta: **senha + (hora × dia × 13)**, com a hora local (0–23) e o dia do mês (1–31) — minutos e segundos são ignorados. Exemplos: dia 17, 16h → `TESTE3536`; meia-noite de dia 1 → `TESTE0`. Mesmo comportamento do recurso "Senha padrão (base)" do SupraAgil.
- **Alternância de tema** — pílula claro/escuro (`Claro.svg`/`Escuro.svg`) presa à barra lateral, visível e acessível a partir de qualquer tela; alterna e persiste `DarkMode` na hora (sol = tema claro, lua = tema escuro)
- **Fechar a janela minimiza para a bandeja do Windows (segundo plano)** (`MinimizeToTrayOnClose`, padrão `false`) — ao fechar a janela, o Traz Base some para a bandeja em vez de encerrar; na primeira vez da sessão, um toast explica o menu da bandeja. **Sem** essa opção (padrão), fechar a janela (× / Alt+F4) pergunta antes de encerrar: **"Deseja realmente fechar a aplicação?"** (Sim/Não; botão padrão **Não**). "Fechar" no menu da bandeja encerra de verdade, sem perguntar.
- **Iniciar o TrazBase em segundo plano (na bandeja)** (`StartInBackground`, padrão `false`) — ao abrir o aplicativo, a janela não é exibida e o TrazBase inicia escondido na bandeja do Windows; os atalhos **Ctrl+Alt+C** e **Ctrl+Alt+X**, o menu da bandeja e a verificação silenciosa de atualização seguem funcionando. A janela é restaurada com o duplo clique no ícone da bandeja ou pelo menu.
- **Iniciar o TrazBase com o Windows** (`StartWithWindows`, padrão `false`) — registra o TrazBase para abrir no login do Windows (chave `TrazBase` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, com o caminho do executável). No arranque, a chave é recriada com o caminho atual caso o executável tenha mudado de pasta. Combinada com a opção anterior, o TrazBase abre já na bandeja.
- **Sempre criar estrutura multi-base (pastas ARQ01/ARQ00)** (`ForceMultiBase`) — mesmo quando a SL não traz pastas `ARQ##`: o fluxo cria `ARQ01\` e move a `ARQSIST.FDB` para lá, cria `ARQ00\` e move `ARQEMP.FDB`/`ARQOCOR.FDB` para lá (ou copia da pasta de apoio/aninhadas), e usa o Registro no formato multi-base (`BASE1`…`BASE99` apenas com correspondente + `EMPRESA` = `ARQ00`). Os arquivos `*.fdb` são procurados também em subpastas (fora das pastas `ARQ##`); mais de uma candidata interrompe com aviso.
- **Quantidade de bases** (`BaseCount`, padrão 1) — define quantas chaves `BASE1`…`BASE<n>` são gravadas no Registro no modo simples (`ARQsist` fora do modelo multi-base). Limitado a 1…99.
- **Tela inicial** (`DefaultScreen`, padrão "Transferir") — define qual tela o app exibe ao abrir. Opções (em ordem alfabética): Alterar base, Atalhos de pasta, Atualização de executáveis, Configurar caminhos, Redefinir base, Restaurar base, Transferir. (Valores antigos "Configuração", "Comandos ARQEMP" e "Comandos ARQSIST" caem na Transferir.)
- **Renomear pasta da solicitação para o nome fantasia (ou o nome) do cliente** (`RenameSlFolderToClientName`) — no fluxo "Transferir", após os comandos SQL da base, a aplicação executa `SELECT FIRST 1 C.NOME, C.NFANTASIA FROM ARQCAD C WHERE C.TIPOC = 'N'` na **ARQSIST** e renomeia a pasta de destino para a **primeira palavra do NFANTASIA** (ou, se o NFANTASIA estiver vazio/nulo, do NOME; se os dois estiverem vazios, a pasta mantém o número da SL) + **`_<SL>`** (ex.: `"JOAO_123456"`). A validação de "pasta já existe" ao iniciar a SL considera a pasta exata `<SL>` ou qualquer pasta que termine em `_<SL>` — ideal para detectar bases renomeadas anteriormente.
- **Notificações de segundo plano** — toda notificação exibida com a aplicação em segundo plano (janela oculta ou minimizada), ou iniciada pela bandeja — início/fim/erro de transferências e atualizações, senhas, atalhos, dicas de bandeja e avisos — é um **toast do Windows** (Central de Notificações), com a marca **Traz Base** (nome/ícone resolvidos pelo atalho do menu Iniciar com o AppUserModelID `BrunoPasseti.TrazBase`, criado pelo instalador). Clicar no toast restaura a janela. Se o toast falhar (ex.: atalho do menu Iniciar ausente ou ambiente restrito), a aplicação cai para o balão da bandeja. Falhas do mecanismo nunca interrompem operações (falha é silenciosa). **Se nenhuma notificação aparecer**, confira se o atalho do menu Iniciar tem o `AppUserModelID` `BrunoPasseti.TrazBase` — sem o AUMID no atalho, o Windows descarta o toast silenciosamente; reinstale com a versão que o grava, ou atualize o atalho.

Alterações de tema, senha, multi-base forçado, EMPRESA-apoio e quantidade de bases são salvas imediatamente; os demais campos são salvos ao iniciar operações, ao fechar o app e na saída.

## Fluxo 8 — Bandeja do Windows (tray)

O ícone "Traz Base" na bandeja **só fica visível com a janela oculta ou minimizada** (segundo plano); com a janela aberta ele some. Ao passar o mouse sobre ele durante um processo, o tooltip mostra a etapa atual e a porcentagem (quando há barra de progresso).

- **Botão direito** abre o menu (todas as linhas exibem ícone à esquerda, desenhado pelo `TrayMenuRenderer` numa grade única — x=2 para ícone, x=24 para o texto; submenus com margem de ícones), nesta ordem:
  1. **Transferir** — linha com ícone (`Transferir.svg`) + texto estático "Transferir" + um edit ao lado, para informar o **Nº da SL** (apenas dígitos). Enter no edit inicia a transferência com essa SL, igual ao botão "Iniciar" da tela Transferir (mesma validação/senha/caminhos). **A janela não restaura**: a ação roda inteiramente em segundo plano e o resultado chega por toast do Windows ("Pronto!" quando concluída; erro detalhado caso falhe).
  2. **Alterar base** — linha com ícone (`Trocar-Base.svg`) + texto estático "Alterar base" + um edit ao lado, para informar o **nome da base** (ex.: `123456`, `AL` ou `JOAO_123456`). Enter resolve a base em `BasesRoot` (pastas cujo nome **começa** com o texto — ex.: `AL` lista `ALLES` e `ALLES_34742`; e, com o último segmento só dígitos, também sufixo `_<dígitos>` — mesma regra da tela Redefinir base; se mais de uma corresponder, abre a tela de seleção "Várias bases encontradas"), respeita a estrutura multi-base quando ela existir (`ARQ##` define as `BASE<n>` e `EMPRESA` aponta para a `ARQ00\`; `ARQ00` ausente recusa a troca) e aponta o Registro para ela — igual à tela **Alterar base**. O resultado chega por toast (mensagem com o nome da base, ou o motivo da falha); a troca também fica registrada no log.
  3. Divisória.
  4. **Buscar chamado** — linha com ícone (`Chamado.svg`) + texto estático "Buscar chamado" + um edit ao lado, para informar o **número do chamado** (até 12 dígitos). Enter abre a busca do chamado no navegador padrão (`https://suporte.suprasys.com.br/intranet/admin/find_tickets.php?q=<número>&what=seqid`, como no SupraAgil — a sessão de acesso do navegador ao suporte é usada; o login pode ser necessário). Número inválido/vazio: toast informando o que fazer.
  5. Divisória.
  6. **Atualizar executáveis** — atalho: inicia a atualização dos executáveis configurados sem restaurar a janela; a confirmação "Deseja atualizar os executáveis?" é pulada quando iniciada pela bandeja, pois a ação é deliberada. Também dispara toast no início ("Atualização iniciada") e ao final (sucesso ou lista dos que falharam, com dica para ver o log na tela). Ícone `Atualizar.svg`.
  7. **Copiar senha padrão SIC** — ícone `Copiar.svg`. Aparece **somente se uma senha padrão do SIC estiver salva** em Configurações; copia a senha (DPAPI) para a área de transferência. O atalho de teclado é **Ctrl+Alt+C** e funciona em qualquer lugar do aplicativo — mesmo com a janela minimizada/na bandeja.
  8. Divisória.
  9. **Atalhos** — ícone `Atalho.svg`; submenu carregado junto com o menu (e reavaliado a cada abertura, via `DropDownOpening`), de modo que a seta "›" aparece já na primeira abertura. Mostra as pastas cadastradas em "Atalhos de pasta" (menu acima de Configuração); cada item abre a listagem da própria pasta (subpastas viram novos submenus navegáveis; arquivos abrem com a associação do Windows; só aparecem os arquivos permitidos pelo filtro da pasta; "Abrir esta pasta no Explorer" sempre presente no topo; "Gerenciar atalhos..." ao final abre a tela de gerenciamento). Os itens de **arquivo exibem o ícone associado** (via shell, 16×16, com cache em memória — ícones dos executáveis como no Explorer); os de **pasta** e os de "Abrir esta pasta no Explorer" usam o ícone de pasta do aplicativo (`Pasta.svg`); "Gerenciar atalhos..." usa o ícone de atalho (`Atalho.svg`). A altura dos submenus é limitada a cerca de **20 itens**; o restante aparece com rolagem (setas no topo/rodapé do menu — também funcionam com a roda do mouse).
  10. Divisória.
  11. **Fechar** — ícone `Fechar.svg`. Encerra de verdade, sem perguntar (escolha deliberada no menu). Com `MinimizeToTrayOnClose` ligado, **fechar durante um processo em andamento também apenas esconde a janela** — a operação continua em segundo plano e avisa por toast ao terminar; sem a opção ligada, o fechar é bloqueado enquanto houver operação (com o aviso de sempre).
- **Duplo clique esquerdo** ou clique em um toast restauram a janela (o ícone some da bandeja enquanto a janela estiver aberta).
- Durante uma ação iniciada pela bandeja, diálogos que exigem decisão do usuário (conflito de pasta de destino, escolha de quais arquivos RAR/ZIP extrair) aparecem como janelas próprias; o restante — validações, progresso, conclusão ou erro — vai apenas para o toast ao final.
- Toasts de perguntas/mensagens de diálogos aparecem apenas com a janela minimizada/oculta (comportamento atual); ao minimizar com a opção `MinimizeToTrayOnClose`, um toast informativo explica o menu da bandeja (só na primeira vez por sessão).
- Iniciar uma transferência (bandeja ou o botão **Iniciar** da tela, com a janela oculta/minimizada) dispara o toast **"Transferência iniciada"**.

Com **"Fechar a janela minimiza para a bandeja do Windows (segundo plano)"** marcada em Configurações, clicar em × (ou Alt+F4) esconde a janela em vez de encerrar; a aplicação segue na bandeja. A opção só afeta o botão de fechar — o **Fechar** do menu da bandeja sempre encerra.

## Fluxo 9 — Atualização do aplicativo

Ao abrir a janela e, depois, a cada **1 minuto**, em segundo plano (o timer roda mesmo com a janela oculta/minimizada na bandeja):

1. Consulta `https://api.github.com/repos/BrunoPasseti21/Traz-base/releases/latest` (timeout 15 s), com **ETag** armazenado entre verificações (`If-None-Match`) — respostas `304 Not Modified` não contam no rate limit do GitHub (60/h por IP sem autenticação).
2. Se `tag_name` (sem o `v` opcional) for maior que a versão do assembly e existir asset cujo nome termina em `Setup.exe` com URL `https`, mostra o botão "Atualização disponível · vX.Y.Z" no rodapé do menu (ou uma animação/barra de progresso durante o download). Reexibições da mesma versão não alteram a UI (sem piscar).
3. Ao clicar: confirmação → download para `%TEMP%\TrazBase-Setup-<guid>.exe` com progresso → execução com `Verb=runas` e `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CLOSEAPPLICATIONS` → o app fecha.
4. Falha na verificação/download é silenciosa (o uso normal não é afetado); o usuário pode tentar de novo.
5. A atualização em `Program Files` exige UAC; versões antigas são bloqueadas pelo instalador.

## Estados de uma operação

`Aguardando entrada → Validando → Confirmando alterações → Executando → Concluída | Falhou | Cancelada`

Cada etapa produz mensagem no log/status. Ao falhar, o fluxo não avança automaticamente e a exceção é exibida com a etapa afetada.
