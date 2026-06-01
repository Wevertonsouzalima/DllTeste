using DllTeste.Banco.Centralizador;
using DllTeste.Banco.SistemaPaginas.Enums;
using DllTeste.Banco.SistemaPaginas.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;

namespace DllTeste.Banco.SistemaPaginas.Services;

public sealed class SistemaPaginasService
{
    private readonly CentralizadorConnectionFactory _connectionFactory;
    private readonly ILogger<SistemaPaginasService>? _logger;

    private static readonly ConcurrentDictionary<string, CacheMenuSistema> _cacheMenu = new();

    private static readonly TimeSpan TempoCacheMenu = TimeSpan.FromMinutes(10);

    public SistemaPaginasService(
        CentralizadorConnectionFactory connectionFactory,
        ILogger<SistemaPaginasService>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    public async Task<List<SistemaPaginaMenuItem>> CarregarMenuAsync(
        string nomeSistema,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
            throw new ArgumentException("Nome do sistema não informado.", nameof(nomeSistema));

        var chaveCache = CriarChaveCacheMenu(nomeSistema);

        if (_cacheMenu.TryGetValue(chaveCache, out var cache) && !cache.Expirou)
            return cache.Itens;

        var paginas = await ListarPaginasAsync(
            nomeSistema: nomeSistema,
            somenteExibirNoMenu: true,
            cancellationToken: cancellationToken);

        var menu = MontarHierarquia(paginas);

        _cacheMenu[chaveCache] = new CacheMenuSistema
        {
            Itens = menu,
            DataExpiracao = DateTime.Now.Add(TempoCacheMenu)
        };

        return menu;
    }

    public async Task<List<SistemaPagina>> ListarPaginasAsync(
        string nomeSistema,
        bool somenteExibirNoMenu = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
            throw new ArgumentException("Nome do sistema não informado.", nameof(nomeSistema));

        try
        {
            await using var connection = _connectionFactory.CriarConexao();
            await connection.OpenAsync(cancellationToken);

            var sql = """
                SELECT
                    p.IdPagina,
                    p.IdSistema,
                    p.IdPaginaPai,
                    p.Chave,
                    p.Titulo,
                    p.Tooltip,
                    p.Rota,
                    p.Icone,
                    p.Ordem,
                    p.TipoPagina,
                    p.StatusPagina,
                    p.ExibirNoMenu,
                    p.AbrirNovaAba,
                    p.Permissao,
                    p.MensagemManutencao,
                    p.DataCriacao,
                    p.DataAtualizacao
                FROM Config.Tb_SistemaPaginas p
                INNER JOIN Config.Tb_Sistemas s ON s.IdSistema = p.IdSistema
                WHERE s.NomeSistema = @NomeSistema
                """;

            if (somenteExibirNoMenu)
                sql += " AND p.ExibirNoMenu = 1 ";

            sql += """
                ORDER BY
                    ISNULL(p.IdPaginaPai, p.IdPagina),
                    p.IdPaginaPai,
                    p.Ordem,
                    p.Titulo;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@NomeSistema", SqlDbType.VarChar, 265)
                {
                    Value = nomeSistema.Trim()
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var paginas = new List<SistemaPagina>();

            while (await reader.ReadAsync(cancellationToken))
            {
                paginas.Add(LerSistemaPagina(reader));
            }

            return paginas;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Erro ao carregar páginas do sistema. Sistema={Sistema}; SomenteMenu={SomenteMenu}",
                nomeSistema,
                somenteExibirNoMenu);

            throw new InvalidOperationException(
                $"Erro ao carregar páginas do sistema '{nomeSistema}'.",
                ex);
        }
    }

    public async Task<SistemaPagina?> BuscarPorChaveAsync(
        string nomeSistema,
        string chave,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
            throw new ArgumentException("Nome do sistema não informado.", nameof(nomeSistema));

        if (string.IsNullOrWhiteSpace(chave))
            return null;

        try
        {
            await using var connection = _connectionFactory.CriarConexao();
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT TOP (1)
                    p.IdPagina,
                    p.IdSistema,
                    p.IdPaginaPai,
                    p.Chave,
                    p.Titulo,
                    p.Tooltip,
                    p.Rota,
                    p.Icone,
                    p.Ordem,
                    p.TipoPagina,
                    p.StatusPagina,
                    p.ExibirNoMenu,
                    p.AbrirNovaAba,
                    p.Permissao,
                    p.MensagemManutencao,
                    p.DataCriacao,
                    p.DataAtualizacao
                FROM Config.Tb_SistemaPaginas p
                INNER JOIN Config.Tb_Sistemas s ON s.IdSistema = p.IdSistema
                WHERE s.NomeSistema = @NomeSistema
                  AND p.Chave = @Chave;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@NomeSistema", SqlDbType.VarChar, 265)
                {
                    Value = nomeSistema.Trim()
                });

            command.Parameters.Add(
                new SqlParameter("@Chave", SqlDbType.VarChar, 150)
                {
                    Value = chave.Trim().ToUpperInvariant()
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return LerSistemaPagina(reader);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Erro ao buscar página por chave. Sistema={Sistema}; Chave={Chave}",
                nomeSistema,
                chave);

            throw new InvalidOperationException(
                $"Erro ao buscar página pela chave '{chave}' no sistema '{nomeSistema}'.",
                ex);
        }
    }

    public async Task<SistemaPagina?> BuscarPorRotaAsync(
        string nomeSistema,
        string rota,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
            throw new ArgumentException("Nome do sistema não informado.", nameof(nomeSistema));

        if (string.IsNullOrWhiteSpace(rota))
            return null;

        var rotaNormalizada = NormalizarRota(rota);

        try
        {
            await using var connection = _connectionFactory.CriarConexao();
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    p.IdPagina,
                    p.IdSistema,
                    p.IdPaginaPai,
                    p.Chave,
                    p.Titulo,
                    p.Tooltip,
                    p.Rota,
                    p.Icone,
                    p.Ordem,
                    p.TipoPagina,
                    p.StatusPagina,
                    p.ExibirNoMenu,
                    p.AbrirNovaAba,
                    p.Permissao,
                    p.MensagemManutencao,
                    p.DataCriacao,
                    p.DataAtualizacao
                FROM Config.Tb_SistemaPaginas p
                INNER JOIN Config.Tb_Sistemas s ON s.IdSistema = p.IdSistema
                WHERE s.NomeSistema = @NomeSistema
                  AND p.Rota IS NOT NULL;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@NomeSistema", SqlDbType.VarChar, 265)
                {
                    Value = nomeSistema.Trim()
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var paginas = new List<SistemaPagina>();

            while (await reader.ReadAsync(cancellationToken))
            {
                paginas.Add(LerSistemaPagina(reader));
            }

            return paginas
                .OrderByDescending(x => CalcularPrioridadeRota(x.Rota))
                .FirstOrDefault(x => RotaCorresponde(x.Rota, rotaNormalizada));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Erro ao buscar página por rota. Sistema={Sistema}; Rota={Rota}",
                nomeSistema,
                rota);

            throw new InvalidOperationException(
                $"Erro ao buscar página pela rota '{rota}' no sistema '{nomeSistema}'.",
                ex);
        }
    }

    public void LimparCacheMenu(string? nomeSistema = null)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
        {
            _cacheMenu.Clear();
            return;
        }

        var chaveCache = CriarChaveCacheMenu(nomeSistema);
        _cacheMenu.TryRemove(chaveCache, out _);
    }

    private static List<SistemaPaginaMenuItem> MontarHierarquia(List<SistemaPagina> paginas)
    {
        var itensPorId = paginas
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Titulo)
            .ToDictionary(
                x => x.IdPagina,
                x => new SistemaPaginaMenuItem
                {
                    Pagina = x
                });

        var raiz = new List<SistemaPaginaMenuItem>();

        foreach (var item in itensPorId.Values
                     .OrderBy(x => x.Pagina.Ordem)
                     .ThenBy(x => x.Pagina.Titulo))
        {
            if (item.Pagina.IdPaginaPai.HasValue &&
                itensPorId.TryGetValue(item.Pagina.IdPaginaPai.Value, out var pai))
            {
                pai.Filhos.Add(item);
            }
            else
            {
                raiz.Add(item);
            }
        }

        OrdenarFilhos(raiz);

        return raiz;
    }

    private static void OrdenarFilhos(List<SistemaPaginaMenuItem> itens)
    {
        itens.Sort((a, b) =>
        {
            var ordem = a.Pagina.Ordem.CompareTo(b.Pagina.Ordem);

            if (ordem != 0)
                return ordem;

            return string.Compare(
                a.Pagina.Titulo,
                b.Pagina.Titulo,
                StringComparison.OrdinalIgnoreCase);
        });

        foreach (var item in itens)
            OrdenarFilhos(item.Filhos);
    }

    private static SistemaPagina LerSistemaPagina(SqlDataReader reader)
    {
        return new SistemaPagina
        {
            IdPagina = reader.GetInt32(reader.GetOrdinal("IdPagina")),
            IdSistema = reader.GetInt32(reader.GetOrdinal("IdSistema")),
            IdPaginaPai = LerIntNullable(reader, "IdPaginaPai"),

            Chave = LerString(reader, "Chave") ?? string.Empty,
            Titulo = LerString(reader, "Titulo") ?? string.Empty,
            Tooltip = LerString(reader, "Tooltip"),
            Rota = LerString(reader, "Rota"),
            Icone = LerString(reader, "Icone"),

            Ordem = reader.GetInt32(reader.GetOrdinal("Ordem")),

            TipoPagina = (TipoPaginaSistema)reader.GetInt32(reader.GetOrdinal("TipoPagina")),
            StatusPagina = (StatusPaginaSistema)reader.GetInt32(reader.GetOrdinal("StatusPagina")),

            ExibirNoMenu = reader.GetBoolean(reader.GetOrdinal("ExibirNoMenu")),
            AbrirNovaAba = reader.GetBoolean(reader.GetOrdinal("AbrirNovaAba")),

            Permissao = LerString(reader, "Permissao"),
            MensagemManutencao = LerString(reader, "MensagemManutencao"),

            DataCriacao = reader.GetDateTime(reader.GetOrdinal("DataCriacao")),
            DataAtualizacao = LerDateTimeNullable(reader, "DataAtualizacao")
        };
    }

    private static string? LerString(SqlDataReader reader, string coluna)
    {
        var ordinal = reader.GetOrdinal(coluna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? LerIntNullable(SqlDataReader reader, string coluna)
    {
        var ordinal = reader.GetOrdinal(coluna);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTime? LerDateTimeNullable(SqlDataReader reader, string coluna)
    {
        var ordinal = reader.GetOrdinal(coluna);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static string CriarChaveCacheMenu(string nomeSistema)
    {
        return $"MENU:{nomeSistema.Trim().ToUpperInvariant()}";
    }

    private static string NormalizarRota(string rota)
    {
        var valor = rota.Trim();

        var indiceQueryString = valor.IndexOf('?', StringComparison.Ordinal);

        if (indiceQueryString >= 0)
            valor = valor[..indiceQueryString];

        if (!valor.StartsWith('/'))
            valor = "/" + valor;

        if (valor.Length > 1)
            valor = valor.TrimEnd('/');

        return valor;
    }

    private static bool RotaCorresponde(string? rotaTemplate, string rotaAtual)
    {
        if (string.IsNullOrWhiteSpace(rotaTemplate))
            return false;

        var template = NormalizarRota(rotaTemplate);

        if (template.Equals(rotaAtual, StringComparison.OrdinalIgnoreCase))
            return true;

        var partesTemplate = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var partesAtual = rotaAtual.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (partesTemplate.Length != partesAtual.Length)
            return false;

        for (var i = 0; i < partesTemplate.Length; i++)
        {
            var parteTemplate = partesTemplate[i];
            var parteAtual = partesAtual[i];

            if (EhParametroRota(parteTemplate))
                continue;

            if (!parteTemplate.Equals(parteAtual, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool EhParametroRota(string parte)
    {
        return parte.StartsWith('{') && parte.EndsWith('}');
    }

    private static int CalcularPrioridadeRota(string? rota)
    {
        if (string.IsNullOrWhiteSpace(rota))
            return 0;

        var partes = rota.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var segmentosFixos = partes.Count(x => !EhParametroRota(x));

        return segmentosFixos * 10 + partes.Length;
    }

    private sealed class CacheMenuSistema
    {
        public List<SistemaPaginaMenuItem> Itens { get; set; } = new();
        public DateTime DataExpiracao { get; set; }

        public bool Expirou => DateTime.Now >= DataExpiracao;
    }
}