using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DllTeste.Banco.Services;

/// <summary>
/// Serviço base genérico para operações de manipulação de dados utilizando o Entity Framework Core.
/// Centraliza o controle do DbContext, tratamento de erros e fornece pontos de extensão para regras de negócio.
/// </summary>
/// <typeparam name="TContext">O tipo do contexto do banco de dados (DbContext).</typeparam>
/// <typeparam name="TEntidade">O tipo da classe/entidade mapeada no banco de dados.</typeparam>
public class ManipulacaoService<TContext, TEntidade>
    where TContext : DbContext
    where TEntidade : class
{
    protected readonly IDbContextFactory<TContext> _contextFactory;
    protected readonly ILogger? _logger;

    /// <summary>
    /// Obtém o nome da entidade que está sendo manipulada. Utilizado principalmente para logs de erro.
    /// </summary>
    protected virtual string NomeEntidade => typeof(TEntidade).Name;

    /// <summary>
    /// Obtém o nome do contexto do banco de dados. Utilizado principalmente para logs de erro.
    /// </summary>
    protected virtual string NomeContexto => typeof(TContext).Name;

    /// <summary>
    /// Inicializa uma nova instância do serviço base de manipulação de dados.
    /// </summary>
    /// <param name="contextFactory">A fábrica responsável por criar instâncias seguras e de curto escopo do DbContext.</param>
    /// <param name="logger">O serviço de log opcional para registrar falhas operacionais.</param>
    /// <exception cref="ArgumentNullException">Lançada caso a fábrica de contexto seja nula.</exception>
    public ManipulacaoService(
        IDbContextFactory<TContext> contextFactory,
        ILogger? logger = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger;
    }

    /// <summary>
    /// Insere uma nova entidade no banco de dados de forma assíncrona.
    /// </summary>
    /// <param name="entidade">A entidade a ser inserida.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A entidade inserida (frequentemente com o ID gerado pelo banco).</returns>
    public virtual async Task<TEntidade> InserirAsync(
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        if (entidade == null)
            throw new ArgumentNullException(nameof(entidade));

        return await ExecutarComTratamentoAsync(
            "inserir entidade",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                await AntesDeInserirAsync(context, entidade, cancellationToken);

                await context.Set<TEntidade>().AddAsync(entidade, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                await DepoisDeInserirAsync(context, entidade, cancellationToken);

                return entidade;
            });
    }

    /// <summary>
    /// Insere uma coleção de entidades no banco de dados em uma única operação.
    /// </summary>
    /// <param name="entidades">A lista de entidades a serem inseridas.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    public virtual async Task InserirVariosAsync(
        IEnumerable<TEntidade> entidades,
        CancellationToken cancellationToken = default)
    {
        if (entidades == null)
            return;

        var lista = entidades.ToList();

        if (lista.Count == 0)
            return;

        await ExecutarComTratamentoAsync(
            $"inserir {lista.Count} entidades",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                foreach (var entidade in lista)
                    await AntesDeInserirAsync(context, entidade, cancellationToken);

                await context.Set<TEntidade>().AddRangeAsync(lista, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                foreach (var entidade in lista)
                    await DepoisDeInserirAsync(context, entidade, cancellationToken);
            });
    }

    /// <summary>
    /// Atualiza integralmente o estado de uma entidade existente no banco de dados.
    /// </summary>
    /// <param name="entidade">A entidade contendo as modificações a serem persistidas.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a atualização foi bem-sucedida no banco, ou <c>false</c> caso contrário.</returns>
    public virtual async Task<bool> AtualizarAsync(
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        if (entidade == null)
            throw new ArgumentNullException(nameof(entidade));

        return await ExecutarComTratamentoAsync(
            "atualizar entidade completa",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                await AntesDeAtualizarAsync(context, entidade, cancellationToken);

                context.Attach(entidade);
                context.Entry(entidade).State = EntityState.Modified;

                var result = await context.SaveChangesAsync(cancellationToken);

                await DepoisDeAtualizarAsync(context, entidade, cancellationToken);

                return result > 0;
            });
    }

    /// <summary>
    /// Busca uma entidade pelo ID, aplica uma ação de modificação e a salva no banco de dados.
    /// </summary>
    /// <param name="id">O identificador único (chave primária) da entidade.</param>
    /// <param name="aplicarAlteracoes">Uma ação delegate que recebe a entidade carregada para que suas propriedades sejam alteradas.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a alteração foi aplicada e salva, <c>false</c> se a entidade não foi encontrada.</returns>
    public virtual async Task<bool> AtualizarCarregadoAsync(
        object id,
        Action<TEntidade> aplicarAlteracoes,
        CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        return await AtualizarCarregadoPorChavesAsync(
            new object?[] { id },
            aplicarAlteracoes,
            cancellationToken);
    }

    /// <summary>
    /// Busca uma entidade por chaves compostas, aplica uma ação de modificação e a salva no banco de dados.
    /// </summary>
    /// <param name="chaves">Um array contendo as chaves primárias que identificam o registro.</param>
    /// <param name="aplicarAlteracoes">Ação executada para alterar a entidade encontrada antes do save.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se atualizada; <c>false</c> se não encontrada.</returns>
    public virtual async Task<bool> AtualizarCarregadoPorChavesAsync(
        object?[] chaves,
        Action<TEntidade> aplicarAlteracoes,
        CancellationToken cancellationToken = default)
    {
        if (chaves == null || chaves.Length == 0)
            throw new ArgumentException("As chaves da entidade não foram informadas.", nameof(chaves));

        if (aplicarAlteracoes == null)
            throw new ArgumentNullException(nameof(aplicarAlteracoes));

        return await ExecutarComTratamentoAsync(
            "atualizar entidade carregada",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                var entidade = await context.Set<TEntidade>()
                    .FindAsync(chaves, cancellationToken);

                if (entidade == null)
                    return false;

                aplicarAlteracoes(entidade);

                await AntesDeAtualizarAsync(context, entidade, cancellationToken);

                var result = await context.SaveChangesAsync(cancellationToken);

                await DepoisDeAtualizarAsync(context, entidade, cancellationToken);

                return result > 0;
            });
    }

    /// <summary>
    /// Atualiza uma coleção de entidades no banco de dados.
    /// </summary>
    /// <param name="entidades">Lista de entidades modificadas a serem atualizadas.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    public virtual async Task AtualizarVariosAsync(
        IEnumerable<TEntidade> entidades,
        CancellationToken cancellationToken = default)
    {
        if (entidades == null)
            return;

        var lista = entidades.ToList();

        if (lista.Count == 0)
            return;

        await ExecutarComTratamentoAsync(
            $"atualizar {lista.Count} entidades",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                foreach (var entidade in lista)
                    await AntesDeAtualizarAsync(context, entidade, cancellationToken);

                context.Set<TEntidade>().UpdateRange(lista);
                await context.SaveChangesAsync(cancellationToken);

                foreach (var entidade in lista)
                    await DepoisDeAtualizarAsync(context, entidade, cancellationToken);
            });
    }

    /// <summary>
    /// Exclui um registro do banco de dados baseado no seu ID (chave simples).
    /// </summary>
    /// <param name="id">O ID do registro a ser excluído.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a exclusão ocorreu; <c>false</c> se não encontrado.</returns>
    public virtual async Task<bool> DeletarPorIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        return await DeletarPorChavesAsync(
            new object?[] { id },
            cancellationToken);
    }

    /// <summary>
    /// Exclui um registro do banco de dados baseado em uma chave composta.
    /// </summary>
    /// <param name="chaves">Um array representando as chaves primárias do registro.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a exclusão ocorreu; <c>false</c> se não encontrado.</returns>
    public virtual async Task<bool> DeletarPorChavesAsync(
        object?[] chaves,
        CancellationToken cancellationToken = default)
    {
        if (chaves == null || chaves.Length == 0)
            throw new ArgumentException("As chaves da entidade não foram informadas.", nameof(chaves));

        return await ExecutarComTratamentoAsync(
            "deletar entidade por chave",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                var entidade = await context.Set<TEntidade>()
                    .FindAsync(chaves, cancellationToken);

                if (entidade == null)
                    return false;

                await AntesDeDeletarAsync(context, entidade, cancellationToken);

                context.Set<TEntidade>().Remove(entidade);

                var result = await context.SaveChangesAsync(cancellationToken);

                await DepoisDeDeletarAsync(context, entidade, cancellationToken);

                return result > 0;
            });
    }

    /// <summary>
    /// Remove permanentemente uma coleção de registros do banco de dados.
    /// </summary>
    /// <param name="entidades">A lista de entidades a serem removidas.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    public virtual async Task DeletarVariosAsync(
        IEnumerable<TEntidade> entidades,
        CancellationToken cancellationToken = default)
    {
        if (entidades == null)
            return;

        var lista = entidades.ToList();

        if (lista.Count == 0)
            return;

        await ExecutarComTratamentoAsync(
            $"deletar {lista.Count} entidades",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                foreach (var entidade in lista)
                    await AntesDeDeletarAsync(context, entidade, cancellationToken);

                context.Set<TEntidade>().RemoveRange(lista);
                await context.SaveChangesAsync(cancellationToken);

                foreach (var entidade in lista)
                    await DepoisDeDeletarAsync(context, entidade, cancellationToken);
            });
    }

    /// <summary>
    /// Obtém todos os registros da tabela correspondente à entidade de forma irrestrita.
    /// </summary>
    /// <param name="asNoTracking">Define se as entidades retornadas não devem ser rastreadas pelo contexto (recomendado para consultas de leitura).</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Uma lista contendo todos os registros encontrados.</returns>
    public virtual async Task<List<TEntidade>> GetAllAsync(
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        return await ListarAsync(
            filtro: null,
            ordenarPor: null,
            prepararQuery: null,
            skip: null,
            take: null,
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Busca uma única entidade pelo seu identificador principal.
    /// </summary>
    /// <param name="id">O valor da chave primária.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A entidade encontrada, ou <c>null</c> se não existir.</returns>
    public virtual async Task<TEntidade?> GetPorIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        return await GetPorChavesAsync(
            new object?[] { id },
            cancellationToken);
    }

    /// <summary>
    /// Busca uma única entidade por suas chaves compostas no banco de dados.
    /// </summary>
    /// <param name="chaves">Valores correspondentes à chave composta.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A entidade correspondente ou nulo.</returns>
    public virtual async Task<TEntidade?> GetPorChavesAsync(
        object?[] chaves,
        CancellationToken cancellationToken = default)
    {
        if (chaves == null || chaves.Length == 0)
            throw new ArgumentException("As chaves da entidade não foram informadas.", nameof(chaves));

        return await ExecutarComTratamentoAsync(
            "buscar entidade por chave",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                return await context.Set<TEntidade>()
                    .FindAsync(chaves, cancellationToken);
            });
    }

    /// <summary>
    /// Realiza uma busca customizada utilizando filtros lógicos, ordenação e regras de paginação via LINQ.
    /// </summary>
    /// <param name="filtro">A expressão lambda contendo a lógica da cláusula WHERE.</param>
    /// <param name="ordenarPor">Função contendo as cláusulas de ordenação (OrderBy / OrderByDescending).</param>
    /// <param name="prepararQuery">Função utilizada para customizar o IQueryable (ex: aplicar .Include() para tabelas relacionadas).</param>
    /// <param name="skip">A quantidade de registros a serem pulados (offset).</param>
    /// <param name="take">A quantidade de registros limitados ao retorno.</param>
    /// <param name="asNoTracking">Define se o rastreamento do EF será desativado, o que aumenta a performance (true por padrão).</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A lista processada contendo os registros encontrados.</returns>
    public virtual async Task<List<TEntidade>> ListarAsync(
        Expression<Func<TEntidade, bool>>? filtro = null,
        Func<IQueryable<TEntidade>, IOrderedQueryable<TEntidade>>? ordenarPor = null,
        Func<IQueryable<TEntidade>, IQueryable<TEntidade>>? prepararQuery = null,
        int? skip = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        return await ExecutarComTratamentoAsync(
            "listar entidades",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>();

                if (asNoTracking)
                    query = query.AsNoTracking();

                if (prepararQuery != null)
                    query = prepararQuery(query);

                if (filtro != null)
                    query = query.Where(filtro);

                if (ordenarPor != null)
                    query = ordenarPor(query);

                if (skip.HasValue && skip.Value > 0)
                    query = query.Skip(skip.Value);

                if (take.HasValue && take.Value > 0)
                    query = query.Take(take.Value);

                return await query.ToListAsync(cancellationToken);
            });
    }

    /// <summary>
    /// Realiza a busca de forma otimizada para paginação em telas, calculando dinamicamente offsets e fornecendo o total de registros.
    /// </summary>
    /// <param name="pagina">O número da página solicitada (inicia em 1).</param>
    /// <param name="tamanhoPagina">A quantidade de itens por página.</param>
    /// <param name="filtro">A expressão lambda para filtrar os dados.</param>
    /// <param name="ordenarPor">Regra de ordenação dos resultados.</param>
    /// <param name="prepararQuery">Função para customizar propriedades ou Inclusões.</param>
    /// <param name="asNoTracking">Se desabilita o acompanhamento das entidades na memória.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>Um objeto de ResultadoPaginado encapsulando a lista e metadados de paginação.</returns>
    public virtual async Task<ResultadoPaginado<TEntidade>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        Expression<Func<TEntidade, bool>>? filtro = null,
        Func<IQueryable<TEntidade>, IOrderedQueryable<TEntidade>>? ordenarPor = null,
        Func<IQueryable<TEntidade>, IQueryable<TEntidade>>? prepararQuery = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        if (pagina <= 0)
            pagina = 1;

        if (tamanhoPagina <= 0)
            tamanhoPagina = 10;

        return await ExecutarComTratamentoAsync(
            "listar entidades paginadas",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>();

                if (asNoTracking)
                    query = query.AsNoTracking();

                if (prepararQuery != null)
                    query = prepararQuery(query);

                if (filtro != null)
                    query = query.Where(filtro);

                var total = await query.CountAsync(cancellationToken);

                if (ordenarPor != null)
                    query = ordenarPor(query);

                var skip = (pagina - 1) * tamanhoPagina;

                var itens = await query
                    .Skip(skip)
                    .Take(tamanhoPagina)
                    .ToListAsync(cancellationToken);

                return new ResultadoPaginado<TEntidade>
                {
                    Itens = itens,
                    TotalRegistros = total,
                    PaginaAtual = pagina,
                    TamanhoPagina = tamanhoPagina
                };
            });
    }

    /// <summary>
    /// Retorna o primeiro registro que satisfaça a condição especificada, ou o valor padrão (nulo) caso nada seja encontrado.
    /// </summary>
    /// <param name="filtro">A condição WHERE essencial para a busca.</param>
    /// <param name="prepararQuery">Funções adicionais a aplicar no objeto de consulta.</param>
    /// <param name="asNoTracking">Se o rastreamento deve ser desligado.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>O primeiro objeto TEntidade encontrado, ou nulo.</returns>
    public virtual async Task<TEntidade?> PrimeiroOuPadraoAsync(
        Expression<Func<TEntidade, bool>> filtro,
        Func<IQueryable<TEntidade>, IQueryable<TEntidade>>? prepararQuery = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        if (filtro == null)
            throw new ArgumentNullException(nameof(filtro));

        return await ExecutarComTratamentoAsync(
            "buscar primeira entidade por filtro",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>();

                if (asNoTracking)
                    query = query.AsNoTracking();

                if (prepararQuery != null)
                    query = prepararQuery(query);

                return await query.FirstOrDefaultAsync(filtro, cancellationToken);
            });
    }

    /// <summary>
    /// Verifica a existência de pelo menos um registro no banco que atenda ao critério estabelecido (mais performático que o .Count()).
    /// </summary>
    /// <param name="filtro">A cláusula a ser validada.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns><c>true</c> se ao menos um registro existe; <c>false</c> caso nenhum.</returns>
    public virtual async Task<bool> ExisteAsync(
        Expression<Func<TEntidade, bool>> filtro,
        CancellationToken cancellationToken = default)
    {
        if (filtro == null)
            throw new ArgumentNullException(nameof(filtro));

        return await ExecutarComTratamentoAsync(
            "verificar existência de entidade",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                return await context.Set<TEntidade>()
                    .AsNoTracking()
                    .AnyAsync(filtro, cancellationToken);
            });
    }

    /// <summary>
    /// Retorna a quantidade total de registros da tabela ou os que correspondem a uma condição de filtro.
    /// </summary>
    /// <param name="filtro">Opcional. Uma expressão condicional para restringir a contagem.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>O número em formato inteiro do total de linhas no banco.</returns>
    public virtual async Task<int> ContarAsync(
        Expression<Func<TEntidade, bool>>? filtro = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecutarComTratamentoAsync(
            "contar entidades",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>()
                    .AsNoTracking();

                if (filtro != null)
                    query = query.Where(filtro);

                return await query.CountAsync(cancellationToken);
            });
    }

    /// <summary>
    /// Permite a execução de projeções e transformações diretas no banco de dados, mapeando TEntidade para um DTO ou tipo customizado (TResult).
    /// </summary>
    /// <typeparam name="TResult">O tipo desejado como saída da consulta.</typeparam>
    /// <param name="consulta">A função que define o LINQ e o ".Select()" (projeção).</param>
    /// <param name="asNoTracking">Recomendado manter true ao buscar dados transformados.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A lista transformada do tipo TResult.</returns>
    public virtual async Task<List<TResult>> ConsultarAsync<TResult>(
        Func<IQueryable<TEntidade>, IQueryable<TResult>> consulta,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        if (consulta == null)
            throw new ArgumentNullException(nameof(consulta));

        return await ExecutarComTratamentoAsync(
            "executar consulta projetada",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>();

                if (asNoTracking)
                    query = query.AsNoTracking();

                return await consulta(query).ToListAsync(cancellationToken);
            });
    }

    /// <summary>
    /// Permite executar uma projeção, retornando apenas o primeiro registro correspondente mapeado para um tipo específico.
    /// </summary>
    /// <typeparam name="TResult">O tipo de objeto resultante desejado.</typeparam>
    /// <param name="consulta">A função mapeadora do LINQ.</param>
    /// <param name="asNoTracking">Habilita ou desabilita o cache de contexto.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>O registro único convertido, ou nulo.</returns>
    public virtual async Task<TResult?> PrimeiroOuPadraoAsync<TResult>(
        Func<IQueryable<TEntidade>, IQueryable<TResult>> consulta,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        if (consulta == null)
            throw new ArgumentNullException(nameof(consulta));

        return await ExecutarComTratamentoAsync(
            "executar consulta projetada com primeiro registro",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);

                IQueryable<TEntidade> query = context.Set<TEntidade>();

                if (asNoTracking)
                    query = query.AsNoTracking();

                return await consulta(query).FirstOrDefaultAsync(cancellationToken);
            });
    }

    /// <summary>
    /// Fornece acesso direto ao DbContext para executar uma operação livre e que necessita de retorno customizado.
    /// </summary>
    /// <typeparam name="TResult">O tipo de dado gerado no final da operação.</typeparam>
    /// <param name="operacao">Delegate fornecendo o DbContext como parâmetro principal da execução.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>O resultado customizado obtido pela função.</returns>
    public virtual async Task<TResult> ExecutarNoContextoAsync<TResult>(
        Func<TContext, Task<TResult>> operacao,
        CancellationToken cancellationToken = default)
    {
        if (operacao == null)
            throw new ArgumentNullException(nameof(operacao));

        return await ExecutarComTratamentoAsync(
            "executar operação personalizada no contexto",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);
                return await operacao(context);
            });
    }

    /// <summary>
    /// Fornece acesso direto ao DbContext para executar um bloco de código assíncrono complexo sem retorno (void/Task).
    /// </summary>
    /// <param name="operacao">A ação envolvendo manipulação com o context sendo exposto.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    public virtual async Task ExecutarNoContextoAsync(
        Func<TContext, Task> operacao,
        CancellationToken cancellationToken = default)
    {
        if (operacao == null)
            throw new ArgumentNullException(nameof(operacao));

        await ExecutarComTratamentoAsync(
            "executar operação personalizada no contexto",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);
                await operacao(context);
            });
    }

    /// <summary>
    /// Executa um conjunto customizado de comandos no Entity Framework embalado de forma atômica utilizando transação de banco (BeginTransaction).
    /// </summary>
    /// <typeparam name="TResult">O tipo resultante gerado ao fim da transação.</typeparam>
    /// <param name="operacao">O delegate contendo a rotina a ser processada na transação.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>O resultado e efetua o CommitAsync automaticamente; em falhas processa um RollbackAsync.</returns>
    public virtual async Task<TResult> ExecutarEmTransacaoAsync<TResult>(
        Func<TContext, Task<TResult>> operacao,
        CancellationToken cancellationToken = default)
    {
        if (operacao == null)
            throw new ArgumentNullException(nameof(operacao));

        return await ExecutarComTratamentoAsync(
            "executar operação em transação",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var resultado = await operacao(context);

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return resultado;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
    }

    /// <summary>
    /// Executa uma rotina sem retorno forçando a proteção de transação no banco (BeginTransaction).
    /// </summary>
    /// <param name="operacao">Delegate que fornece o Context, garantindo o processo transacional completo de todos os comandos executados.</param>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    public virtual async Task ExecutarEmTransacaoAsync(
        Func<TContext, Task> operacao,
        CancellationToken cancellationToken = default)
    {
        if (operacao == null)
            throw new ArgumentNullException(nameof(operacao));

        await ExecutarComTratamentoAsync(
            "executar operação em transação",
            async () =>
            {
                await using var context = await CriarContextoAsync(cancellationToken);
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await operacao(context);

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
    }

    /// <summary>
    /// Instancia e cria o objeto TContext apropriado utilizando a Fábrica definida na injeção de dependência (Factory Pattern).
    /// </summary>
    /// <param name="cancellationToken">O token para cancelamento da operação.</param>
    /// <returns>A nova instância construída do DbContext pronto para operação unitária.</returns>
    protected virtual async Task<TContext> CriarContextoAsync(
        CancellationToken cancellationToken = default)
    {
        return await _contextFactory.CreateDbContextAsync(cancellationToken);
    }

    /// <summary>
    /// Gancho (Hook) chamado pelo serviço antes que a chamada de Inserir (AddAsync) ocorra no Contexto. Ideal para popular datas de auditoria ou IDs secundários em aplicações clientes.
    /// </summary>
    protected virtual Task AntesDeInserirAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gancho (Hook) chamado pelo serviço logo após as mudanças serem comitadas no banco de dados via Inserir (AddAsync).
    /// </summary>
    protected virtual Task DepoisDeInserirAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gancho (Hook) chamado pelo serviço antes de aplicar uma flag de Atualização (Modified) na entidade no Contexto EF.
    /// </summary>
    protected virtual Task AntesDeAtualizarAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gancho (Hook) chamado após o SaveChanges da rotina de Update (Atualizar) já ter sido executado e aprovado pelo DB.
    /// </summary>
    protected virtual Task DepoisDeAtualizarAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gancho (Hook) interceptando a entidade antes de ser passada à rotina de exclusão (Remove) no EF Core. Útil para verificar lógicas de exclusão lógica ou validar pendências.
    /// </summary>
    protected virtual Task AntesDeDeletarAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gancho (Hook) acionado quando a entidade foi formalmente apagada da base de dados e o commmit ocorreu com sucesso.
    /// </summary>
    protected virtual Task DepoisDeDeletarAsync(
        TContext context,
        TEntidade entidade,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envelope de segurança interno para processos (void/Task) capturando, registrando via ILogger e empacotando falhas em falhas tratáveis do serviço.
    /// </summary>
    /// <param name="operacao">O nome da operação sendo monitorada.</param>
    /// <param name="acao">O delegate assíncrono para ser disparado dentro do try/catch.</param>
    protected async Task ExecutarComTratamentoAsync(
        string operacao,
        Func<Task> acao)
    {
        try
        {
            await acao();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ManipulacaoServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw CriarErro(ex, operacao);
        }
    }

    /// <summary>
    /// Envelope de segurança interno para processos geradores de valor (TResult) tratando com Log centralizado todo tipo de falhas subjacentes (DbUpdateException, SqlException, etc).
    /// </summary>
    /// <typeparam name="TResult">O tipo de volta estipulado pela função principal.</typeparam>
    /// <param name="operacao">A descrição resumida do processo para constar no Logger.</param>
    /// <param name="acao">Delegate contendo a execução que preenche o retorno.</param>
    /// <returns>Valor original validado caso as exceções não ocorram.</returns>
    protected async Task<TResult> ExecutarComTratamentoAsync<TResult>(
        string operacao,
        Func<Task<TResult>> acao)
    {
        try
        {
            return await acao();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ManipulacaoServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw CriarErro(ex, operacao);
        }
    }

    /// <summary>
    /// Unifica o registro (Log) da exceção e modelagem de uma custom Exception (ManipulacaoServiceException) repassando o StackTrace completo.
    /// </summary>
    /// <param name="ex">A exceção capturada (InnerException).</param>
    /// <param name="operacao">O identificador da operação originária da falha.</param>
    /// <returns>A exception empacotada pronta para ser lançada à aplicação Blazor.</returns>
    protected virtual ManipulacaoServiceException CriarErro(
        Exception ex,
        string operacao)
    {
        var mensagem = $"Erro ao {operacao} da entidade '{NomeEntidade}' no contexto '{NomeContexto}'.";

        _logger?.LogError(
            ex,
            "Erro no ManipulacaoService. Contexto={Contexto}; Entidade={Entidade}; Operacao={Operacao}",
            NomeContexto,
            NomeEntidade,
            operacao);

        return new ManipulacaoServiceException(mensagem, ex);
    }
}