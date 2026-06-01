namespace DllTeste.Banco.Services;

public sealed class ResultadoPaginado<T>
{
    public List<T> Itens { get; set; } = new();
    public int TotalRegistros { get; set; }
    public int PaginaAtual { get; set; }
    public int TamanhoPagina { get; set; }

    public int TotalPaginas =>
        TamanhoPagina <= 0
            ? 0
            : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
}