using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class ResiduosViewModel : ObservableObject
{
    private readonly ResidueService _servico = new();
    private CancellationTokenSource? _cts;

    public TarefaSegundoPlanoStatus StatusTarefa { get; } = new("🗑️");
    public ObservableCollection<ResidueItem> Itens { get; } = new();

    [ObservableProperty]
    private bool escaneando;

    [ObservableProperty]
    private string status = "Procura pastas de programas que já foram desinstalados, mas deixaram resíduos. Revise cada item antes de apagar.";

    public long TamanhoSelecionadoTotal => Itens.Where(i => i.Selecionado).Sum(i => i.TamanhoBytes);
    public string TamanhoSelecionadoTexto => FormatHelper.FormatarBytes(TamanhoSelecionadoTotal);

    [RelayCommand]
    private async Task EscanearAsync()
    {
        Escaneando = true;
        StatusTarefa.Ocupado = true;
        StatusTarefa.LimparConcluido();
        Itens.Clear();
        _cts = new CancellationTokenSource();
        var progresso = new Progress<string>(msg => Status = msg);

        try
        {
            var itens = await _servico.EncontrarResiduosAsync(progresso, _cts.Token);
            foreach (var item in itens)
            {
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ResidueItem.Selecionado))
                        AtualizarTotais();
                };
                Itens.Add(item);
            }
            Status = itens.Count == 0
                ? "Nenhum resíduo óbvio encontrado."
                : $"{itens.Count} possíveis resíduos encontrados. Revise antes de excluir.";
            StatusTarefa.MarcarConcluido();
            NotificationService.Instancia.Notificar("Busca de resíduos concluída", Status);
        }
        catch (OperationCanceledException)
        {
            Status = "Busca cancelada.";
        }
        finally
        {
            Escaneando = false;
            StatusTarefa.Ocupado = false;
            AtualizarTotais();
        }
    }

    private void AtualizarTotais()
    {
        OnPropertyChanged(nameof(TamanhoSelecionadoTotal));
        OnPropertyChanged(nameof(TamanhoSelecionadoTexto));
    }

    public List<ResidueItem> ObterSelecionados() => Itens.Where(i => i.Selecionado).ToList();

    public (int excluidos, long bytesLiberados) ExcluirSelecionados()
    {
        int excluidos = 0;
        long bytes = 0;

        foreach (var item in ObterSelecionados())
        {
            if (FileSystemHelper.TentarExcluirPasta(item.Caminho))
            {
                excluidos++;
                bytes += item.TamanhoBytes;
                Itens.Remove(item);
            }
        }

        AtualizarTotais();
        Status = $"{excluidos} pastas removidas, {FormatHelper.FormatarBytes(bytes)} liberados.";
        return (excluidos, bytes);
    }
}
