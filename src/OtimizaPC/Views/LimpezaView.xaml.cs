using System.Windows;
using System.Windows.Controls;
using OtimizaPC.Helpers;
using OtimizaPC.ViewModels;

namespace OtimizaPC.Views;

public partial class LimpezaView : UserControl
{
    public LimpezaView()
    {
        InitializeComponent();
    }

    private async void BotaoLimpar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LimpezaViewModel vm) return;

        var selecionados = vm.ObterSelecionadosComItens();
        if (selecionados.Count == 0)
        {
            MessageBox.Show("Nada selecionado (ou nada foi escaneado ainda).", "OtimizaPC", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var total = FormatHelper.FormatarBytes(selecionados.Sum(c => c.TamanhoBytes));
        var lista = string.Join("\n", selecionados.Select(c => $"  • {c.Nome} ({FormatHelper.FormatarBytes(c.TamanhoBytes)})"));

        var pergunta = $"Isto vai apagar permanentemente {total} nas seguintes categorias:\n\n{lista}\n\nDeseja continuar?";
        var resposta = MessageBox.Show(pergunta, "Confirmar limpeza", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (resposta == MessageBoxResult.Yes)
        {
            var resultado = await vm.LimparSelecionadosAsync();
            if (resultado.Avisos.Count > 0)
                MessageBox.Show(string.Join("\n", resultado.Avisos), "Avisos", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
