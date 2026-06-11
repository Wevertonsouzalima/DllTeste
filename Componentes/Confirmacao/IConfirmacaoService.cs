namespace DllTeste.Componentes.Confirmacao;

/// <summary>
/// Servico de dialogos de confirmacao. Registrar como Scoped no Blazor Server.
/// </summary>
public interface IConfirmacaoService
{
    /// <summary>Disparado quando um pedido de confirmacao e aberto ou fechado.</summary>
    event Action? AoMudar;

    /// <summary>Opcoes da confirmacao atualmente aberta (null se nenhuma).</summary>
    ConfirmacaoOpcoes? Atual { get; }

    /// <summary>Abre uma confirmacao simples e aguarda a resposta do usuario.</summary>
    /// <returns>true se confirmado, false se cancelado.</returns>
    Task<bool> ConfirmarAsync(ConfirmacaoOpcoes opcoes);

    /// <summary>Sobrecarga de conveniencia com titulo e mensagem.</summary>
    Task<bool> ConfirmarAsync(string titulo, string mensagem);

    /// <summary>Responde a confirmacao aberta (uso interno do dialog).</summary>
    void Responder(bool confirmado);
}
