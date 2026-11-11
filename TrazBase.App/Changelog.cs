namespace TrazBase.App;

internal static class Changelog
{
    private static readonly IReadOnlyDictionary<string, string[]> Notes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
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
