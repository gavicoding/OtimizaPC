using CommunityToolkit.Mvvm.ComponentModel;

namespace OtimizaPC.ViewModels;

/// <summary>
/// Estado de "está rodando em segundo plano" de uma aba, usado pelo menu lateral pra
/// mostrar um indicador mesmo quando o usuário navegou pra outra tela.
/// </summary>
public partial class TarefaSegundoPlanoStatus : ObservableObject
{
    private readonly string _emojiOcupado;

    public TarefaSegundoPlanoStatus(string emojiOcupado = "🔄")
    {
        _emojiOcupado = emojiOcupado;
    }

    [ObservableProperty]
    private bool ocupado;

    [ObservableProperty]
    private bool concluidoRecentemente;

    public string Indicador => Ocupado ? _emojiOcupado : (ConcluidoRecentemente ? "✅" : "");

    partial void OnOcupadoChanged(bool value) => OnPropertyChanged(nameof(Indicador));
    partial void OnConcluidoRecentementeChanged(bool value) => OnPropertyChanged(nameof(Indicador));

    public void MarcarConcluido()
    {
        Ocupado = false;
        ConcluidoRecentemente = true;
    }

    public void LimparConcluido() => ConcluidoRecentemente = false;
}
