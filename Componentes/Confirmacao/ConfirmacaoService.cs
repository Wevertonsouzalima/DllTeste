namespace DllTeste.Componentes.Confirmacao;

/// <inheritdoc cref="IConfirmacaoService" />
public sealed class ConfirmacaoService : IConfirmacaoService
{
    private TaskCompletionSource<bool>? _conclusao;

    public event Action? AoMudar;

    public ConfirmacaoOpcoes? Atual { get; private set; }

    public Task<bool> ConfirmarAsync(ConfirmacaoOpcoes opcoes)
    {
        if (opcoes is null)
        {
            return Task.FromResult(false);
        }

        // Se ja houver uma confirmacao aberta, cancela a anterior.
        _conclusao?.TrySetResult(false);

        Atual = opcoes;
        _conclusao = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        AoMudar?.Invoke();
        return _conclusao.Task;
    }

    public Task<bool> ConfirmarAsync(string titulo, string mensagem)
    {
        return ConfirmarAsync(new ConfirmacaoOpcoes
        {
            Titulo = titulo,
            Mensagem = mensagem
        });
    }

    public void Responder(bool confirmado)
    {
        var conclusao = _conclusao;

        Atual = null;
        _conclusao = null;

        AoMudar?.Invoke();
        conclusao?.TrySetResult(confirmado);
    }
}
