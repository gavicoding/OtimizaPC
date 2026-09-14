using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.ViewModels;

namespace OtimizaPC.Views;

public partial class EspacoDiscoView : UserControl
{
    public EspacoDiscoView()
    {
        InitializeComponent();
    }

    private void Nome_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not EspacoDiscoViewModel vm) return;
        if (sender is not FrameworkElement elemento || elemento.DataContext is not DiskSpaceEntry item) return;

        if (item.EhPasta)
        {
            vm.AbrirPastaCommand.Execute(item.CaminhoCompleto);
        }
        else
        {
            vm.AbrirNoExplorerCommand.Execute(item);
        }
    }

    private void BotaoExcluirItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EspacoDiscoViewModel vm) return;
        if (sender is not FrameworkElement elemento || elemento.DataContext is not DiskSpaceEntry item) return;

        var tipo = item.EhPasta ? "pasta" : "arquivo";
        var pergunta = $"Excluir permanentemente {tipo} '{item.Nome}' ({FormatHelper.FormatarBytes(item.TamanhoBytes)})?";
        var resposta = MessageBox.Show(pergunta, "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (resposta == MessageBoxResult.Yes)
            vm.ExcluirItemCommand.Execute(item);
    }
}
