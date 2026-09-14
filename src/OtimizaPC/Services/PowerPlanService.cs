using System.Diagnostics;
using System.Text.RegularExpressions;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public partial class PowerPlanService
{
    public List<PowerPlanInfo> ObterPlanos()
    {
        var saida = ExecutarPowercfg("/list");
        var planos = new List<PowerPlanInfo>();

        foreach (Match match in RegexListaPlanos().Matches(saida))
        {
            planos.Add(new PowerPlanInfo
            {
                Guid = Guid.Parse(match.Groups["guid"].Value),
                Nome = match.Groups["nome"].Value.Trim(),
                Ativo = match.Groups["ativo"].Success
            });
        }

        return planos;
    }

    public void AtivarPlano(Guid guid) => ExecutarPowercfg($"/setactive {guid}");

    private static string ExecutarPowercfg(string argumentos)
    {
        var info = new ProcessStartInfo("powercfg", argumentos)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using var processo = Process.Start(info);
        if (processo == null) return "";
        var saida = processo.StandardOutput.ReadToEnd();
        processo.WaitForExit(5000);
        return saida;
    }

    [GeneratedRegex(@"(?:GUID do Esquema de Energia|Power Scheme GUID):\s*(?<guid>[0-9a-fA-F-]{36})\s*\((?<nome>[^)]+)\)\s*(?<ativo>\*)?", RegexOptions.IgnoreCase)]
    private static partial Regex RegexListaPlanos();
}
