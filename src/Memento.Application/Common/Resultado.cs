namespace Memento.Application.Common;

public enum TipoErro
{
    Validacao,
    NaoAutenticado,
    Proibido,
    NaoEncontrado,
    Conflito
}

public sealed record Erro(
    TipoErro Tipo,
    string Codigo,
    string Mensagem,
    IReadOnlyDictionary<string, string[]>? Detalhes = null)
{
    public static Erro Validacao(IReadOnlyDictionary<string, string[]> detalhes)
        => new(TipoErro.Validacao, "validacao", "Um ou mais campos são inválidos.", detalhes);

    public static Erro Validacao(string campo, string mensagem)
        => Validacao(new Dictionary<string, string[]> { [campo] = [mensagem] });

    public static Erro NaoAutenticado(string mensagem = "Não autenticado.")
        => new(TipoErro.NaoAutenticado, "nao_autenticado", mensagem);

    public static Erro Proibido(string mensagem = "Você não tem permissão para esta ação.")
        => new(TipoErro.Proibido, "proibido", mensagem);

    public static Erro NaoEncontrado(string mensagem)
        => new(TipoErro.NaoEncontrado, "nao_encontrado", mensagem);

    public static Erro Conflito(string mensagem)
        => new(TipoErro.Conflito, "conflito", mensagem);
}

public class Resultado
{
    protected Resultado(Erro? erro) => Erro = erro;

    public bool Sucesso => Erro is null;
    public Erro? Erro { get; }

    public static Resultado Ok() => new(null);
    public static Resultado Falha(Erro erro) => new(erro);

    public static implicit operator Resultado(Erro erro) => Falha(erro);
}

public sealed class Resultado<T> : Resultado
{
    private readonly T? _valor;

    private Resultado(T valor) : base(null) => _valor = valor;
    private Resultado(Erro erro) : base(erro) { }

    public T Valor => Sucesso
        ? _valor!
        : throw new InvalidOperationException("Não há valor em um resultado de falha.");

    public static Resultado<T> Ok(T valor) => new(valor);
    public new static Resultado<T> Falha(Erro erro) => new(erro);

    public static implicit operator Resultado<T>(T valor) => Ok(valor);
    public static implicit operator Resultado<T>(Erro erro) => Falha(erro);
}
