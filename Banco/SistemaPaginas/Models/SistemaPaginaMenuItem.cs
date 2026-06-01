namespace DllTeste.Banco.SistemaPaginas.Models;

public sealed class SistemaPaginaMenuItem
{
    public SistemaPagina Pagina { get; set; } = new();

    public List<SistemaPaginaMenuItem> Filhos { get; set; } = new();

    public bool TemFilhos => Filhos.Count > 0;
}