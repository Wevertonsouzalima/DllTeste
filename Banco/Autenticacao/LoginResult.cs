using System.Security.Claims;

namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Resultado de uma tentativa de login. Em caso de sucesso traz as claims
/// que compõem a identidade do usuário no cookie/sessão.
/// </summary>
public sealed class LoginResult
{
    public bool Sucesso { get; private set; }
    public string? Erro { get; private set; }
    public IReadOnlyList<Claim> Claims { get; private set; } = Array.Empty<Claim>();

    private LoginResult()
    {
    }

    public static LoginResult Ok(IEnumerable<Claim> claims)
    {
        return new LoginResult
        {
            Sucesso = true,
            Claims = claims.ToList()
        };
    }

    public static LoginResult Falha(string erro)
    {
        return new LoginResult
        {
            Sucesso = false,
            Erro = erro
        };
    }
}
