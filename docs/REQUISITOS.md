# Requisitos do projeto

## Objetivo do produto

Substituir o fluxo interativo do `Traz-base-sl.bat` por uma aplicação desktop Windows (Traz Base) que ajuda a preparar e selecionar bases de solicitações para uso local com Firebird.

**Estado:** a aplicação está implementada e publicada na versão **0.3.0**. Este documento lista os requisitos e o status de cada um na versão atual.

## Usuários e ambiente

- Usuário operador de suporte/desenvolvimento em Windows 10 ou superior.
- Bases de solicitações acessíveis por compartilhamento de rede (origem configurável).
- Pastas locais para bases de trabalho e bases de apoio.
- Serviço Firebird local instalado e em execução. A extração de RAR e ZIP é fornecida pelo próprio aplicativo (SharpCompress e `ZipArchive`).

## Requisitos funcionais — status

Legenda: **OK** implementado · **Parcial** implementado com limitações · **Pendente** não implementado.

### Configuração

- **RF-01 — OK.** Diretório local de trabalho das bases configurável e validado (`BasesRoot`).
- **RF-02 — OK.** Diretório de bases de apoio configurável (`SupportRoot`), usado para copiar `ARQEMP.FDB`/`ARQOCOR.FDB` ausentes.
- **RF-03 — Parcial.** Não há diretórios separados de utilitários Firebird: o aplicativo conecta-se ao Firebird pelo serviço local (ADO.NET e serviços de gerenciamento do provider — `FbConnection` e `FbRestore`), sem `isql`/`gbak` externos. O temporário usado no Registro (`TEMP`) pode ser configurado em Configurações (`TempPath`); vazio → `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()`.
- **RF-04 — OK.** Caminho de origem das solicitações na rede configurável (`SolicitationRoot`).
- **RF-05 — OK.** Configuração persistida em JSON versionável e legível (`%APPDATA%\TrazBase\settings.json`), sem depender de ordem de linhas nem de `Caminhos.txt`.

### Operações de base

