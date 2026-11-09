# Plano de implementação — status

As fases originais foram executadas; o aplicativo está funcional na versão **0.3.0**. Este documento registra o que foi entregue e o que ainda falta.

## Fase 1 — Confirmar o comportamento — **concluída**

- A aplicação nasceu do script legado `Traz-base-sl.bat`; o significado das opções foi traduzido diretamente em telas (Transferir, Alterar base, Redefinir base, Comandos ARQEMP/ARQSIST).
- Decisões de negócio tomadas e implementadas: ver [Riscos e decisões](RISCOS-E-DECISOES.md).

## Fase 2 — Fundação da aplicação — **concluída**

- Solução WinForms em C#/.NET 10 criada (`TrazBase.App`).
- Configuração nomeada em JSON (`%APPDATA%\TrazBase\settings.json`) com DPAPI para a senha.
- Captura de exceções e exibição via diálogos próprios; **log apenas em memória** (painel por tela) — ver lacunas.
- Serviço central de processos **não foi criado**: optou-se por eliminar dependências de executáveis externos (cópia nativa, `ZipArchive`, SharpCompress, `FbConnection`), restando como único processo o instalador da atualização.

## Fase 3 — Operações locais — **concluída**

- Listagem e seleção de bases na tela **Alterar base**.
- Validação estrutural de pastas/arquivos em todos os fluxos (SL somente dígitos, existência de pastas, `ARQSIST.FDB` obrigatório, bases de apoio).
- Atualização de `HKCU\SIC\CONEXAO` com relatório do resultado (fluxos Transferir, Redefinir e Alterar base).

## Fase 4 — Cópia e extração — **concluída**

- Cópia nativa com progresso por bytes, cancelamento e preservação de timestamps/atributos.
- Detecção e seleção de ZIP/RAR (lista marcável quando há vários; extração direta quando há um).
- Proteção contra path traversal nas entradas; movimentação de `.fdb` para a raiz com detecção de conflito.

## Fase 5 — Firebird — **parcialmente concluída**

- Execução configurável dos SQLs em `ARQEMP.FDB`/`ARQSIST.FDB` via ADO.NET (`FbScript`/`FbBatchExecution`), com confirmação e saída visível: **OK**.
- Consulta de versão (`CONTROLE.NRO_SCRIPT`) ao final do fluxo Transferir: **OK**.
- Restauração `.FBK` → `.FDB`: **OK** — tela **Restaurar base** porta a opção 4 do BAT (arquivos reconhecidos na subpasta `BKP` quando existe, senão na própria pasta selecionada, em paralelo via `FbRestore.ExecuteAsync`, substituição apenas após confirmação).
- Falhas sem avanço automático: **OK** (exceção interrompe o fluxo com diálogo; na restauração, falhas individuais são isoladas por base e aparecem no log e no resumo).

## Fase 6 — Preparação para uso — **parcialmente concluída**

- Interface em pt-BR com tema claro/escuro, DPI PerMonitorV2, diálogos próprios: **OK**.
- Instalador (Inno Setup) com escolha de pasta, bloqueio de downgrade e atualização silenciosa via GitHub Releases: **OK**.
- Documentação para operadores: este diretório (`docs/`).
- Validação manual com bases descartáveis e comparação com o BAT: responsabilidade contínua do operador; não há matriz formal preenchida.
- Testes automatizados: **parciais** — `TrazBase.Tests` cobre a lógica pura (`BaseOperationRules`, `ArchivePathSafety`, `GitHubReleaseUpdater.ParseLatestRelease`, proteção DPAPI de senha em `AppSettings`); a UI e os fluxos de `MainForm` não têm cobertura.

## Entregáveis — status

| Entregável | Status |
|---|---|
| Aplicação Windows e código-fonte | OK (v0.3.0) |
| Configuração inicial documentada | OK ([Configuração](CONFIGURACAO.md)) |
| Log de operações e instruções de diagnóstico | Parcial (log em tela; sem arquivo) |
| Guia de instalação/atualização | OK ([Desenvolvimento](DESENVOLVIMENTO.md) + `README.md` + `Instalador/LEIA-ME.txt`) |
| Matriz de validação manual por fluxo | Pendente |

## Próximos passos (ordem sugerida)

1. **Ampliar a cobertura de testes** para os fluxos menos óbvios (`MainForm`) ou, ao extrair lógica de `MainForm`, cobrir a nova lógica pura; manter `BaseOperationRules`/`ArchivePathSafety` sempre cobertos.
2. **Log persistente** em arquivo (ex.: `%APPDATA%\TrazBase\logs\`), com rotação simples e nível de detalhe; nunca registrar a senha.
3. **Restauração com segurança adicional**: opcional backup dos `.FDB` substituídos antes da restauração e suporte/validação de `.FBK` multipartes e protegidos por senha.
4. **Backup do Registro** antes de gravar `HKCU\SIC\CONEXAO` (ler e registrar valores anteriores).
5. **Verificação de espaço em disco** antes de copiar/extrair.
6. **Validação manual documentada** por fluxo, com cópias descartáveis de bases.
7. Avaliar modularização (extrair Application/Infrastructure para assemblies próprios) somente se o tamanho de `MainForm.cs` se tornar impeditivo.
