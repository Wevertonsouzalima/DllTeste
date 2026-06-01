namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Abstração de validação de credenciais.
///
/// Hoje (pessoal): EfLoginAuthenticator (valida na tabela Usuarios via ManipulacaoService).
/// Futuro (empresa): basta criar AdLoginAuthenticator : ILoginAuthenticator e
/// registrar no DI. A tela de login NÃO muda.
/// </summary>
public interface ILoginAuthenticator
{
    Task<LoginResult> ValidarAsync(string usuarioOuEmail, string senha, CancellationToken cancellationToken = default);
}
