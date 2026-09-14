using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Models;

namespace OtimizaPC.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private TelaMenu telaAtual = TelaMenu.Dashboard;

    public DashboardViewModel Dashboard { get; } = new();
    public LimpezaViewModel Limpeza { get; } = new();
    public DuplicadosViewModel Duplicados { get; } = new();
    public ResiduosViewModel Residuos { get; } = new();
    public DesempenhoViewModel Desempenho { get; } = new();
    public EspacoDiscoViewModel EspacoDisco { get; } = new();

    [RelayCommand]
    private void Navegar(TelaMenu tela)
    {
        TelaAtual = tela;
        switch (tela)
        {
            case TelaMenu.Limpeza: Limpeza.StatusTarefa.LimparConcluido(); break;
            case TelaMenu.Duplicados: Duplicados.StatusTarefa.LimparConcluido(); break;
            case TelaMenu.Residuos: Residuos.StatusTarefa.LimparConcluido(); break;
            case TelaMenu.EspacoDisco: EspacoDisco.StatusTarefa.LimparConcluido(); break;
        }
    }
}
