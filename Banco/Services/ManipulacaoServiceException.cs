namespace DllTeste.Banco.Services;

public sealed class ManipulacaoServiceException : Exception
{
    public ManipulacaoServiceException(string message)
        : base(message)
    {
    }

    public ManipulacaoServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}