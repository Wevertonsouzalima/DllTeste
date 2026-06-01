using DllTeste.Componentes.Menu.Enums;

namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Opções do login. Definidas em AddLoginPadrao(...).
/// </summary>
public sealed class LoginOptions
{
    /// <summary>Título exibido na tela.</summary>
    public string Titulo { get; set; } = "Entrar";

    /// <summary>Subtítulo opcional (ex.: nome do sistema).</summary>
    public string? Subtitulo { get; set; }

    /// <summary>URL de um logo opcional exibido acima do título.</summary>
    public string? UrlLogo { get; set; }

    /// <summary>Nome do cookie de autenticação.</summary>
    public string NomeCookie { get; set; } = ".DllTeste.Auth";

    /// <summary>
    /// Tempo de inatividade até a desconexão automática (SlidingExpiration).
    /// </summary>
    public TimeSpan TempoInatividade { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Duração da sessão quando "Lembrar-me" está marcado.</summary>
    public TimeSpan DuracaoLembrarMe { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Para onde redirecionar após o login (quando não há returnUrl).</summary>
    public string UrlPosLogin { get; set; } = "/";

    /// <summary>
    /// Nome do cookie onde o site guarda o tema atual ("claro"/"escuro").
    /// A tela de login lê esse cookie para respeitar a config do site.
    /// </summary>
    public string NomeCookieTema { get; set; } = "dll-tema";

    /// <summary>Tema usado quando não há cookie nem parâmetro (fallback).</summary>
    public TemaSistema TemaPadrao { get; set; } = TemaSistema.Claro;
}
