using System.IO;
using Microsoft.Win32;

namespace OtimizaPC.Helpers;

public static class UserProfileHelper
{
    /// <summary>
    /// Lê a lista oficial de perfis de usuário do Windows (registro), que é mais
    /// confiável do que simplesmente listar subpastas de C:\Users.
    /// </summary>
    private static List<string> ObterTodosOsPerfis()
    {
        var perfis = new List<string>();
        try
        {
            using var chaveLista = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (chaveLista == null) return perfis;

            foreach (var sid in chaveLista.GetSubKeyNames())
            {
                using var chaveSid = chaveLista.OpenSubKey(sid);
                var caminho = chaveSid?.GetValue("ProfileImagePath") as string;
                if (!string.IsNullOrWhiteSpace(caminho) && Directory.Exists(caminho))
                    perfis.Add(caminho);
            }
        }
        catch (System.Security.SecurityException) { }

        return perfis;
    }

    /// <summary>
    /// Perfis de outras pessoas que usam esta máquina (exclui o usuário atual, a pasta
    /// Public e contas de serviço do Windows que não moram em C:\Users). Ler ou apagar
    /// arquivos nessas pastas normalmente exige administrador — o Windows bloqueia o
    /// acesso via permissão de arquivo, então sem elevação essas pastas simplesmente
    /// aparecem vazias na varredura.
    /// </summary>
    public static List<string> ObterOutrosPerfisDeUsuarios()
    {
        var perfilAtual = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var pastaUsuarios = Path.GetDirectoryName(perfilAtual);
        if (string.IsNullOrEmpty(pastaUsuarios)) return new List<string>();

        return ObterTodosOsPerfis()
            .Where(p => string.Equals(Path.GetDirectoryName(p), pastaUsuarios, StringComparison.OrdinalIgnoreCase))
            .Where(p => !string.Equals(p, perfilAtual, StringComparison.OrdinalIgnoreCase))
            .Where(p => !Path.GetFileName(p).Equals("Public", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
