# Riscos e decisões

## Riscos observados no script legado — destino

| Risco no BAT (`Traz-base-sl.bat`) | Situação na aplicação |
|---|---|
| Exclusão recursiva do destino antes de copiar | Substituída por confirmação contextual com três opções: Cancelar / **Criar backup** (renomeia para `.backup-yyyyMMdd-HHmmss`) / **Substituir** (`Directory.Delete` recursivo, somente após escolha explícita). O backup não é automático — o operador escolhe. |
| `SYSDBA` com senha fixa `masterkey` embutida | Removida. A senha é informada pelo operador em cada uso (ou salva protegida por DPAPI). O usuário continua fixo como `SYSDBA`; a senha varia por ambiente. |
| `Caminhos.txt` posicional com barras finais | Substituído por `settings.json` nomeado e legível; caminhos são normalizados com `Path.GetFullPath` onde necessário. |
| Caminhos relativos / `cd`, dependência do diretório corrente | Todos os caminhos de trabalho são absolutos; nada depende do diretório corrente. |
| Detecção de processo pelo nome (global) | Eliminada: não há espera por `gbak`/`isql` porque não há esses processos; o SQL roda via ADO.NET e o cancelamento fecha a conexão. |
| Redirecionamentos para `nul` ocultando erros | Não existem redirecionamentos; erros sobem como exceções e são exibidos com a etapa afetada. |
| Entrada usada em caminhos sem validação | SL validada como somente dígitos; destino resolvido com proteção de path traversal; entradas de compactados validadas contra `..` e caminhos absolutos. |
| `gbak` em segundo plano sem verificação individual | Restauração implementada com `FbRestore.ExecuteAsync` por arquivo, cada resultado acompanhado individualmente; falhas de uma base não bloqueiam as demais, e o log identifica cada uma. |
| Envolvimento BAT-to-EXE do instalador legado | Substituído por Inno Setup + autoatualização via GitHub Releases. |

## Decisões tomadas

1. **Windows somente.** Alvo: Windows 10+ (x64), .NET 10, WinForms, DPI PerMonitorV2.
2. **Distribuição.** Instalador Inno Setup (`TrazBase-Setup.exe`) publicado como asset de GitHub Release no repositório `BrunoPasseti21/Traz-base`; a aplicação detecta versões mais novas pela API `releases/latest` e instala silenciosamente (com UAC). Bloqueia downgrade; permite reinstalar a mesma versão.
3. **Chave do Registro.** Confirmado `HKCU\SIC\CONEXAO`; valores gravados: `BASE1`–`BASE4` e `EMPRESA` = caminho da pasta da base com barra final, `SERVIDOR=LOCALHOST`, `TEMP=pasta temporária do usuário`. Valores anteriores **não** são salvos (pendente).
4. **Credenciais.** Sem senha embutida. Senha informada pelo operador; opcionalmente salva com DPAPI (`CurrentUser`). Usuário `SYSDBA` fixo; dialeto 3; charset UTF8; sem pooling. A credencial real varia por ambiente e nunca vai para o repositório.
5. **Scripts SQL.** Substituídos por **comandos inline** editados no próprio aplicativo (abas ARQEMP/ARQSIST), com realce e salvamento automático. Arquivos `.sql` externos legados foram migrados para o formato inline na primeira execução após a atualização (`CommandScriptsMigrated`); as chaves `ScriptArqEmp`/`ScriptArqSist` foram descontinuadas (mantidas no JSON apenas para a migração).
6. **Múltiplos compactados.** O operador escolhe em lista o que extrair (padrão: todos selecionados); com um único arquivo, extração direta. RAR multipartes e protegidos por senha **não foram validados** com exemplos reais; o SharpCompress suporta ambos em tese, mas não há cobertura de teste nem tratamento específico — tratar como não suportado até validar.
7. **Pasta da SL existente.** Regra de negócio escolhida: o operador decide a cada transferência — criar backup (renomear) ou substituir (apagar). Mesclar não é suportado; não há opção "criar com outro nome".
8. **Restauração `.FBK`.** Implementada como a opção 4 do BAT (tela **Restaurar base**): se existir subpasta `BKP` na pasta escolhida, usa os `BKP\*.fbk`; caso contrário, usa os `.fbk` da própria pasta selecionada. Restaura cada base reconhecida (`ARQEMP`/`ARQOCOR`/`ARQSIST` no nome) como `<NOME>.FDB` **na mesma pasta do `.FBK`**, **em paralelo**, via serviços do Firebird (`FbRestore.ExecuteAsync`), sem `gbak.exe`/`cmd`/PowerShell. `.FBK` não reconhecido → o operador escolhe o destino ou pula o arquivo. `.FDB` existente → substituído somente após confirmação (`Replace` vs `Create`). Ao final, o operador escolhe se o Registro passa a apontar para a pasta restaurada. Pendente: validar `.FBK` multipartes e protegidos por senha.
9. **Execução local do Firebird.** Confirmado acesso ao serviço Firebird em `localhost`; não há suporte a servidor remoto (não previsto).

