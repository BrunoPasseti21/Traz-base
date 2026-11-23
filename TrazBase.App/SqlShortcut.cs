namespace TrazBase.App;

/// <summary>
/// SQL salvo na tela "Copiar SQL": um nome para identificação e o comando montado pelo usuário.
/// Fica na lista exibida na própria tela e no submenu da bandeja de segundo plano.
/// </summary>
internal sealed class SqlShortcut
{
    /// <summary>Identificador estável, usado para localizar o registro ao editar ou excluir.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Nome exibido para o comando — ex.: "Clientes ativos".</summary>
    public string Title { get; set; } = "";

    /// <summary>Texto SQL copiado para a área de transferência.</summary>
    public string Content { get; set; } = "";
}
