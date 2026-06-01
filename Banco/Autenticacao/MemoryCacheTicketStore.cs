using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Guarda o ticket de autenticação no SERVIDOR (IMemoryCache).
/// O cookie do navegador passa a conter apenas um ID de sessão.
/// Combinado com a expiração deslizante, entrega a desconexão por inatividade.
///
/// Para múltiplas instâncias / persistência entre reinícios, troque IMemoryCache
/// por IDistributedCache (Redis/SQL) mantendo a mesma interface ITicketStore.
/// </summary>
public sealed class MemoryCacheTicketStore : ITicketStore
{
    private const string KeyPrefix = "AuthSession-";
    private readonly IMemoryCache _cache;

    public MemoryCacheTicketStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        string key = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();

        DateTimeOffset? expiraEm = ticket.Properties.ExpiresUtc;
        if (expiraEm.HasValue)
            options.SetAbsoluteExpiration(expiraEm.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(key, ticket, options);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