- **RF-06 — OK.** Cópia recursiva da SL por streaming `FileStream` em blocos de 1 MB, com preservação de data de modificação e atributos dos arquivos.
- **RF-07 — OK.** Andamento em bytes/MB e arquivos copiados na barra de progresso, com log textual da operação.
- **RF-08 — OK.** Detecção de `*.rar` e `*.zip` na pasta copiada; com vários arquivos, o operador escolhe em uma lista (`CheckedListBox`) o que extrair; com um único arquivo, ele é extraído direto. Proteção contra path traversal nos caminhos das entradas.
- **RF-09 — OK.** Seleção de base local existente na tela **Alterar base** (lista de pastas de `BasesRoot`).
- **RF-10 — OK.** Atualização de `HKCU\SIC\CONEXAO`: modo simples → `BASE1`…`BASE<n>` (quantidade definida nas Configurações, padrão 1, limite 99) e `EMPRESA` recebem o caminho completo da base com barra final; modo multi-base (pastas `ARQ##`) → `BASE<n>` por pasta `ARQn` (`\` final), `EMPRESA` = pasta `ARQ00`, e remoção das chaves `BASE1`–`BASE99` sem correspondente; `SERVIDOR=LOCALHOST`; `TEMP` = pasta temporária configurada (vazio: `HKCU\SIC\CONEXAO\TEMP` se existir, senão `Path.GetTempPath()`).
- **RF-11 — Parcial.** A tela **Redefinir base** aceita texto no campo (SL ou nome simples de pasta, com resolução segura em `BasesRoot`: exata ou sufixo `_<dígitos>`), e a pasta local — listando as pastas de `BasesRoot` como na tela **Alterar base**; a redefinição atua sobre a pasta selecionada na lista, mesmo sem SL no nome (com a base do Registro pré-selecionada; pastas `<nome>_<SL>` têm a SL extraída automaticamente). Exige `ARQSIST.FDB` presente (senão informa e interrompe), copia `ARQEMP.FDB`/`ARQOCOR.FDB` da pasta de apoio quando ausentes e executa os comandos SQL configurados. Não renomeia arquivos automaticamente e não copia a pasta inteira.
- **RF-12 — OK.** Restaura os arquivos `.FBK` reconhecidos (`ARQEMP`, `ARQOCOR`, `ARQSIST`) da subpasta `BKP` quando ela existe (senão da própria pasta selecionada) para `.FDB` ao lado deles, pelo serviço de gerenciamento do Firebird (`FbRestore` do provider ADO.NET, `-se service_mgr`), em paralelo; sem `gbak.exe` nem processos externos. Substituição de `.FDB` existente exige confirmação.
- **RF-13 — OK.** Comandos SQL configurados são executados diretamente em `ARQEMP.FDB` e `ARQSIST.FDB` via `FbConnection`/`FbScript`/`FbBatchExecution`, sem `isql.exe`. Conexão: servidor `localhost`, usuário `SYSDBA`, dialeto 3, charset UTF8, sem pooling. No modo multi-base, havendo mais de uma ARQSIST entre as pastas `ARQ##`, o operador escolhe se os comandos ARQSIST rodam em todas elas (ou só na `ARQ01`).
- **RF-14 — OK.** Versão consultada em `ARQSIST.FDB` (`SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE`) e exibida ao final do fluxo **Transferir**. A redefinição não consulta a versão.
- **RF-15 — OK.** Cancelamento cooperativo por `CancellationToken` nas cópias e extrações; no SQL, o cancelamento fecha a conexão Firebird. Operações de Registro e movimentação de arquivos não são canceláveis no meio da etapa.
- **RF-16 — Parcial.** Log de operação existe (painel com carimbo de hora e etapa), mas fica apenas em memória; não é gravado em arquivo. Senhas nunca são registradas.

## Requisitos não funcionais — status

- **RNF-01 — OK.** Interface em português (pt-BR).
- **RNF-02 — OK.** Operações longas rodam fora da thread da UI (`async`/`await`); progresso e etapa atual são exibidos. Balloons de perguntas/mensagens pendentes são sempre exibidos quando a janela está minimizada.
- **RNF-03 — OK (parcial).** Validação prévia de caminhos, existência de pastas/arquivos e senha obrigatória antes de iniciar. Não há verificação de espaço em disco disponível.
- **RNF-04 — OK.** Argumentos de processos externos não são concatenados em shell (não há chamadas a executáveis externos no fluxo principal; a atualização inicia o instalador via `ProcessStartInfo` com `Verb="runas"`). Caminhos de arquivo usam APIs do .NET com `Path`.
- **RNF-05 — OK.** Substituição de pasta existente exige confirmação contextual com três opções explícitas: **Cancelar**, **Criar backup** (renomeia para `.backup-yyyyMMdd-HHmmss`) ou **Substituir** (apaga sem cópia).
- **RNF-06 — Parcial.** Mensagens técnicas aparecem no log e em diálogos próprios; não há arquivo de log persistente. Senhas nunca são exibidas.
- **RNF-07 — Parcial.** O aplicativo indica a pasta/registro afetado antes de agir; a documentação de instalação explica a necessidade de acesso ao compartilhamento (origem) e ao Registro do usuário. Não há texto explicativo in-app sobre permissões de rede/Registro além das mensagens de erro.
- **RNF-08 — Parcial.** Interface, regras puras (`BaseOperationRules`, `ArchivePathSafety`), configuração (`AppSettings`) e integrações (Firebird, Registro, GitHub) estão separadas em arquivos distintos, mas o projeto é um único assembly e a UI (`MainForm`) concentra a orquestração dos fluxos.
- **RNF-09 — OK.** Senha opcionalmente salva protegida por DPAPI (`DataProtectionScope.CurrentUser`); nunca em texto puro nem em logs. Se a descriptografia falhar, a senha é descartada silenciosamente e a opção é desmarcada.
- **RNF-10 — OK.** Todos os botões seguem o padrão documentado em [Padrões de interface](PADROES-DE-INTERFACE.md).

## Fora do escopo inicial

- Alterar os próprios scripts SQL sem decisão explícita do responsável pelo banco (a aplicação edita/executa os comandos configurados, mas não versiona mudanças de conteúdo).
- Atualização automática do Firebird ou da aplicação alvo (o Traz Base se autoatualiza via GitHub Releases; a aplicação alvo não).
- Operação remota em outros computadores.
- Gerenciamento de usuários ou senhas do Firebird (a senha é informada pelo operador; o usuário é sempre `SYSDBA`).

## Critérios gerais de aceite — status

- Configurar e validar dependências sem editar arquivos manualmente: **OK** (tela Configuração + validações no início de cada fluxo).
- Mostrar a base e o caminho usados antes de executar: **OK** (confirmação de destino, diálogos de conflito, telas de resumo nas ações destrutivas).
- Falhas com etapa, contexto e orientação: **Parcial** (mensagens com etapa e causa; não há código de saída de processos externos porque não há processos externos no caminho crítico; não há opção "copiar detalhes técnicos").
- Confirmação contextual para operações destrutivas, sem iniciar após falha de validação: **OK**.

## Lacunas conhecidas

- A restauração cobre apenas `.FBK` reconhecidos por nome (`ARQEMP`/`ARQOCOR`/`ARQSIST`) ou escolhidos manualmente; `.FBK` multipartes e backups criptografados não foram validados.
- Log não persistido em arquivo (RF-16/RNF-06).
- Testes automatizados cobrem a lógica pura (`BaseOperationRules`, `ArchivePathSafety`, `GitHubReleaseUpdater.ParseLatestRelease`, `AppSettings`/DPAPI); a orquestração de UI em `MainForm` ainda não tem cobertura.
- Verificação de espaço em disco não implementada (RNF-03).
