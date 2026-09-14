using System.Windows;
using System.Windows.Controls;
using OtimizaPC.Helpers;
using OtimizaPC.ViewModels;

namespace OtimizaPC.Views;

public partial class ResiduosView : UserControl
{
    public ResiduosView()
    {
        InitializeComponent();
    }

    private void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ResiduosViewModel vm) return;

        var selecionados = vm.ObterSelecionados();
        if (selecionados.Count == 0)
        {
            MessageBox.Show("Nenhum item selecionado.", "OtimizaPC", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var total = FormatHelper.FormatarBytes(selecionados.Sum(i => i.TamanhoBytes));
        var lista = string.Join("\n", selecionados.Take(10).Select(i => $"  • {i.Caminho}"));
        if (selecionados.Count > 10) lista += $"\n  ... e mais {selecionados.Count - 10} pasta(s)";

        var pergunta = $"Isto vai apagar permanentemente {selecionados.Count} pasta(s), liberando {total}:\n\n{lista}\n\nTem certeza que estas pastas pertencem a programas já desinstalados?";
        var resposta = MessageBox.Show(pergunta, "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (resposta == MessageBoxResult.Yes)
            vm.ExcluirSelecionados();
    }
}
