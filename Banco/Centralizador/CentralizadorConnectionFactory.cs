using Microsoft.Data.SqlClient;

namespace DllTeste.Banco.Centralizador;

public sealed class CentralizadorConnectionFactory
{
    private const string ConnectionStringCentralizador =
        "Server=192.168.15.4,1433;Database=Db_Centralizador;User Id=sa;Password=95652867;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";

    public SqlConnection CriarConexao()
    {
        return new SqlConnection(ConnectionStringCentralizador);
    }

    public string ObterConnectionStringCentralizador()
    {
        return ConnectionStringCentralizador;
    }
}