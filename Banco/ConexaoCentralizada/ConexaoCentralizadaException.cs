namespace DllTeste.Banco.ConexaoCentralizada;

public sealed class ConexaoCentralizadaException : Exception
{
    public ConexaoCentralizadaException(string message)
        : base(message)
    {
    }

    public ConexaoCentralizadaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}