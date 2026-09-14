using System.IO;

namespace OtimizaPC.Helpers;

public static class FileSystemHelper
{
    /// <summary>
    /// Enumera arquivos recursivamente ignorando pastas sem permissão de acesso e
    /// link simbólicos/reparse points (evita loops e contagem duplicada de espaço).
    /// </summary>
    public static IEnumerable<FileInfo> EnumerarArquivosComSeguranca(string pastaRaiz, CancellationToken ct = default, Func<string, bool>? devePular = null)
    {
        var pilha = new Stack<string>();
        pilha.Push(pastaRaiz);

        while (pilha.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var pastaAtual = pilha.Pop();

            if (devePular != null && devePular(pastaAtual))
                continue;

            IEnumerable<string> subpastas = Array.Empty<string>();
            try
            {
                var dirInfo = new DirectoryInfo(pastaAtual);
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                subpastas = Directory.EnumerateDirectories(pastaAtual);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }

            foreach (var sub in subpastas)
                pilha.Push(sub);

            IEnumerable<string> arquivos = Array.Empty<string>();
            try
            {
                arquivos = Directory.EnumerateFiles(pastaAtual);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }

            foreach (var arquivo in arquivos)
            {
                ct.ThrowIfCancellationRequested();
                FileInfo? info = null;
                try
                {
                    info = new FileInfo(arquivo);
                    if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;
                }
                catch (UnauthorizedAccessException) { info = null; }
                catch (IOException) { info = null; }

                if (info != null)
                    yield return info;
            }
        }
    }

    public static long CalcularTamanhoPasta(string caminho, CancellationToken ct = default)
    {
        long total = 0;
        foreach (var arquivo in EnumerarArquivosComSeguranca(caminho, ct))
        {
            try { total += arquivo.Length; }
            catch (IOException) { }
        }
        return total;
    }

    /// <summary>
    /// Calcula tamanho total e a data de modificação mais recente dentro da pasta
    /// (incluindo subpastas) numa única varredura. A data mais recente é o sinal mais
    /// confiável de que algo ainda está em uso — muito mais confiável do que tentar
    /// adivinhar pelo nome da pasta.
    /// </summary>
    public static (long TamanhoBytes, DateTime UltimaModificacaoUtc) AnalisarPasta(string caminho, CancellationToken ct = default)
    {
        long total = 0;
        var ultima = DateTime.MinValue;

        try { ultima = new DirectoryInfo(caminho).LastWriteTimeUtc; }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        foreach (var arquivo in EnumerarArquivosComSeguranca(caminho, ct))
        {
            try
            {
                total += arquivo.Length;
                if (arquivo.LastWriteTimeUtc > ultima)
                    ultima = arquivo.LastWriteTimeUtc;
            }
            catch (IOException) { }
        }

        return (total, ultima);
    }

    public static bool TentarExcluirArquivo(string caminho)
    {
        try
        {
            var info = new FileInfo(caminho);
            if (info.IsReadOnly)
                info.IsReadOnly = false;
            info.Delete();
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }

    public static bool TentarExcluirPasta(string caminho)
    {
        try
        {
            Directory.Delete(caminho, recursive: true);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }
}
