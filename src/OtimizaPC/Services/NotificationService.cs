using System.Drawing;
using System.Windows.Forms;

namespace OtimizaPC.Services;

/// <summary>
/// Ícone na bandeja do sistema usado só para exibir notificações nativas do Windows
/// quando uma tarefa longa (escaneamento) termina em segundo plano.
/// </summary>
public class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instancia = new(() => new NotificationService());
    public static NotificationService Instancia => _instancia.Value;

    private readonly NotifyIcon _icone;

    private NotificationService()
    {
        Icon icone;
        try { icone = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "")! ?? SystemIcons.Application; }
        catch { icone = SystemIcons.Application; }

        _icone = new NotifyIcon
        {
            Icon = icone,
            Text = "OtimizaPC",
            Visible = false
        };
    }

    public void Notificar(string titulo, string mensagem)
    {
        _icone.Visible = true;
        _icone.BalloonTipTitle = titulo;
        _icone.BalloonTipText = mensagem;
        _icone.ShowBalloonTip(6000);
    }

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.Dispose();
    }
}
