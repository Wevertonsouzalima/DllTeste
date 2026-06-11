namespace DllTeste.Componentes.Confirmacao;
/// <summary>
/// Opcoes para um dialogo de confirmacao.
/// </summary>
public sealed class ConfirmacaoOpcoes
{
    /// <summary>Titulo do dialogo.</summary>
    public string Titulo { get; init; } = "Confirmar";

    /// <summary>Mensagem/pergunta exibida.</summary>
    public string Mensagem { get; init; } = string.Empty;

    /// <summary>Texto do botao de confirmacao.</summary>
    public string TextoConfirmar { get; init; } = "Confirmar";

    /// <summary>Texto do botao de cancelamento.</summary>
    public string TextoCancelar { get; init; } = "Cancelar";

    /// <summary>Variante do botao de confirmacao.</summary>
    public BotaoVariante VarianteConfirmar { get; init; } = BotaoVariante.Primario;

    /// <summary>Icone (Bootstrap Icons) do cabecalho.</summary>
    public string? Icone { get; init; } = "bi-question-circle";

    /// <summary>Indica acao destrutiva (ajusta icone/cor padrao quando aplicavel).</summary>
    public bool Perigosa { get; init; }
}
