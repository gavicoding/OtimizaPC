using CommunityToolkit.Mvvm.ComponentModel;

namespace OtimizaPC.Models;

public enum CleanupTargetKind
{
    Folder,
    RecycleBin
}

public class CleanupCategoryDefinition
{
    public required string Id { get; init; }
    public required string Nome { get; init; }
    public required string Descricao { get; init; }
    public required CleanupTargetKind Tipo { get; init; }
    public List<string> Caminhos { get; init; } = new();
    public bool SelecionadoPorPadrao { get; init; } = true;
    public bool RequerAdministrador { get; init; }
    public string[]? ExtensoesPermitidas { get; init; }
    public int? IdadeMinimaDias { get; init; }
}

public partial class CleanupCategory : ObservableObject
{
    public required CleanupCategoryDefinition Definicao { get; init; }

    [ObservableProperty]
    private bool selecionado;

    [ObservableProperty]
    private long tamanhoBytes;

    [ObservableProperty]
    private int quantidadeArquivos;

    [ObservableProperty]
    private bool escaneado;

    [ObservableProperty]
    private string status = "Não escaneado";

    public string Nome => Definicao.Nome;
    public string Descricao => Definicao.Descricao;

    public List<string> ArquivosEncontrados { get; } = new();
}
