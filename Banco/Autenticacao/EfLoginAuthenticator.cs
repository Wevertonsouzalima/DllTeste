using DllTeste.Banco.Models;
using DllTeste.Banco.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Valida credenciais contra a tabela Usuarios usando o ManipulacaoService
/// (mesmo padrão de acesso a dados do resto do projeto).
///
/// É genérico no TContext porque o DbContext concreto é resolvido por sistema
/// (via ConexaoCentralizada). Registre fechando o tipo, ex.:
///
///   builder.Services.AddScoped&lt;ILoginAuthenticator, EfLoginAuthenticator&lt;MeuDbContext&gt;&gt;();
///
/// Migrar para AD no futuro: crie AdLoginAuthenticator : ILoginAuthenticator
/// e troque apenas o registro acima.
/// </summary>
public class EfLoginAuthenticator<TContext> : ILoginAuthenticator
    where TContext : DbContext
{
    private readonly ManipulacaoService<TContext, Usuario> _usuarios;

    public EfLoginAuthenticator(
        IDbContextFactory<TContext> contextFactory,
        ILogger<EfLoginAuthenticator<TContext>>? logger = null)
    {
        // Reaproveita toda a infra de tratamento de erro/log do ManipulacaoService.
        _usuarios = new ManipulacaoService<TContext, Usuario>(contextFactory, logger);
    }

    public async Task<LoginResult> ValidarAsync(
        string usuarioOuEmail,
        string senha,
        CancellationToken cancellationToken = default)
    {
        string entrada = usuarioOuEmail.Trim();

        Usuario? usuario = await _usuarios.PrimeiroOuPadraoAsync(
            filtro: u => u.Ativo && (u.NomeUsuario == entrada || u.Email == entrada),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        // Mensagem genérica em ambos os casos (não revela se o usuário existe).
        if (usuario is null || !PasswordHasher.Verificar(senha, usuario.SenhaHash))
            return LoginResult.Falha("Credenciais inválidas.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NomeExibicao),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("usuario", usuario.NomeUsuario)
        };

        return LoginResult.Ok(claims);
    }
}
