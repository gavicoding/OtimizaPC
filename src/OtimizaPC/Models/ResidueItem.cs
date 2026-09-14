using CommunityToolkit.Mvvm.ComponentModel;

namespace OtimizaPC.Models;

public partial class ResidueItem : ObservableObject
{
    public required string Caminho { get; init; }
    public required long TamanhoBytes { get; init; }
    public required string Motivo { get; init; }

    [ObservableProperty]
    private bool selecionado;
}
