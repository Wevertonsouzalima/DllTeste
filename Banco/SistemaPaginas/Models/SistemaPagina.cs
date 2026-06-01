using DllTeste.Banco.SistemaPaginas.Enums;

namespace DllTeste.Banco.SistemaPaginas.Models;

public sealed class SistemaPagina
{
    public int IdPagina { get; set; }
    public int IdSistema { get; set; }
    public int? IdPaginaPai { get; set; }

    public string Chave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public string? Rota { get; set; }
    public string? Icone { get; set; }

    public int Ordem { get; set; }

    public TipoPaginaSistema TipoPagina { get; set; }
    public StatusPaginaSistema StatusPagina { get; set; }

    public bool ExibirNoMenu { get; set; }
    public bool AbrirNovaAba { get; set; }

    public string? Permissao { get; set; }
    public string? MensagemManutencao { get; set; }

    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public bool EhGrupo => TipoPagina == TipoPaginaSistema.Grupo;
    public bool EhPaginaMenu => TipoPagina == TipoPaginaSistema.PaginaMenu;
    public bool EhRotaInterna => TipoPagina == TipoPaginaSistema.RotaInterna;
    public bool EhSeparador => TipoPagina == TipoPaginaSistema.Separador;
    public bool EhLinkExterno => TipoPagina == TipoPaginaSistema.LinkExterno;

    public bool EstaAtiva => StatusPagina == StatusPaginaSistema.Ativa;
    public bool EstaInativa => StatusPagina == StatusPaginaSistema.Inativa;
    public bool EstaEmManutencao => StatusPagina == StatusPaginaSistema.Manutencao;
}