# Componentes Padrão — Lote 1 (Ações, Layout e Feedback)

Blazor Server (.NET, render mode interativo). Paleta escura `#0b1120` / `#0f172a` / `#0b1225`, Bootstrap Icons.

Componentes inclusos:

- **Botao** — botão com variantes, ícone, estados de loading e disabled.
- **Cartao** — card/painel com cabeçalho, corpo, rodapé e área de ações.
- **Modal** — modal genérico com `@bind-Visivel`, tamanhos, fechar no fundo/ESC.
- **Toast** — sistema de notificações (serviço + container) com auto-dismiss.
- **Confirmacao** — diálogo de confirmação que retorna `Task<bool>`.

---

## 1. Instalação

1. Copie a pasta `Padrao/` para dentro do seu projeto (ex.: `Components/Padrao/`).

2. **Troque o namespace.** Todos os arquivos usam `MeuApp.Componentes.Padrao`.
   Substitua `MeuApp` pelo namespace raiz do seu projeto. No Linux/macOS:

   ```bash
   grep -rl "MeuApp.Componentes.Padrao" Padrao/ \
     | xargs sed -i 's/MeuApp\.Componentes\.Padrao/SeuProjeto.Componentes.Padrao/g'
   ```

   No Windows (PowerShell):

   ```powershell
   Get-ChildItem -Recurse Padrao\ | ForEach-Object {
     (Get-Content $_.FullName) -replace 'MeuApp\.Componentes\.Padrao','SeuProjeto.Componentes.Padrao' |
       Set-Content $_.FullName
   }
   ```

3. No `App.razor` (ou `_Host.cshtml`), referencie o CSS de tokens **e** o bundle
   de CSS isolado, dentro do `<head>`:

   ```html
   <link rel="stylesheet" href="Components/Padrao/padrao.css" />
   <link rel="stylesheet" href="SeuProjeto.styles.css" />
   ```

   > `padrao.css` traz as variáveis CSS globais. O `SeuProjeto.styles.css`
   > (gerado pelo build) já reúne todos os `*.razor.css` isolados.

4. No `_Imports.razor` global, adicione:

   ```razor
   @using SeuProjeto.Componentes.Padrao
   ```

---

## 2. Registro dos serviços (`Program.cs`)

No Blazor Server os serviços são **Scoped** (um por circuito/usuário):

```csharp
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IConfirmacaoService, ConfirmacaoService>();
```

---

## 3. Host visual no layout (`MainLayout.razor`)

Coloque os dois "hosts" uma única vez, no fim do layout principal:

```razor
@inherits LayoutComponentBase

<div class="page">
    @* ... seu layout ... *@
    <main>
        @Body
    </main>
</div>

@* Hosts dos componentes padrão *@
<ToastContainer />
<ConfirmacaoDialog />
```

---

## 4. Exemplos de uso

### Botao

```razor
<Botao Texto="Salvar" Icone="bi-save" OnClick="Salvar" />
<Botao Texto="Excluir" Variante="BotaoVariante.Perigo" Icone="bi-trash" />
<Botao Texto="Processando" Carregando="true" />
<Botao Variante="BotaoVariante.Secundario" Texto="Cancelar" Tamanho="sm" />
<Botao Bloco="true" Texto="Entrar" TipoHtml="submit" />
```

### Cartao

```razor
<Cartao Titulo="Jogadores" Icone="bi-people">
    <AcoesCabecalho>
        <Botao Variante="BotaoVariante.Fantasma" Icone="bi-plus-lg" Tamanho="sm" Texto="Novo" />
    </AcoesCabecalho>

    Conteúdo do cartão aqui.

    <Rodape>
        <Botao Variante="BotaoVariante.Secundario" Texto="Fechar" />
        <Botao Texto="Confirmar" />
    </Rodape>
</Cartao>
```

### Modal

```razor
<Botao Texto="Abrir" OnClick="() => _aberto = true" />

<Modal @bind-Visivel="_aberto" Titulo="Detalhes" Icone="bi-info-circle" Tamanho="lg">
    Conteúdo do modal.

    <Rodape>
        <Botao Variante="BotaoVariante.Secundario" Texto="Fechar" OnClick="() => _aberto = false" />
        <Botao Texto="Salvar" OnClick="Salvar" />
    </Rodape>
</Modal>

@code {
    private bool _aberto;
    private void Salvar() => _aberto = false;
}
```

### Toast

```razor
@inject IToastService Toasts

<Botao Texto="Sucesso" OnClick="() => Toasts.Sucesso(\"Registro salvo!\")" />
<Botao Texto="Erro" OnClick="() => Toasts.Erro(\"Falha ao salvar\", \"Atenção\")" />
```

