using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DllTeste.Banco.Autenticacao;

public static class LoginEndpointExtensions
{
    public const string LoginPath = "/login";
    public const string LogoutPath = "/logout";
    public const string EndpointLogin = "/account/login";

    /// <summary>
    /// Registra autenticação por cookie com sessão no servidor (ITicketStore),
    /// expiração por inatividade e as opções do login.
    /// NÃO registra o ILoginAuthenticator: a implementação fica a cargo do app
    /// (EfLoginAuthenticator hoje, AD no futuro).
    /// </summary>
    public static IServiceCollection AddLoginPadrao(
        this IServiceCollection services,
        Action<LoginOptions>? configurar = null)
    {
        var opcoes = new LoginOptions();
        if (configurar is not null)
            configurar(opcoes);

        services.AddSingleton(opcoes);

        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheTicketStore>();
        services.AddAntiforgery();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = LoginPath;
                options.LogoutPath = LogoutPath;
                options.AccessDeniedPath = LoginPath;
                options.ExpireTimeSpan = opcoes.TempoInatividade;
                options.SlidingExpiration = true;   // renova a cada atividade => logout por inatividade
                options.Cookie.Name = opcoes.NomeCookie;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

        // Liga o armazenamento da sessão no servidor ao esquema de cookie.
        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<MemoryCacheTicketStore>((options, store) =>
            {
                options.SessionStore = store;
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Mapeia os endpoints que gravam/limpam o cookie:
    ///   POST /account/login  -> valida e faz SignIn
    ///   POST /logout         -> faz SignOut
    /// </summary>
    public static IEndpointRouteBuilder MapLoginPadrao(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(EndpointLogin, async (
            HttpContext http,
            ILoginAuthenticator autenticador,
            IAntiforgery antiforgery,
            LoginOptions opcoes) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Redirect($"{LoginPath}?error=token");
            }

            IFormCollection form = await http.Request.ReadFormAsync();
            string usuario = form["usuario"].ToString().Trim();
            string senha = form["senha"].ToString();
            string lembrarRaw = form["lembrar"].ToString();
            bool lembrar = lembrarRaw == "true" || lembrarRaw == "on";

            string returnUrl = form["returnUrl"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl) ||
                !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            {
                returnUrl = opcoes.UrlPosLogin;   // bloqueia open-redirect
            }

            string returnUrlEnc = Uri.EscapeDataString(returnUrl);

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
                return Results.Redirect($"{LoginPath}?error=campos&returnUrl={returnUrlEnc}");

            LoginResult resultado = await autenticador.ValidarAsync(usuario, senha, http.RequestAborted);
            if (!resultado.Sucesso)
                return Results.Redirect($"{LoginPath}?error=credenciais&returnUrl={returnUrlEnc}");

            var identity = new ClaimsIdentity(
                resultado.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var propriedades = new AuthenticationProperties
            {
                IsPersistent = lembrar,
                ExpiresUtc = lembrar
                    ? DateTimeOffset.UtcNow.Add(opcoes.DuracaoLembrarMe)
                    : null
            };

            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, principal, propriedades);

            return Results.Redirect(returnUrl);
        })
        .DisableAntiforgery(); // validamos manualmente acima

        endpoints.MapPost(LogoutPath, async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect(LoginPath);
        })
        .DisableAntiforgery();

        return endpoints;
    }
}
