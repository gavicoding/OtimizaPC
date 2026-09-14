using Microsoft.Win32;

namespace OtimizaPC.Services;

/// <summary>
/// Alterna entre "Melhor aparência" (padrão do Windows) e "Melhor desempenho"
/// usando exatamente as mesmas chaves de registro que a caixa de diálogo
/// Propriedades do Sistema > Desempenho usa.
/// </summary>
public class VisualEffectsService
{
    private const string ChaveVisualFx = @"Software\Microsoft\Windows\CurrentVersion\explorer\VisualEffects";
    private const string ChaveDesktop = @"Control Panel\Desktop";

    private static readonly byte[] MascaraMelhorDesempenho = { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
    private static readonly byte[] MascaraMelhorAparencia = { 0x9E, 0x1E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 };

    public bool EstaEmModoDesempenho()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(ChaveVisualFx);
        var valor = chave?.GetValue("VisualFXSetting");
        return valor is int i && i == 2;
    }

    public void DefinirModoDesempenho(bool ativar)
    {
        using (var chave = Registry.CurrentUser.CreateSubKey(ChaveVisualFx))
        {
            chave.SetValue("VisualFXSetting", ativar ? 2 : 0, RegistryValueKind.DWord);
        }

        using (var chave = Registry.CurrentUser.CreateSubKey(ChaveDesktop))
        {
            chave.SetValue("UserPreferencesMask", ativar ? MascaraMelhorDesempenho : MascaraMelhorAparencia, RegistryValueKind.Binary);
        }
    }
}
