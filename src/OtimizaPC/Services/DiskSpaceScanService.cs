using System.IO;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

/// <summary>
/// Faz uma única varredura recursiva de uma unidade inteira, calculando o tamanho de
/// cada pasta em todos os níveis de uma vez. O resultado fica em memória (uma lista de
/// filhos por pasta), então depois de escanear, navegar entre pastas é instantâneo —
/// não precisa recalcular nada a cada clique.
/// </summary>
public class DiskSpaceScanService
{
    /// <summary>
    /// Pastas com dezenas de milhares de arquivos soltos (node_modules, cache de
    /// pacotes, etc.) faziam a árvore em memória crescer sem limite — em discos de
    /// máquina de desenvolvedor isso passava de 2GB de RAM e deixava o programa
    /// travado por minutos. Cada pasta guarda no máximo esta quantidade de itens
    /// individuais; o resto vira um único item agregado (o tamanho total continua
    /// exato, só o detalhe item-a-item que é resumido).
    /// </summary>
    private const int MaximoItensPorPasta = 60;

    public async Task<Dictionary<string, List<DiskSpaceEntry>>> EscanearArvoreCompletaAsync(
        string raiz, IProgress<string>? progresso = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var progressoLimitado = new ProgressoComLimite(progresso);
            progressoLimitado.Report("Analisando a unidade inteira — pode levar alguns minutos na primeira vez...");
            var arvore = new Dictionary<string, List<DiskSpaceEntry>>(StringComparer.OrdinalIgnoreCase);
            int pastasProcessadas = 0;
            EscanearRecursivo(raiz, arvore, ct, progressoLimitado, ref pastasProcessadas);
            return arvore;
        }, ct);
    }

    private static long EscanearRecursivo(
        string pasta, Dictionary<string, List<DiskSpaceEntry>> arvore,
        CancellationToken ct, ProgressoComLimite progresso, ref int pastasProcessadas)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            if (new DirectoryInfo(pasta).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                arvore[pasta] = new List<DiskSpaceEntry>();
                return 0;
            }
        }
        catch (IOException) { arvore[pasta] = new List<DiskSpaceEntry>(); return 0; }
        catch (UnauthorizedAccessException) { arvore[pasta] = new List<DiskSpaceEntry>(); return 0; }

        var filhos = new List<DiskSpaceEntry>();
        long total = 0;

        IEnumerable<string> subpastas = Array.Empty<string>();
        try { subpastas = Directory.EnumerateDirectories(pasta); }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        foreach (var sub in subpastas)
        {
            var tamanhoSub = EscanearRecursivo(sub, arvore, ct, progresso, ref pastasProcessadas);
            total += tamanhoSub;
            filhos.Add(new DiskSpaceEntry { Nome = Path.GetFileName(sub), CaminhoCompleto = sub, EhPasta = true, TamanhoBytes = tamanhoSub });
        }

        IEnumerable<string> arquivos = Array.Empty<string>();
        try { arquivos = Directory.EnumerateFiles(pasta); }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        foreach (var arquivo in arquivos)
        {
            long tamanho;
            try
            {
                var info = new FileInfo(arquivo);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                tamanho = info.Length;
            }
            catch (IOException) { continue; }

            total += tamanho;
            filhos.Add(new DiskSpaceEntry { Nome = Path.GetFileName(arquivo), CaminhoCompleto = arquivo, EhPasta = false, TamanhoBytes = tamanho });
        }

        filhos.Sort((a, b) => b.TamanhoBytes.CompareTo(a.TamanhoBytes));

        if (filhos.Count > MaximoItensPorPasta)
        {
            var mantidos = filhos.Take(MaximoItensPorPasta - 1).ToList();
            var resto = filhos.Skip(MaximoItensPorPasta - 1).ToList();
            mantidos.Add(new DiskSpaceEntry
            {
                Nome = $"(+{resto.Count} outros itens)",
                CaminhoCompleto = Path.Combine(pasta, $"~outros~{resto.Count}"),
                EhPasta = false,
                TamanhoBytes = resto.Sum(r => r.TamanhoBytes)
            });
            filhos = mantidos;
        }

        foreach (var filho in filhos)
            filho.PercentualDoPai = total == 0 ? 0 : (double)filho.TamanhoBytes / total * 100;

        arvore[pasta] = filhos;

        pastasProcessadas++;
        progresso.Report($"Analisando... {pastasProcessadas} pastas processadas");

        return total;
    }
}
