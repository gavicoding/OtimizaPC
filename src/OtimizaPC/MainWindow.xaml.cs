using System.Windows;
using OtimizaPC.Services;
using OtimizaPC.ViewModels;

namespace OtimizaPC;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Closed += (_, _) =>
        {
            _viewModel.Dashboard.Dispose();
            NotificationService.Instancia.Dispose();
        };
    }
}
