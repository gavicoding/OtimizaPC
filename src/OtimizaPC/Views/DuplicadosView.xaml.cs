using System.Windows;
using System.Windows.Controls;
using OtimizaPC.Helpers;
using OtimizaPC.ViewModels;

namespace OtimizaPC.Views;

public partial class DuplicadosView : UserControl
{
    public DuplicadosView()
    {
        InitializeComponent();
    }

    private void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DuplicadosViewModel vm) return;

        var marcados = vm.ObterMarcadosParaExclusao();
        if (marcados.Count == 0)
        {
            MessageBox.Show("Nenhum arquivo marcado para exclusão.", "OtimizaPC", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var total = FormatHelper.FormatarBytes(marcados.Sum(a => a.TamanhoBytes));
        var pergunta = $"Isto vai apagar permanentemente {marcados.Count} arquivo(s), liberando {total}.\n\nDeseja continuar?";
        var resposta = MessageBox.Show(pergunta, "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (resposta == MessageBoxResult.Yes)
            vm.ExcluirMarcados();
    }
}
