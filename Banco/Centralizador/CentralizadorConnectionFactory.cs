using Microsoft.Data.SqlClient;
using DllTeste.Banco.ConexaoCentralizada.Enums;

namespace DllTeste.Banco.Centralizador;

public sealed class CentralizadorConnectionFactory
{

    private const string ConnectionStringDev =
        "Server=192.168.15.4,1433;Database=Db_Centralizador;User Id=sa;Password=95652867;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";

    private const string ConnectionStringHom =
        "Server=192.168.15.4,1433;Database=Db_Centralizador;User Id=sa;Password=95652867;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";

    private const string ConnectionStringProd =
        "Server=192.168.15.4,1433;Database=Db_Centralizador;User Id=sa;Password=95652867;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";

    /// <summary>
    /// Cria uma conexão com o centralizador do ambiente informado.
    /// </summary>
    public SqlConnection CriarConexao(AmbienteSistema ambiente= AmbienteSistema.Prod)
    {
        return new SqlConnection(ObterConnectionStringCentralizador(ambiente));
    }

    /// <summary>
    /// Retorna a connection string do centralizador conforme o ambiente.
    /// </summary>
    public string ObterConnectionStringCentralizador(AmbienteSistema ambiente)
    {
        switch (ambiente)
        {
            case AmbienteSistema.Dev:
                return ConnectionStringDev;

            case AmbienteSistema.Hom:
                return ConnectionStringHom;

            case AmbienteSistema.Prod:
                return ConnectionStringProd;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(ambiente),
                    $"Ambiente '{ambiente}' não suportado pelo centralizador.");
        }
    }
}