## Riscos em aberto

- **Cobertura parcial de testes:** `TrazBase.Tests` cobre a lógica pura, mas a UI e os fluxos de `MainForm` (cópia, extração, Registro, SQL) não têm cobertura automatizada. Uma regressão em `MainForm.StartFlowAsync`/`ResetBaseAsync` pode apagar dados fora da pasta esperada. Mitigar: ampliar cobertura ou extrair mais lógica pura; cobertura manual com bases descartáveis.
- **Apagamento irreversível com "Substituir".** A opção não cria cópia; depende de o operador ler e entender o diálogo. Mitigar: manter o texto explicativo atual e considerar desabilitar "Substituir" sem backup no futuro, ou exigir digitação de confirmação.
- **Backup acumula sem limpeza.** Pastas `.backup-*` crescem sem política de retenção. Mitigar: documentar para o operador ou adicionar limpeza opcional.
- **Movimentação de `.fdb` para a raiz.** Em colisão de nome, o fluxo falha no meio (após apagar/mover parcialmente). Mitigar: validar colisões **antes** de mover qualquer arquivo.
- **Restauração cancela/falha no meio.** Um `.FDB` parcialmente escrito pode sobrar na pasta de backups e ser confundido com uma base válida. Mitigar: a restauração pode ser refeita (a substituição exige confirmação); validar o `.FDB` restaurado antes de reutilizá-lo.
- **Arquivo `.FBK` corresponde só pelo nome.** Um arquivo mal rotulado como `ARQEMP.fbk` restaura como `ARQEMP.FDB` mesmo sendo outro banco (o diálogo de destino ajuda, mas o conteúdo não é checado). Mitigar: restaurar sempre com bases descartáveis primeiro.
- **`gbak`-like services sobrescrevem com `Replace`.** Após confirmar, o `.FDB` anterior é irrecuperável sem backup próprio; não há cópia de segurança automática dos `.FDB` substituídos na restauração. Mitigar: confirmar com cuidado; se preciso, copiar os `.FDB` antigos antes.
- **Registro sem backup.** Valores anteriores de `HKCU\SIC\CONEXAO` são perdidos na troca de base. Mitigar: ler e gravar `*.bak` ou log dos valores anteriores.
- **Senha em memória.** A senha fica em texto claro no campo/`_password` durante a execução; inevitável para o provider Firebird, mas reforça não deixar a janela aberta sem uso e não salvar "por conveniência" em máquinas compartilhadas. DPAPI protege apenas o armazenamento, não a memória.
- **Autoatualização depende do GitHub.** Se o repositório/release mudar de nome, a checagem falha silenciosamente (por design). O asset precisa continuar se chamando `*Setup.exe` e ser HTTPS.
- **Instalação em `Program Files`.** Requer UAC a cada atualização; em máquinas com política restritiva, a atualização automática falha e o operador deve instalar manualmente.
- **Varredura de `*.fdb`/`*.rar`/`*.zip` por `SearchOption.AllDirectories`** pode incluir arquivos de pastas aninhadas inesperadas dentro da SL; o operador deve revisar os compactados selecionados.
- **`ARQSIST.FDB` escolhido pelo operador.** O arquivo selecionado é apenas *copiado* para `ARQSIST.FDB` sem validar se é de fato um banco válido do sistema; um arquivo errado só será detectado na etapa SQL. Mitigar: abrir conexão de teste antes de copiar (pendente).
- **Log não persistido.** Diagnóstico pós-fato é impossível sem o operador copiar o texto do painel. Mitigar: gravar log em arquivo (pendente).
- **Encoding/locale.** A UI assume pt-BR e fontes Segoe UI; em Windows sem essas fontes o fallback usa Arial/fontes do sistema (comportamento aceitável, visualmente diferente).
- **`Traz-base-sl.bat` permanece no repositório local** (fora do Git por `.gitignore`, pois contém credenciais). Não publicar nem versionar.

## Recomendações mantidas

Não reintroduzir exclusões automáticas nem credenciais fixas do BAT. Qualquer nova operação destrutiva deve ser explícita, contextual, confirmada pelo operador e auditável (log). Qualquer dependência de processo externo deve declarar como é iniciado, como é cancelado e como sua saída é verificada.
