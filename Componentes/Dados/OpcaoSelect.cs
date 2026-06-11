namespace DllTeste.Componentes.Dados;

/// <summary>
/// Item de opcao para SelectBusca e MultiSelect.
/// O Valor e string para cobrir qualquer tipo de chave (Id, Guid, codigo);
/// converta conforme necessario no seu codigo.
/// </summary>
public sealed class OpcaoSelect
{
    public OpcaoSelect() { }

    public OpcaoSelect(string valor, string texto)
    {
        Valor = valor;
        Texto = texto;
    }

    /// <summary>Valor unico da opcao (chave).</summary>
    public string Valor { get; init; } = string.Empty;

    /// <summary>Texto exibido.</summary>
    public string Texto { get; init; } = string.Empty;

    /// <summary>Icone opcional (Bootstrap Icons), ex: "bi-flag".</summary>
    public string? Icone { get; init; }

    /// <summary>Impede a selecao desta opcao.</summary>
    public bool Desabilitada { get; init; }
}
