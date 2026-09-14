using System.Diagnostics;
using System.Security.Principal;

namespace OtimizaPC.Helpers;

public static class ElevationHelper
{
    public static bool EstaElevado()
    {
        using var identidade = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identidade);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void ReiniciarComoAdministrador()
    {
        var caminhoExe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(caminhoExe)) return;

        var info = new ProcessStartInfo(caminhoExe)
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(info);
            Environment.Exit(0);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Usuário cancelou o UAC.
        }
    }
}
