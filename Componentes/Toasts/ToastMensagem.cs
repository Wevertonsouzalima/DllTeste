namespace DllTeste.Componentes.Toasts;


/// <summary>
/// Representa uma notificacao toast exibida na tela.
/// </summary>
public sealed class ToastMensagem
{
    /// <summary>Identificador unico (usado como @key e para remocao).</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Tipo visual da notificacao.</summary>
    public ToastTipo Tipo { get; init; } = ToastTipo.Info;

    /// <summary>Titulo opcional (negrito).</summary>
    public string? Titulo { get; init; }

    /// <summary>Mensagem principal.</summary>
    public string Mensagem { get; init; } = string.Empty;

    /// <summary>Duracao em milissegundos. Use 0 para nao fechar automaticamente.</summary>
    public int DuracaoMs { get; init; } = 4000;
}
