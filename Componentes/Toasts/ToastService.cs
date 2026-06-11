using System.Collections.Concurrent;

namespace DllTeste.Componentes.Toasts;

/// <inheritdoc cref="IToastService" />
public sealed class ToastService : IToastService, IDisposable
{
    private readonly List<ToastMensagem> _mensagens = new();
    private readonly ConcurrentDictionary<Guid, System.Timers.Timer> _timers = new();
    private readonly object _trava = new();

    public event Action? AoMudar;

    public IReadOnlyList<ToastMensagem> Mensagens
    {
        get
        {
            lock (_trava)
            {
                return _mensagens.ToList();
            }
        }
    }

    public void Mostrar(ToastMensagem toast)
    {
        if (toast is null)
        {
            return;
        }

        lock (_trava)
        {
            _mensagens.Add(toast);
        }

        if (toast.DuracaoMs > 0)
        {
            AgendarRemocao(toast.Id, toast.DuracaoMs);
        }

        AoMudar?.Invoke();
    }

    public void Sucesso(string mensagem, string? titulo = null, int duracaoMs = 4000)
    {
        Mostrar(new ToastMensagem
        {
            Tipo = ToastTipo.Sucesso,
            Titulo = titulo,
            Mensagem = mensagem,
            DuracaoMs = duracaoMs
        });
    }

    public void Erro(string mensagem, string? titulo = null, int duracaoMs = 6000)
    {
        Mostrar(new ToastMensagem
        {
            Tipo = ToastTipo.Erro,
            Titulo = titulo,
            Mensagem = mensagem,
            DuracaoMs = duracaoMs
        });
    }

    public void Aviso(string mensagem, string? titulo = null, int duracaoMs = 5000)
    {
        Mostrar(new ToastMensagem
        {
            Tipo = ToastTipo.Aviso,
            Titulo = titulo,
            Mensagem = mensagem,
            DuracaoMs = duracaoMs
        });
    }

    public void Info(string mensagem, string? titulo = null, int duracaoMs = 4000)
    {
        Mostrar(new ToastMensagem
        {
            Tipo = ToastTipo.Info,
            Titulo = titulo,
            Mensagem = mensagem,
            DuracaoMs = duracaoMs
        });
    }

    public void Remover(Guid id)
    {
        bool removeu;

        lock (_trava)
        {
            removeu = _mensagens.RemoveAll(m => m.Id == id) > 0;
        }

        if (_timers.TryRemove(id, out var timer))
        {
            timer.Stop();
            timer.Dispose();
        }

        if (removeu)
        {
            AoMudar?.Invoke();
        }
    }

    private void AgendarRemocao(Guid id, int duracaoMs)
    {
        var timer = new System.Timers.Timer(duracaoMs)
        {
            AutoReset = false
        };

        timer.Elapsed += (_, _) => Remover(id);
        _timers[id] = timer;
        timer.Start();
    }

    public void Dispose()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Stop();
            timer.Dispose();
        }

        _timers.Clear();
    }
}
