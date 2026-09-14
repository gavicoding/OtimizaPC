using System.IO;
using System.Security.Cryptography;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public class DuplicateFinderService
{
    public Task<List<DuplicateGroup>> EncontrarDuplicadosAsync(
        string pastaRaiz,
        IProgress<string>? progresso = null,
        CancellationToken ct = default) =>
        EncontrarDuplicadosAsync(new[] { pastaRaiz }, null, progresso, ct);

    public async Task<List<DuplicateGroup>> EncontrarDuplicadosAsync(
        IEnumerable<string> pastasRaiz,
        Func<string, bool>? devePular,
        IProgress<string>? progresso = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // Reporta o progresso no máximo a cada ~150ms — sem isso, uma varredura de
            // "Todo o PC" com centenas de milhares de arquivos inunda a thread de UI de
            // atualizações e a interface parece travada até a varredura terminar.
            var progressoLimitado = new ProgressoComLimite(progresso);

            progressoLimitado.Report("Listando arquivos...");
            var arquivos = new List<FileInfo>();
            foreach (var arquivo in pastasRaiz.SelectMany(pasta => FileSystemHelper.EnumerarArquivosComSeguranca(pasta, ct, devePular)))
            {
                ct.ThrowIfCancellationRequested();
                if (SafeLength(arquivo) > 0)
                    arquivos.Add(arquivo);
                progressoLimitado.Report($"Listando arquivos... {arquivos.Count} encontrados");
            }

            progressoLimitado.Report($"{arquivos.Count} arquivos encontrados. Agrupando por tamanho...");
            var candidatosPorTamanho = arquivos
                .GroupBy(f => SafeLength(f))
                .Where(g => g.Count() > 1)
                .ToList();

            var grupos = new List<DuplicateGroup>();
            int processados = 0;
            int totalCandidatos = candidatosPorTamanho.Sum(g => g.Count());

            foreach (var grupoTamanho in candidatosPorTamanho)
            {
                ct.ThrowIfCancellationRequested();
                var porHash = new Dictionary<string, List<DuplicateFileEntry>>();

                foreach (var arquivo in grupoTamanho)
                {
                    ct.ThrowIfCancellationRequested();
                    processados++;
                    progressoLimitado.Report($"Comparando arquivos ({processados}/{totalCandidatos})...");

                    var hash = CalcularHash(arquivo.FullName);
                    if (hash == null) continue;

                    if (!porHash.TryGetValue(hash, out var lista))
                    {
                        lista = new List<DuplicateFileEntry>();
                        porHash[hash] = lista;
                    }

                    lista.Add(new DuplicateFileEntry
                    {
                        CaminhoCompleto = arquivo.FullName,
                        TamanhoBytes = SafeLength(arquivo),
                        DataModificacao = SafeLastWrite(arquivo)
                    });
                }

                foreach (var (hash, lista) in porHash)
                {
                    if (lista.Count > 1)
                    {
                        // Sugere manter o arquivo mais antigo e marcar os demais para exclusão.
                        var ordenados = lista.OrderBy(a => a.DataModificacao).ToList();
                        for (int i = 1; i < ordenados.Count; i++)
                            ordenados[i].MarcadoParaExclusao = true;

                        grupos.Add(new DuplicateGroup { HashArquivo = hash, Arquivos = ordenados });
                    }
                }
            }

            return grupos.OrderByDescending(g => g.EspacoDesperdicado).ToList();
        }, ct);
    }

    private static string? CalcularHash(string caminho)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(caminho);
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToHexString(hashBytes);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private static long SafeLength(FileInfo f)
    {
        try { return f.Length; } catch (IOException) { return 0; }
    }

    private static DateTime SafeLastWrite(FileInfo f)
    {
        try { return f.LastWriteTimeUtc; } catch (IOException) { return DateTime.UtcNow; }
    }
}
