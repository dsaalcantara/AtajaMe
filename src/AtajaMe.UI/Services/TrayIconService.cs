using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AtajaMe.Application.Services;

namespace AtajaMe.UI.Services;

public interface ITrayIconService : IDisposable
{
    void Initialize();
    void UpdateState(bool isPaused);
    void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info);
}

/// <summary>
/// Gestiona el icono de la bandeja del sistema (System Tray), los estados visuales (activo/pausado)
/// y el menú contextual según RF-34 y RF-35.
/// </summary>
public sealed class TrayIconService : ITrayIconService
{
    private readonly IKeyboardExpansionOrchestrator _orchestrator;
    private readonly Action _openMainWindow;
    private readonly Action _openSettings;
    private readonly Action _syncNow;
    private readonly Action _exitApplication;

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _pauseResumeItem;
    private Icon? _activeIcon;
    private Icon? _pausedIcon;
    private bool _disposed;

    public TrayIconService(
        IKeyboardExpansionOrchestrator orchestrator,
        Action openMainWindow,
        Action openSettings,
        Action syncNow,
        Action exitApplication)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _openMainWindow = openMainWindow ?? throw new ArgumentNullException(nameof(openMainWindow));
        _openSettings = openSettings ?? throw new ArgumentNullException(nameof(openSettings));
        _syncNow = syncNow ?? throw new ArgumentNullException(nameof(syncNow));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
    }

    public void Initialize()
    {
        _activeIcon = CreateGeneratedIcon(Color.FromArgb(37, 99, 235), "A", isPaused: false);
        _pausedIcon = CreateGeneratedIcon(Color.FromArgb(217, 119, 6), "⏸", isPaused: true);

        _contextMenu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Abrir AtajaMe", null, (_, _) => _openMainWindow())
        {
            Font = new Font(_contextMenu.Font, System.Drawing.FontStyle.Bold)
        };
        _contextMenu.Items.Add(openItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        _pauseResumeItem = new ToolStripMenuItem("Pausar expansión", null, (_, _) => TogglePauseState());
        _contextMenu.Items.Add(_pauseResumeItem);

        var syncItem = new ToolStripMenuItem("Sincronizar ahora", null, (_, _) => _syncNow());
        _contextMenu.Items.Add(syncItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Configuración...", null, (_, _) => _openSettings());
        _contextMenu.Items.Add(settingsItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Salir de AtajaMe", null, (_, _) => _exitApplication());
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _activeIcon,
            Text = "AtajaMe · Gestor de Atajos (Activo)",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) => _openMainWindow();
    }

    public void UpdateState(bool isPaused)
    {
        if (_notifyIcon == null || _pauseResumeItem == null)
        {
            return;
        }

        if (isPaused)
        {
            _notifyIcon.Icon = _pausedIcon;
            _notifyIcon.Text = "AtajaMe · En Pausa (Atajos desactivados)";
            _pauseResumeItem.Text = "Reanudar expansión";
            _pauseResumeItem.Checked = true;
        }
        else
        {
            _notifyIcon.Icon = _activeIcon;
            _notifyIcon.Text = "AtajaMe · Gestor de Atajos (Activo)";
            _pauseResumeItem.Text = "Pausar expansión";
            _pauseResumeItem.Checked = false;
        }
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    private void TogglePauseState()
    {
        _orchestrator.IsPaused = !_orchestrator.IsPaused;
        UpdateState(_orchestrator.IsPaused);
    }

    private static Icon CreateGeneratedIcon(Color bgColor, string label, bool isPaused)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Fondo circular redondeado
            using var brush = new SolidBrush(bgColor);
            g.FillEllipse(brush, 1, 1, 30, 30);

            // Borde sutil
            using var pen = new Pen(Color.FromArgb(255, 255, 255), 1.5f);
            g.DrawEllipse(pen, 1, 1, 30, 30);

            if (isPaused)
            {
                // Dibujar dos barras verticales de pausa
                using var barBrush = new SolidBrush(Color.White);
                g.FillRectangle(barBrush, 10, 9, 4, 14);
                g.FillRectangle(barBrush, 18, 9, 4, 14);
            }
            else
            {
                // Letra "A"
                using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                using var textBrush = new SolidBrush(Color.White);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, font, textBrush, new RectangleF(0, 2, 32, 30), sf);
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        // Creamos una copia desacoplada del handle
        return (Icon)icon.Clone();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _activeIcon?.Dispose();
        _pausedIcon?.Dispose();
    }
}
