using DllTeste.Banco.Centralizador;
using DllTeste.Banco.ConexaoCentralizada.Enums;
using DllTeste.Banco.ConexaoCentralizada.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DllTeste.Banco.ConexaoCentralizada.Services;

public sealed class ConexaoCentralizadaService
{
    private readonly CentralizadorConnectionFactory _connectionFactory;

    public ConexaoCentralizadaService()
        : this(new CentralizadorConnectionFactory())
    {
    }

    public ConexaoCentralizadaService(CentralizadorConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<DadosConexaoSistema> ObterDadosConexaoAsync(
        string nomeSistema,
        AmbienteSistema ambiente,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeSistema))
            throw new ConexaoCentralizadaException("Nome do sistema não informado.");

        try
        {
            await using var connection = _connectionFactory.CriarConexao();
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT TOP (1)
                    NomeSistema,
                    ServidorDev,
                    BancoDadosDev,
                    UsuarioBancoDev,
                    SenhaBancoDev,
                    ServidorProd,
                    BancoDadosProd,
                    UsuarioBancoProd,
                    SenhaBancoProd
                FROM Config.Tb_Sistemas
                WHERE NomeSistema = @NomeSistema
                  AND Ativo = 1;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@NomeSistema", SqlDbType.VarChar, 265)
                {
                    Value = nomeSistema.Trim()
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new ConexaoCentralizadaException($"Sistema '{nomeSistema}' não encontrado ou inativo no centralizador.");

            var dados = ambiente switch
            {
                AmbienteSistema.Dev => new DadosConexaoSistema
                {
                    NomeSistema = reader["NomeSistema"].ToString() ?? string.Empty,
                    Ambiente = "Dev",
                    Servidor = reader["ServidorDev"].ToString() ?? string.Empty,
                    BancoDados = reader["BancoDadosDev"].ToString() ?? string.Empty,
                    UsuarioBanco = reader["UsuarioBancoDev"].ToString() ?? string.Empty,
                    SenhaBanco = reader["SenhaBancoDev"].ToString() ?? string.Empty
                },

                AmbienteSistema.Prod => new DadosConexaoSistema
                {
                    NomeSistema = reader["NomeSistema"].ToString() ?? string.Empty,
                    Ambiente = "Prod",
                    Servidor = reader["ServidorProd"].ToString() ?? string.Empty,
                    BancoDados = reader["BancoDadosProd"].ToString() ?? string.Empty,
                    UsuarioBanco = reader["UsuarioBancoProd"].ToString() ?? string.Empty,
                    SenhaBanco = reader["SenhaBancoProd"].ToString() ?? string.Empty
                },

                _ => throw new ConexaoCentralizadaException($"Ambiente '{ambiente}' não suportado.")
            };

            ValidarDadosConexao(dados);

            dados.ConnectionString = MontarConnectionString(dados);

            return dados;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ConexaoCentralizadaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ConexaoCentralizadaException(
                $"Erro ao obter a connection string do sistema '{nomeSistema}' no ambiente '{ambiente}'.",
                ex);
        }
    }

    public async Task<string> ObterConnectionStringAsync(
        string nomeSistema,
        string ambiente,
        CancellationToken cancellationToken = default)
    {
        var ambienteSistema = ConverterAmbienteSistema(ambiente);

        return await ObterConnectionStringAsync(
            nomeSistema,
            ambienteSistema,
            cancellationToken);
    }

    public async Task<string> ObterConnectionStringAsync(
        string nomeSistema,
        AmbienteSistema ambiente,
        CancellationToken cancellationToken = default)
    {
        var dados = await ObterDadosConexaoAsync(nomeSistema, ambiente, cancellationToken);
        return dados.ConnectionString;
    }

    private static AmbienteSistema ConverterAmbienteSistema(string ambiente)
    {
        if (string.IsNullOrWhiteSpace(ambiente))
            throw new ConexaoCentralizadaException("Ambiente não informado. Valores aceitos: Dev ou Prod.");

        var valor = ambiente.Trim();

        if (valor.Equals("dev", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("desenvolvimento", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            return AmbienteSistema.Dev;
        }

        if (valor.Equals("prod", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("producao", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("produção", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("production", StringComparison.OrdinalIgnoreCase))
        {
            return AmbienteSistema.Prod;
        }

        throw new ConexaoCentralizadaException(
            $"Ambiente '{ambiente}' inválido. Valores aceitos: Dev ou Prod.");
    }

    private static string MontarConnectionString(DadosConexaoSistema dados)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dados.Servidor,
            InitialCatalog = dados.BancoDados,
            UserID = dados.UsuarioBanco,
            Password = dados.SenhaBanco,
            Encrypt = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = 30,
            ApplicationName = dados.NomeSistema
        };

        return builder.ConnectionString;
    }

    private static void ValidarDadosConexao(DadosConexaoSistema dados)
    {
        if (string.IsNullOrWhiteSpace(dados.Servidor))
            throw new ConexaoCentralizadaException($"Servidor do ambiente '{dados.Ambiente}' não informado para o sistema '{dados.NomeSistema}'.");

        if (string.IsNullOrWhiteSpace(dados.BancoDados))
            throw new ConexaoCentralizadaException($"Banco de dados do ambiente '{dados.Ambiente}' não informado para o sistema '{dados.NomeSistema}'.");

        if (string.IsNullOrWhiteSpace(dados.UsuarioBanco))
            throw new ConexaoCentralizadaException($"Usuário do banco do ambiente '{dados.Ambiente}' não informado para o sistema '{dados.NomeSistema}'.");

        if (string.IsNullOrWhiteSpace(dados.SenhaBanco))
            throw new ConexaoCentralizadaException($"Senha do banco do ambiente '{dados.Ambiente}' não informada para o sistema '{dados.NomeSistema}'.");
    }
}