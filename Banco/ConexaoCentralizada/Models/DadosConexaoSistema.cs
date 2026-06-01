namespace DllTeste.Banco.ConexaoCentralizada.Models
{
    public sealed class DadosConexaoSistema
    {
        public string NomeSistema { get; set; } = string.Empty;
        public string Ambiente { get; set; } = string.Empty;

        public string Servidor { get; set; } = string.Empty;
        public string BancoDados { get; set; } = string.Empty;
        public string UsuarioBanco { get; set; } = string.Empty;
        public string SenhaBanco { get; set; } = string.Empty;

        public string ConnectionString { get; set; } = string.Empty;
    }
}
