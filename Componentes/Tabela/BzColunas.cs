using Componentes;
using Microsoft.AspNetCore.Components;

namespace DllTeste.Componentes.Tabela;

public enum BzTamanho { Sm, Md, Lg }
public enum BzAlinhamento { Esquerda, Centro, Direita }

/// <summary>Base de coluna. Não renderiza nada: registra-se no grid e carrega config.</summary>
public abstract class BzColunaBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] internal BzGrid<TItem>? Grid { get; set; }

    [Parameter] public string Titulo { get; set; } = string.Empty;
    /// <summary>Classe de ícone antes do título (ex.: "fas fa-calendar").</summary>
    [Parameter] public string? Icone { get; set; }
    /// <summary>Largura CSS (ex.: "25%", "120px"). Nulo = auto.</summary>
    [Parameter] public string? Largura { get; set; }
    [Parameter] public BzAlinhamento Alinhamento { get; set; } = BzAlinhamento.Esquerda;
    [Parameter] public bool Ordenavel { get; set; }
    /// <summary>Chave de ordenação. Se nulo e Ordenavel, usa o valor padrão da coluna.</summary>
    [Parameter] public Func<TItem, object?>? OrdenarPor { get; set; }
    [Parameter] public bool Pesquisavel { get; set; } = true;
    [Parameter] public string? CssClasse { get; set; }

    protected override void OnInitialized() => Grid?.AddColuna(this);
    public void Dispose() => Grid?.RemoveColuna(this);

    public abstract string ObterTextoBusca(TItem item);
    internal virtual Func<TItem, object?>? ChaveOrdenacao => OrdenarPor;
}

/// <summary>Coluna de dados (texto). Formatação, truncamento e template livre.</summary>
public class BzColuna<TItem> : BzColunaBase<TItem>
{
    [Parameter] public Func<TItem, object?>? Valor { get; set; }
    [Parameter] public Func<object?, string>? Formatar { get; set; }
    [Parameter] public int? MaxCaracteres { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    internal override Func<TItem, object?>? ChaveOrdenacao => OrdenarPor ?? Valor;

    public string TextoFormatado(TItem item)
    {
        var bruto = Valor?.Invoke(item);
        var texto = Formatar != null ? Formatar(bruto) : bruto?.ToString() ?? string.Empty;
        if (MaxCaracteres.HasValue && texto.Length > MaxCaracteres.Value)
            texto = texto.Substring(0, MaxCaracteres.Value) + "...";
        return texto;
    }

    public override string ObterTextoBusca(TItem item)
        => Valor?.Invoke(item)?.ToString() ?? string.Empty;
}

/// <summary>Coluna de imagem. Trata URL vazia e erro de carregamento via fallback.</summary>
public class BzColunaImagem<TItem> : BzColunaBase<TItem>
{
    [Parameter] public Func<TItem, string?>? Valor { get; set; }
    [Parameter]
    public string Fallback { get; set; } =
        "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80'><rect width='100%25' height='100%25' fill='%23e5e7eb'/><text x='50%25' y='50%25' font-size='10' fill='%239ca3af' text-anchor='middle' dominant-baseline='middle'>sem imagem</text></svg>";
    [Parameter] public string LarguraImagem { get; set; } = "56px";
    [Parameter] public string AlturaImagem { get; set; } = "56px";
    /// <summary>contain | cover. Padrão: cover.</summary>
    [Parameter] public string ObjectFit { get; set; } = "cover";
    [Parameter] public string Raio { get; set; } = "6px";

    public string ResolverUrl(TItem item)
    {
        var url = Valor?.Invoke(item);
        return string.IsNullOrWhiteSpace(url) ? Fallback : url!;
    }

    public override string ObterTextoBusca(TItem item) => string.Empty;
}

/// <summary>Coluna de ações. O conteúdo (botões etc.) recebe o item como contexto.</summary>
public class BzColunaAcoes<TItem> : BzColunaBase<TItem>
{
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    public BzColunaAcoes()
    {
        Pesquisavel = false;
        Ordenavel = false;
    }

    public override string ObterTextoBusca(TItem item) => string.Empty;
}