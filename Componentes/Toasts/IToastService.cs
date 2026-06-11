namespace DllTeste.Componentes.Toasts;

/// <summary>
/// Servico de notificacoes toast. Registrar como Scoped no Blazor Server.
/// </summary>
public interface IToastService
{
    /// <summary>Disparado sempre que a lista de toasts muda.</summary>
    event Action? AoMudar;

    /// <summary>Toasts atualmente visiveis.</summary>
    IReadOnlyList<ToastMensagem> Mensagens { get; }

    /// <summary>Exibe um toast generico.</summary>
    void Mostrar(ToastMensagem toast);

    /// <summary>Atalho para toast de sucesso.</summary>
    void Sucesso(string mensagem, string? titulo = null, int duracaoMs = 4000);

    /// <summary>Atalho para toast de erro.</summary>
    void Erro(string mensagem, string? titulo = null, int duracaoMs = 6000);

    /// <summary>Atalho para toast de aviso.</summary>
    void Aviso(string mensagem, string? titulo = null, int duracaoMs = 5000);

    /// <summary>Atalho para toast informativo.</summary>
    void Info(string mensagem, string? titulo = null, int duracaoMs = 4000);

    /// <summary>Remove um toast pelo Id.</summary>
    void Remover(Guid id);
}
