using CommunityToolkit.Mvvm.ComponentModel;

namespace OtimizaPC.Models;

public partial class DuplicateFileEntry : ObservableObject
{
    public required string CaminhoCompleto { get; init; }
    public required long TamanhoBytes { get; init; }
    public required DateTime DataModificacao { get; init; }

    [ObservableProperty]
    private bool marcadoParaExclusao;
}

public class DuplicateGroup
{
    public required string HashArquivo { get; init; }
    public required List<DuplicateFileEntry> Arquivos { get; init; }

    public long TamanhoUnitario => Arquivos.Count > 0 ? Arquivos[0].TamanhoBytes : 0;
    public long EspacoDesperdicado => TamanhoUnitario * (Arquivos.Count - 1);
}
