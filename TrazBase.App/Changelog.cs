namespace TrazBase.App;

internal static class Changelog
{
    private static readonly IReadOnlyDictionary<string, string[]> Notes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["0.9.2"] =
        [
            "Bandeja — Atalhos abrem mais rápido: a listagem das pastas fica em cache de 15 segundos (navegação repetida abre na hora, mesmo em pastas com milhares de arquivos, como a de desenvolvimento) e a extração dos ícones dos executáveis roda em paralelo — antes cada abertura do submenu re-enumerava toda a pasta e extraía ícone por ícone."
        ],
        ["0.9.1"] =
        [
            "Tela \"Atalhos de pasta\" ajustada em monitores menores (ex.: 1366×768 em 125% de escala): a página rola verticalmente quando o conteúdo não cabe, as colunas da grade se ajustam à largura (Nome e Filtro fixos, \"Caminho\" ocupa a sobra) e os botões \"Adicionar / atualizar\" e \"Remover\" ficam sempre abaixo das caixas \"Apenas .exe / .bat / Todos os arquivos\", sem sobreposição.",
            "Tela \"Configurar caminhos\": as linhas da grade acompanham o tema claro/escuro (antes ficavam escuras no tema claro).",
            "Verificação de atualização agora roda a cada 1 minuto (antes apenas na abertura da aplicação), com cache de ETag para não esgotar o rate limit do GitHub."
        ],
        ["0.9.0"] =
        [
            "Novo menu da bandeja totalmente redesenhado: ícones em todas as opções e grade única de alinhamento (ícone e texto em colunas únicas).",
            "Nova opção \"Buscar chamado\" na bandeja — informe o número do chamado (até 12 dígitos) e pressione Enter; abre a busca no navegador, igual ao SupraAgil.",
            "Nova opção \"Alterar base\" na bandeja — digite o nome da base e o Registro aponta para ela (respeita a estrutura multi-base ARQ##/ARQ00).",
            "Nova tela \"Atalhos de pasta\" (menu acima de Configuração): pastas favoritas com filtro por tipo de arquivo (.exe/.bat/todos) e duplo clique abrindo no Explorer. O mesmo submenu está na bandeja, navegável por subpastas, com os ícones dos executáveis.",
            "Ícone do Traz Base na bandeja só aparece com a janela oculta/minimizada (segundo plano); tooltip com a etapa atual e a porcentagem; duplo clique restaura a janela.",
            "Fechar a janela com o segundo plano desligado pergunta antes de sair; o \"Fechar\" da bandeja encerra sem perguntar.",
            "Instância única: abrir o TrazBase.exe novamente traz a janela existente para frente (sem abrir outra janela).",
            "Notificações em segundo plano viraram toast do Windows, com fallback para balão quando o toast não está disponível.",
            "Tela inicial: novas opções \"Configurar caminhos\" e \"Atalhos de pasta\" (opções antigas caem na Transferir); lista em ordem alfabética.",
            "Alterar base respeita a estrutura multi-base (pastas ARQ##) independente da opção \"Sempre criar estrutura multi-base\" — o Registro aponta BASE<n> para cada ARQ## e EMPRESA para a ARQ00.",
            "Nova opção \"Apontar a EMPRESA do Regedit para a pasta de bases de apoio\" nas Configurações.",
            "Submenus da bandeja com altura máxima de ~20 itens e rolagem nativa (setas ou roda do mouse); indicativo de submenu (\"›\") sempre visível.",
            "Ícones em 16×16 em todos os menus (mesmo em telas com escala); ícone do Copiar senha SIC (Ctrl+Alt+C), Olho nas senhas e Tela de Atalhos renovada.",
            "Novos ícones/opção: \"Atualização de executáveis\" com submenu maior; fechar a janela sem o segundo plano pergunta antes de sair."
        ],
        ["0.8.1"] =
        [
            "A atualização disponível agora aparece com o ícone de download no botão azul do rodapé mesmo com o menu lateral recolhido (o botão não desenhava a própria imagem).",
            "A janela agora é ajustada à área útil do monitor ao abrir, sem deixar o rodapé para fora da tela (telas menores e segundo monitor).",
            "Menu expandido: a mensagem de atualização disponível é exibida com fonte menor e aparece inteira no botão."
        ],
        ["0.8.0"] =
        [
            "Tela \"Atualização de executáveis\" renovada: card de resumo compacto (botão à esquerda), apenas \"Atualizar executáveis\" e mensagens mais curtas.",
            "Nova tela \"Configurar caminhos executáveis\" no menu: tabela editável com a lista dos executáveis a atualizar, com botão … para escolher pastas e arquivos e menu de contexto para marcar/desmarcar Renomeia com a versão em lote.",
            "A coluna nome arquivo aceita qualquer arquivo (qualquer extensão) e armazena apenas o nome sem extensão (correto para arquivos com extensão composta como .exe.zip).",
            "Checkbox \"Usar um único destino para todos os executáveis\" para compartilhar o destino e ocultar a coluna Destino; destino por linha continua disponível.",
            "Com \"Renomeia com a versão\" marcado, a extração já salva o .exe com sufixo _<versão> na pasta de destino (ex.: SIC_146.exe), sem etapa separada de renomeação.",
            "Mensagens enxutas: só a lista dos executáveis que falharam; sucesso curto.",
            "Correção de falha com a barra de progresso (anterior causava erro antes da extração começar)."
        ],
        ["0.7.0"] =
        [
            "Nova seleção de base ao trocar (Alterar base): versão do SIC exibida no canto superior direito, igual ao fluxo de Transferência.",
            "Redefinir base agora lista as pastas de bases como a Alterar base (com pré-seleção pelo Registro) e aceita pastas sem número de SL no nome (ex.: 'Teste', 'Teste_123456').",
            "O campo SL da redefinição aceita texto livre (SL ou nome da pasta, ex.: 123456, Teste, Teste_123456).",
            "Tema claro: checkboxes e áreas de texto (SQL, logs) agora acompanham o tema, com cores no padrão VS Code Light+.",
            "Tema claro/escuro: botões desenhados com cantos arredondados de verdade (sem sangramento visual).",
            "Memos e caixas de seleção redesenhados no padrão visual próprio do aplicativo.",
            "Hint da pasta de bases de apoio fixado em 500 px com quebra de linha, acompanhando o tema.",
            "Transferir: botão Cancelar agora segue o padrão secundário dos demais fluxos.",
            "Renomeação da pasta da SL agora prioriza o nome fantasia (NFANTASIA) com fallback para o nome e para a SL.",
            "Ícone de ajuda (Ajuda.svg) para a pasta de bases de apoio.",
            "Alterar base: a área da lista usa o mesmo fundo do memo escuro."
        ],
        ["0.6.0"] =
        [
            "Nova tela \"Novidades\" com resumo das mudanças da versão instalada — aparece automaticamente na primeira execução após cada release e clicando na versão no rodapé do menu.",
            "Configuração do editor SQL e área de logs melhoradas; barra de progresso da atualização com bytes processados em tempo real.",
            "Tela inicial configurável (Transferir, Alterar base, Redefinir, Restaurar, Atualizar SIC, Configuração, Comandos ARQEMP/ARQSIST).",
            "Novos handlers de erros inesperados mostram os detalhes dentro do aplicativo antes de fechar.",
            "Diálogos de mensagem agora são arrastáveis pela barra de título; hover do botão de fechar igual ao do Windows.",
            "Diálogos de mensagem: título sem negrito; borda do botão de fechar removida.",
            "Botão × dos diálogos responde mesmo quando flat-style; navegação por teclado (Enter) cobre todas as telas."
        ],
        ["0.5.0"] =
        [
            "Nova aba \"Atualizar SIC\" — executáveis SIC agora são baixados e extraídos dentro do aplicativo, sempre na versão configurada, sem scripts externos.",
            "Progresso visual da atualização em tempo real com os bytes processados.",
            "Tratamento multi-base automático para SLs com pastas ARQ##: ARQEMP/ARQOCOR são movidas para a pasta ARQ00 e a base ARQ01 é usada para a ARQSIST.",
            "Nova opção \"Sempre criar estrutura multi-base\" nas Configurações para aplicar essa estrutura inclusive quando a SL não trouxer as pastas ARQ##.",
            "Quantidade de bases do Registro (BASE1…) agora é configurável (padrão 4); o Registro ainda remove chaves sem correspondência.",
            "Configurações do editor SQL agora persistem automaticamente.",
            "Tela inicial configurável (Transferir, Alterar base, Redefinir, Restaurar, Atualizar SIC, Configuração, Comandos ARQEMP/ARQSIST).",
            "Diálogos de mensagem agora são arrastáveis pela barra de título, com o hover do botão de fechar igual ao do Windows.",
            "Handler global de exceções exibe detalhes do erro dentro do aplicativo."
        ],
        ["0.4.1"] =
        [
            "Mais de 30 mensagens de erro reformuladas em linguagem direta e instrucional, com etapa afetada e o que fazer."
        ],
        ["0.4.0"] =
        [
            "Nova aba \"Restaurar base\" — restaura os backups .FBK da subpasta BKP (ou da pasta selecionada) em paralelo direto no .FDB ao lado de cada .FBK.",
            "Restauração native via FbRestore (sem gbak.exe, cmd ou PowerShell)."
        ],
        ["0.3.0"] =
        [
            "Cancelamento cooperativo em cópias, extrações e comandos SQL.",
            "Backup ou substituição explícita do destino ao trazer uma SL existente.",
            "Tela inicial clara com Transferir, Alterar base, Redefinir e Configuração."
        ]
    };

    public static string GetNotesText(Version version)
    {
        var key = $"{version.Major}.{version.Minor}.{version.Build}";
        var text = new System.Text.StringBuilder();
        text.AppendLine($"Novidades da versão {key}");
        text.AppendLine();
        if (Notes.TryGetValue(key, out var items) && items.Length > 0)
        {
            foreach (var item in items) text.AppendLine("  • " + item);
        }
        else
        {
            text.AppendLine($"  Apenas pequenas correções internas na versão {key}.");
        }
        return text.ToString();
    }
}
