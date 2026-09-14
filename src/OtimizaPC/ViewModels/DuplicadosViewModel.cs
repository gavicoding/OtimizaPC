using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class DuplicadosViewModel : ObservableObject
{
    private readonly DuplicateFinderService _servico = new();
    private CancellationTokenSource? _cts;

    public TarefaSegundoPlanoStatus StatusTarefa { get; } = new("🔍");
    public ObservableCollection<DuplicateGroup> Grupos { get; } = new();

    [ObservableProperty]
    private string pastaSelecionada = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private bool escanearPcInteiro;

    [ObservableProperty]
    private bool escaneando;

    [ObservableProperty]
    private string status = "Escolha uma pasta (ou marque \"Todo o PC\") e clique em \"Procurar duplicados\".";

    public long EspacoDesperdicadoTotal => Grupos.SelectMany(g => g.Arquivos).Where(a => a.MarcadoParaExclusao).Sum(a => a.TamanhoBytes);
    public string EspacoDesperdicadoTexto => FormatHelper.FormatarBytes(EspacoDesperdicadoTotal);

    public int TotalMarcadosParaExclusao => Grupos.SelectMany(g => g.Arquivos).Count(a => a.MarcadoParaExclusao);

    [RelayCommand]
    private void EscolherPasta()
    {
        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta para procurar duplicados" };
        if (dialogo.ShowDialog() == true)
            PastaSelecionada = dialogo.FolderName;
    }

    [RelayCommand]
    private async Task ProcurarDuplicadosAsync()
    {
        List<string> pastasRaiz;
        Func<string, bool> devePular;

        if (EscanearPcInteiro)
        {
            pastasRaiz = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .ToList();
            var ignoradas = ObterPastasIgnoradasNoPcInteiro(pastasRaiz);
            devePular = pasta => ignoradas.Contains(pasta.TrimEnd('\\'));
        }
        else
        {
            if (!Directory.Exists(PastaSelecionada))
            {
                Status = "Pasta inválida.";
                return;
            }
            pastasRaiz = new List<string> { PastaSelecionada };
            var ignoradas = ObterPastasSempreIgnoradas(pastasRaiz);
            devePular = pasta => ignoradas.Contains(pasta.TrimEnd('\\'));
        }

        Escaneando = true;
        StatusTarefa.Ocupado = true;
        StatusTarefa.LimparConcluido();
        Grupos.Clear();
        _cts = new CancellationTokenSource();
        var progresso = new Progress<string>(msg => Status = msg);

        try
        {
            var grupos = await _servico.EncontrarDuplicadosAsync(pastasRaiz, devePular, progresso, _cts.Token);
            foreach (var grupo in grupos)
            {
                foreach (var arquivo in grupo.Arquivos)
                    arquivo.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(DuplicateFileEntry.MarcadoParaExclusao))
                            AtualizarTotais();
                    };
                Grupos.Add(grupo);
            }

            Status = grupos.Count == 0
                ? "Nenhum arquivo duplicado encontrado."
                : $"{grupos.Count} grupos de duplicados encontrados.";
            StatusTarefa.MarcarConcluido();
            NotificationService.Instancia.Notificar("Busca de duplicados concluída", Status);
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

    [RelayCommand]
    private void Cancelar() => _cts?.Cancel();

    private static HashSet<string> ObterPastasSempreIgnoradas(IEnumerable<string> raizes)
    {
        var ignoradas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raiz in raizes)
        {
            var drive = Path.GetPathRoot(raiz)?.TrimEnd('\\') ?? "";
            if (string.IsNullOrEmpty(drive)) continue;
            ignoradas.Add(Path.Combine(drive, "$Recycle.Bin"));
            ignoradas.Add(Path.Combine(drive, "System Volume Information"));
        }
        return ignoradas;
    }

    private static HashSet<string> ObterPastasIgnoradasNoPcInteiro(IEnumerable<string> raizes)
    {
        var ignoradas = ObterPastasSempreIgnoradas(raizes);
        ignoradas.Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\'));
        ignoradas.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\'));
        ignoradas.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).TrimEnd('\\'));
        ignoradas.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).TrimEnd('\\'));
        return ignoradas;
    }

    public void AtualizarTotais()
    {
        OnPropertyChanged(nameof(EspacoDesperdicadoTotal));
        OnPropertyChanged(nameof(EspacoDesperdicadoTexto));
        OnPropertyChanged(nameof(TotalMarcadosParaExclusao));
    }

    public List<DuplicateFileEntry> ObterMarcadosParaExclusao() =>
        Grupos.SelectMany(g => g.Arquivos).Where(a => a.MarcadoParaExclusao).ToList();

    public (int excluidos, long bytesLiberados) ExcluirMarcados()
    {
        int excluidos = 0;
        long bytes = 0;

        foreach (var grupo in Grupos.ToList())
        {
            foreach (var arquivo in grupo.Arquivos.Where(a => a.MarcadoParaExclusao).ToList())
            {
                if (FileSystemHelper.TentarExcluirArquivo(arquivo.CaminhoCompleto))
                {
                    excluidos++;
                    bytes += arquivo.TamanhoBytes;
                    grupo.Arquivos.Remove(arquivo);
                }
            }

            if (grupo.Arquivos.Count <= 1)
                Grupos.Remove(grupo);
        }

        AtualizarTotais();
        Status = $"{excluidos} arquivos removidos, {FormatHelper.FormatarBytes(bytes)} liberados.";
        return (excluidos, bytes);
    }
}
