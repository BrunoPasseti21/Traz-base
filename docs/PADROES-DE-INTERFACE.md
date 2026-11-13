# Padrões de interface

Todos os valores abaixo refletem o que está implementado em `UiTypography.cs`, `MainForm.cs`, `AppMessageBox.cs`, `ThemedScrollingControls.cs` e `SvgIconLoader.cs`. Ao criar telas novas, reutilize esses padrões em vez de definir estilos locais.

## Tipografia

Centralizada em `UiTypography` (com fallback para Segoe UI / fontes do sistema quando a família Variable não está instalada):

| Uso | Família | Tamanho |
|---|---|---|
| Corpo de texto | Segoe UI Variable Text (ou Segoe UI) | 10 pt |
| Corpo semibold (botões, títulos de grupo) | Segoe UI Variable Text Semibold | 10 pt |
| Títulos de tela ("Transferir", "Configurações"…) | Segoe UI Variable Text Semibold | 21 pt |
| Rótulos/apoio | Segoe UI Variable Text | 8,5–9 pt |
| Descrições do menu lateral | Segoe UI Variable Text | 12 pt |
| Log e editores SQL | Consolas | 10–11 pt |

O `Form.Font` base é `UiTypography.Body()`.

## Cores — paleta clara (padrão)

| Token | Valor | Uso |
|---|---|---|
| Fundo da janela | `#F4F7FB` (244,247,251) | background |
| Superfície (cards) | `White` | painéis de conteúdo |
| Texto principal (ink) | `#1D2A3D` (29,42,61) | rótulos, títulos |
| Texto de apoio (muted) | `#617186` (97,113,134) | legendas, ajuda |
| Campos de entrada | `#F9FBFE` (249,251,254) | TextBox/ListView claro |
| Caixa de seleção (ThemedCheckBox) | fundo `#FFFFFF`, borda `#BBC5D6` (187,197,214); marcada = azul `#1F6FEB` com ✓ branco |

| Borda | `#D0D9E6` (208,217,230) | contornos, botões secundários |
| Destaque/seleção | `#E7F0FF` (231,240,255) | item ativo do menu, hover/pressed |
| Azul primário | `#1F6FEB` (31,111,235) | botões de ação, barra de progresso |
| Hover do azul | `#2D7DF5` (45,125,245) | mouse over |
| Pressed do azul | `#195ECD` (25,94,205) | mouse down |

> Nota: o rodapé do menu (`_appVersionLabel`) usa fundo `#E7F0FF` (claro) / `#263244` (38,50,68, escuro) com texto `#BED0E6` (190,208,230) nos dois temas. O log e os editores SQL acompanham o tema: **escuro** fundo `#171717`/(23,23,23) (mesma cor da barra de rolagem nativa escura, para coerência completa; a área de lista da tela **Alterar base** também usa esse fundo no escuro), texto `#DCE5F2`/(220,229,242), com realce por `HighlightSql` em cores VS Code Dark+; **claro** fundo `#FFFFFF`, texto `#1D2A3D`/ink, realce em paleta VS Code Light+ (comentários verdes `#008000`, strings marrons `#A31515`, palavras-chave azuis `#0000CD`, números verdes `#098658`).

## Cores — paleta escura

| Token | Valor | Uso |
|---|---|---|
| Fundo | `#181E28` (24,30,40) | background |
| Superfície | `#222A37` (34,42,55) | cards |
| Menu/rodapé | `#1C232F` (28,35,47) | sidebar, menu footer, update row |
| Campos | `#2B3545` (43,53,69) | TextBox/ListView |
| Caixa de seleção (ThemedCheckBox) | fundo `#2B3545`, borda `#536277`; marcada = azul `#1F6FEB` com ✓ branco |
| Texto | `#E8EDF5` (232,237,245) | rótulos |
| Texto de apoio | `#A6B4C7` (166,180,199) | legendas |
| Selecionado | `#30435E` (48,67,94) | item ativo do menu |
| Hover | `#26303E` (38,47,62) | mouse over do menu |
| Pressed | `#30435E` (48,67,94) | mouse down |

A barra de título segue o tema em Windows 11+ via DWM (`DWMWA_CAPTION_COLOR`/`TEXT_COLOR`/`BORDER_COLOR`; `ImmersiveDarkMode` em todos os Windows 10 1809+). Em versões sem suporte, os atributos são ignorados sem erro.

## Botões de ação

Padrão único para todos os botões interativos (ações principais e auxiliares), desenhado pelo componente próprio **`RoundedButton`** (`ThemedScrollingControls.cs`). O botão nativo do Windows 11 vazava uma moldura retangular sobre os (aparentes) cantos arredondados; o `RoundedButton` elimina isso:

