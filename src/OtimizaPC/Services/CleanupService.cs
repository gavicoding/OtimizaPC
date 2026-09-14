using System.IO;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public class CleanupResult
{
    public long BytesLiberados { get; set; }
    public int ArquivosExcluidos { get; set; }
    public int ArquivosComFalha { get; set; }
    public List<string> Avisos { get; } = new();
}

public class CleanupService
{
    public async Task EscanearCategoriaAsync(CleanupCategory categoria, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            categoria.ArquivosEncontrados.Clear();
            long total = 0;
            int quantidade = 0;

            if (categoria.Definicao.Tipo == CleanupTargetKind.RecycleBin)
            {
                foreach (var unidade in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
                {
                    var lixeira = Path.Combine(unidade.RootDirectory.FullName, "$Recycle.Bin");
                    if (!Directory.Exists(lixeira)) continue;
                    foreach (var arquivo in FileSystemHelper.EnumerarArquivosComSeguranca(lixeira, ct))
                    {
                        total += SafeLength(arquivo);
                        quantidade++;
                    }
                }
            }
            else
            {
                foreach (var caminhoDefinido in categoria.Definicao.Caminhos)
                {
                    foreach (var caminhoReal in CleanupDefinitions.ExpandirCaminho(caminhoDefinido))
                    {
                        foreach (var arquivo in FileSystemHelper.EnumerarArquivosComSeguranca(caminhoReal, ct))
                        {
                            if (!PassaFiltros(arquivo, categoria.Definicao)) continue;
                            categoria.ArquivosEncontrados.Add(arquivo.FullName);
                            total += SafeLength(arquivo);
                            quantidade++;
                        }
                    }
                }
            }

            categoria.TamanhoBytes = total;
            categoria.QuantidadeArquivos = quantidade;
            categoria.Escaneado = true;

            if (quantidade == 0 && categoria.Definicao.RequerAdministrador && !ElevationHelper.EstaElevado())
                categoria.Status = "Reinicie como administrador para ver isto";
            else
                categoria.Status = quantidade == 0 ? "Nada a limpar" : $"{quantidade} itens encontrados";
        }, ct);
    }

    public async Task<CleanupResult> LimparAsync(IEnumerable<CleanupCategory> categoriasSelecionadas, IProgress<string>? progresso = null, CancellationToken ct = default)
    {
        var resultado = new CleanupResult();

        await Task.Run(() =>
        {
            foreach (var categoria in categoriasSelecionadas)
            {
                ct.ThrowIfCancellationRequested();
                progresso?.Report($"Limpando: {categoria.Nome}...");

                if (categoria.Definicao.Tipo == CleanupTargetKind.RecycleBin)
                {
                    LimparLixeira(resultado);
                    continue;
                }

                if (categoria.Definicao.RequerAdministrador && !ElevationHelper.EstaElevado())
                {
                    resultado.Avisos.Add($"'{categoria.Nome}' foi pulada — requer administrador.");
                    continue;
                }

                foreach (var caminhoArquivo in categoria.ArquivosEncontrados)
                {
                    ct.ThrowIfCancellationRequested();
                    long tamanho = 0;
                    try { tamanho = new FileInfo(caminhoArquivo).Length; } catch (IOException) { }

                    if (FileSystemHelper.TentarExcluirArquivo(caminhoArquivo))
                    {
                        resultado.BytesLiberados += tamanho;
                        resultado.ArquivosExcluidos++;
                    }
                    else
                    {
                        resultado.ArquivosComFalha++;
                    }
                }

                RemoverPastasVaziasResultantes(categoria);
            }
        }, ct);

        return resultado;
    }

    private static void LimparLixeira(CleanupResult resultado)
    {
        long tamanhoAntes = 0;
        foreach (var unidade in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            var lixeira = Path.Combine(unidade.RootDirectory.FullName, "$Recycle.Bin");
            if (Directory.Exists(lixeira))
                tamanhoAntes += FileSystemHelper.CalcularTamanhoPasta(lixeira);
        }

        var flags = NativeMethods.RecycleFlags.SHERB_NOCONFIRMATION
                  | NativeMethods.RecycleFlags.SHERB_NOPROGRESSUI
                  | NativeMethods.RecycleFlags.SHERB_NOSOUND;

        var hr = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);
        if (hr == 0 /* S_OK */)
        {
            resultado.BytesLiberados += tamanhoAntes;
        }
        else if (tamanhoAntes == 0)
        {
            // Lixeira já estava vazia — o HRESULT de "nada a fazer" não é um erro real.
        }
        else
        {
            resultado.Avisos.Add("Não foi possível esvaziar a Lixeira completamente.");
        }
    }

    private static bool PassaFiltros(FileInfo arquivo, CleanupCategoryDefinition definicao)
    {
        if (definicao.ExtensoesPermitidas != null &&
            !definicao.ExtensoesPermitidas.Contains(arquivo.Extension, StringComparer.OrdinalIgnoreCase))
            return false;

        if (definicao.IdadeMinimaDias is int dias)
        {
            try
            {
                if (arquivo.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-dias))
                    return false;
            }
            catch (IOException) { return false; }
        }

        return true;
    }

    private static void RemoverPastasVaziasResultantes(CleanupCategory categoria)
    {
        var pastas = categoria.ArquivosEncontrados
            .Select(Path.GetDirectoryName)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderByDescending(p => p!.Length);

        foreach (var pasta in pastas)
        {
            try
            {
                if (Directory.Exists(pasta) && !Directory.EnumerateFileSystemEntries(pasta!).Any())
                    Directory.Delete(pasta!);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static long SafeLength(FileInfo arquivo)
    {
        try { return arquivo.Length; }
        catch (IOException) { return 0; }
    }
}