```csharp
Toasts.Aviso("Sessão expira em 5 minutos.");
Toasts.Info("Sincronização concluída.", duracaoMs: 3000);
```

### Confirmação

```razor
@inject IConfirmacaoService Confirmacao
@inject IToastService Toasts

<Botao Texto="Excluir" Variante="BotaoVariante.Perigo" OnClick="Excluir" />

@code {
    private async Task Excluir()
    {
        var ok = await Confirmacao.ConfirmarAsync(new ConfirmacaoOpcoes
        {
            Titulo = "Excluir jogador",
            Mensagem = "Esta ação não pode ser desfeita. Deseja continuar?",
            TextoConfirmar = "Excluir",
            Perigosa = true,
            Icone = "bi-exclamation-triangle"
        });

        if (ok)
        {
            // ... excluir ...
            Toasts.Sucesso("Jogador excluído.");
        }
    }
}
```

Versão curta:

```csharp
if (await Confirmacao.ConfirmarAsync("Sair", "Deseja sair sem salvar?"))
{
    // ...
}
```

---

## 5. Componentes de dados (lote 2)

Ficam na subpasta `Dados/`. Nenhuma dependência de JS.

### Tabela / Grid

Genérica (`TItem`), com **filtro em tempo real** (filtra conforme digita),
ordenação por coluna, paginação e seleção de linhas. Use `[CascadingTypeParameter]`,
então o `TItem` das colunas é inferido do `<Tabela>`.

```razor
<Tabela TItem="Jogador"
        Itens="_jogadores"
        Selecionavel="true"
        ItensPorPagina="10"
        PlaceholderBusca="Buscar jogador..."
        OnSelecaoMudou="AoSelecionar">
    <ColunasDef>
        <Coluna Titulo="Nome" Valor="j => j.Nome" />
        <Coluna Titulo="Cor" Valor="j => j.Cor" Largura="120px" />
        <Coluna Titulo="Saldo" Valor="j => j.Saldo" Classe="text-end" Context="j">
            R$ @j.Saldo.ToString("N0")
        </Coluna>
        <Coluna Titulo="Ações" Ordenavel="false" Context="j">
            <Botao Variante="BotaoVariante.Fantasma" Icone="bi-pencil" Tamanho="sm" />
        </Coluna>
    </ColunasDef>
</Tabela>

@code {
    private List<Jogador> _jogadores = new();
    private void AoSelecionar(IReadOnlyList<Jogador> sel) { /* ... */ }
}
```

Notas: `Valor` é obrigatório para a coluna entrar na busca e na ordenação; sem ele,
a coluna é só de exibição (ótimo para a coluna de ações). Clicar no cabeçalho alterna
crescente → decrescente → sem ordenação. `ItensPorPagina="0"` desativa a paginação.

### SelectBusca (single, estilo select2)

```razor
<SelectBusca Opcoes="_opcoes"
             @bind-Valor="_idSelecionado"
             Placeholder="Selecione um tabuleiro"
             PlaceholderBusca="Filtrar..." />

@code {
    private string? _idSelecionado;
    private List<OpcaoSelect> _opcoes = new()
    {
        new("1", "Padrão Diagonal 70") { Icone = "bi-grid-3x3" },
        new("2", "Clássico")
    };
}
```

### MultiSelect

```razor
<MultiSelect Opcoes="_opcoes"
             @bind-Valores="_selecionados"
             MaxChipsVisiveis="3"
             Placeholder="Selecione os modos" />

@code {
    private List<string> _selecionados = new();
}
```

### CampoData

Usa o input nativo com `color-scheme: dark`, então o seletor abre já no tema escuro
(incluindo o picker nativo do Android).

```razor
<CampoData @bind-Valor="_data" Rotulo="Data de início" />
<CampoData @bind-Valor="_inicio" Tipo="datetime-local" Rotulo="Início" />
<CampoData @bind-Valor="_hora" Tipo="time" Rotulo="Horário" />

@code {
    private DateTime? _data;
    private DateTime? _inicio;
    private DateTime? _hora;
}
```

---

## Observações técnicas

- `ToastService` usa `System.Timers.Timer` para auto-dismiss; o `ToastContainer`
  faz `InvokeAsync(StateHasChanged)` porque o `Elapsed` roda em outra thread.
- `Modal` adiciona/remove a classe `pd-modal-aberto` no `<body>` para travar o
  scroll de fundo (regra global em `padrao.css`).
- `ConfirmacaoService` usa `TaskCompletionSource<bool>`, então você consegue
  `await` direto no fluxo da ação.
- Todas as variantes seguem `switch/case` com `break`, sem switch expressions
  nos pontos de mapeamento de classe/ícone.