- Cantos arredondados de verdade: raio padrão **8 px** (`CornerRadius`), recortado via `GraphicsPath` + `Region` (a pintura e a área de clique respeitam o formato — nenhum vazamento retangular).
- Uma única regra de quebra de linha: texto da ação 10 pt semibold; auxiliares 9 pt semibold; `Segoe UI Variable`.
- Pintura própria (`OnPaint`) com `AntiAlias`; `BorderSize = 0` nos primários; hover/pressionado: `MouseOverBackColor`/`MouseDownBackColor` quando definidos, senão calculados por mistura 16%/28% com branco (texto escuro 12%/22% com preto).
- Botões de fechar (`×`) usam `CornerRadius = 0` (retangular).
- Fundo azul `#1F6FEB`, texto branco, `FlatStyle = Flat`.
- Altura **42 px** para ações principais (ex.: Trazer e preparar), 36–40 px para ações de tela (Redefinir, Testar comando), 28–32 px para auxiliares (Limpar, Procurar, Trocar base). A cor, a fonte, o estilo plano e a remoção de borda são obrigatórios em todas.
- Hover `#2D7DF5`, pressed `#195ECD` nos botões que definem essas cores explicitamente.
- Botões secundários (ex.: **Abrir pasta**, **Cancelar** da transferência / da redefinição / da restauração / da Atualização de executáveis): fundo superfície, texto ink (claro) / texto claro (escuro), borda fina `#D0D9E6` (claro) / `#536277` (83,98,119, escuro); altura igual à do botão primário da mesma tela (42 px na Transferência, na Atualização de executáveis, 36 px nas telas Redefinir/Restaurar) e largura 100 px.
- Proibido criar botões de ação como links sublinhados ou introduzir cores/tipografias diferentes sem atualizar este documento.

## Menu lateral

- Recolhido por padrão: largura **68 px**, só ícones SVG (24 px) centralizados.
- Organização por blocos, na ordem: **Transferência** (Transferir, Alterar base, Redefinir base, Restaurar base) → **Atualização** (Atualização de executáveis) → **Configurações** (Configuração, Comandos ARQEMP, Comandos ARQSIST). Divisórias finas (`sidebarSeparator`, `#E2E8F0` claro / `#39434F` escuro) separam os três grupos.
- Expandido pelo botão sanduíche: largura **300 px**, ícone + descrição em **12 pt** alinhados à esquerda com recuo de 18 px.
- **Transição animada**: alternar expande/recolhe com animação de largura de ~220 ms e easing ease-out cúbico (passos de 15 ms). Ao **recolher**, os rótulos somem de imediato; ao **expandir**, os rótulos aparecem ao final da animação. Cliques rápidos no botão sanduíche redirecionam a animação a partir da largura atual, sem sacudidas.
- Opção selecionada recebe fundo `#E7F0FF` (claro) / `#30435E` (escuro); as demais ficam com o fundo do menu e hover próprio.
- Rodapé do menu: versão do aplicativo sempre visível (compacta quando recolhido); "Criado por Bruno Passeti" e a linha de crédito só no modo expandido; botão de atualização quando há release disponível.
- Acima do rodapé fica a **pílula de alternância de tema** (`ThemeToggle`, ícones `Claro.svg` e `Escuro.svg`): visível e acessível a partir de qualquer tela, centralizada na barra lateral (faixa própria, sem sobrepor opções nem textos), funcional também pelo teclado (Tab + Espaço/Enter).
- Navegação desabilitada durante operações longas (exceto o próprio menu/toggle).

## Cards e layout

- Conteúdo organizado em `TableLayoutPanel` com cards (`Panel` com `BorderStyle.FixedSingle`, fundo superfície, padding interno 16 px).
- Cards com texto informativo (listas "• ...") usam o helper `BuildHelpCard`: a altura da linha é medida pelo texto com `TextRenderer.MeasureText` (respeitando `\r\n` e quebra de linha) e reajustada no redimensionamento da página, de modo que toda a caixa se expande para caber o texto — nunca cortar texto por altura fixa.
- Cabeçalho padrão de cada tela: "TRAZ BASE" (azul, semibold) + título grande + subtítulo em texto de apoio.
- Espaçamento: padding de página 26/20 px; altura de cabeçalho 112 px.

## Caixas de diálogo

Todas as mensagens da aplicação usam o diálogo próprio (`AppMessageDialog`), nunca o `MessageBox` nativo. Posicionamento: `StartPosition = Manual` e calculado no centro da área de trabalho do monitor onde a janela-mãe está — nunca `CenterParent`, para que o diálogo permaneça centralizado mesmo com a janela minimizada. Estrutura do diálogo:

- Janela sem moldura, borda de 1 px, cabeçalho azul `#1F6FEB` (claro) / `#1C232F` (escuro) com o título em semibold branco e botão de fechar `×` (hover e pressionar idênticos ao botão de fechar do Windows: `#C42B1C` no hover, `#A11F15` pressionado).
- Cabeçalho também é área de arraste: clicar e arrastar a barra de título (incluindo o espaço entre o título e o botão de fechar) move o diálogo pela tela; o cursor fica como `Arrow` sobre a área de arraste. O botão `×` preserva o hover padrão e não dispara o arraste.
- Ýcone como glifo (`×`, `!`, `?`, `i`) em círculo colorido com 15% de opacidade.
- Botões à direita, altura 32 px; ação primária azul `#1F6FEB` com texto branco; demais primários com borda fina; `Esc` fecha; foco inicial na ação padrão (configurável por diálogo).
- O diálogo de conflito de destino usa três ações: **Criar backup** (primária), **Substituir**, **Cancelar** — com texto explicando que "Substituir remove a pasta atual sem guardar uma cópia".
- A lista de arquivos a extrair (fluxo Transferir, quando há mais de um `*.rar`/`*.zip`) usa o mesmo padrão visual do `AppMessageBox`: moldura sem borda, cabeçalho `#1F6FEB`/`#1C232F` arrastável com `×`, lista `CheckedListBox` no tema (superfície `#2B3545` com texto claro no modo escuro / `#F9FBFE` com tinta no claro) e botões "Extrair selecionados" (azul primário) e "Pular extração" (secundário).
- Largura fixa de 560 px, altura ajustada ao texto medido (máx. 300 px de mensagem).

## Progresso e status

- O painel de progresso **só ocupa espaço enquanto há operação em andamento** (linha dedicada de 56 px na tela Transferir, visível apenas durante a execução).
- Barra customizada (`ThemedProgressBar`) ou preenchimento manual (`_progressFill` sobre trilho claro/escuro), com o percentual real; cor de preenchimento `#1F6FEB`.
- Texto de status imediatamente acima da barra, com contraste adequado (`#5B6E84` claro / texto escuro no tema escuro); mostra percentual, MB transferidos e contagem de arquivos durante a cópia.
- Download de atualização usa barra em marquee (`ThemedProgressBar.SetMarquee`) ou animação (`Baixando.gif`) quando o menu está recolhido.

## Tabelas (DataGridView)

- A tela **Configurar caminhos executáveis** (e apenas ela) usa `DataGridView` para editar a tabela de executáveis.
- `AutoGenerateColumns = false`; colunas `SourceRoot`, `Version`, `DestinationRoot`, `FileName` (texto) e `RenameWithVersion` (checkbox).
- Duas colunas `DataGridViewButtonColumn` com texto `…` para **procurar a pasta** por campo; clique na célula abre `FolderBrowserDialog` com semente no caminho atual.
- `DataSource = BindingList<UpdateSicEntry>` (`UpdateSicEntry.cs`); `AutoSizeColumnsMode = Fill`, `SelectionMode = FullRowSelect`, `MultiSelect = false`.
- Tema claro/escuro aplicado em `ApplyTheme`: fundo/campos, texto, seleção `#1F6FEB` com texto branco, grid/header conforme o tema da sessão. Cabeçalho em quebra de linha automática e alinhamento à esquerda.

## Editores SQL e log

- **Escuro:** fundo `#171717` (23,23,23 — mesma cor da barra de rolagem nativa escura), texto `#DCE5F2`, Consolas, sem borda; rolagem dupla.
- **Claro:** fundo `#FFFFFF`, texto `#1D2A3D`, Consolas, sem borda; rolagem dupla.
- Realce de sintaxe — tema escuro (VS Code Dark+): comentários `#6A9955`, strings `#CE9178`, palavras-chave `#569CD6`, números `#B5CEA8`.
- Realce de sintaxe — tema claro (VS Code Light+): comentários `#008000`, strings `#A31515`, palavras-chave `#0000CD`, números `#098658`.
- Log somente leitura, fonte 10 pt; com botão **Limpar** no cabeçalho do card.

## Ýcones

- SVGs próprios em `Imagens/` (menu sanduíche, Transferir, Trocar-Base, Redefinir, Configuração, SQL, Atualizar, Claro, Escuro), renderizados por `SvgIconLoader` (comandos `M/L/H/V/C/Q/A/Z`, `translate()`, herança de `fill` da raiz do SVG e suporte a `currentColor`).
- Para ícones novos, exporte SVG com `fill` explícito ou `currentColor` na raiz; registre o arquivo em `TrazBase.App.csproj` como `Content` (já coberto pelo glob `..\Imagens\*.svg`).
- Navegação usa os mesmos ícones 24 px; o botão de atualização usa 16 px.

## Acessibilidade e DPI

- `ApplicationHighDpiMode = PerMonitorV2`; fontes e layouts não usam tamanhos fixos dependentes de 96 DPI além dos declarados.
- Texto com contraste suficiente nos dois temas; cor nunca é o único indicador de estado (seleção do menu também usa fundo e posição).
- `AcceptButton` por tela (Enter dispara a ação principal); botões navegáveis por Tab na ordem de leitura.
