using System.Runtime.InteropServices;

class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int _intervalSeconds;
    private int _direction = 1;

    public MainForm()
    {
        _intervalSeconds = ParseInterval();
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Load += (_, _) => Hide();

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Mostrar", null, (_, _) => ShowWindow());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("Salir", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = $"KeepAwake ({_intervalSeconds}s)",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        NativeMethods.SetThreadExecutionState(
            NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);

        _timer = new System.Windows.Forms.Timer { Interval = _intervalSeconds * 1000 };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private static int ParseInterval()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && int.TryParse(args[1], out int custom) && custom > 0)
            return custom;
        return 60;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var lastInputInfo = new NativeMethods.LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        NativeMethods.GetLastInputInfo(ref lastInputInfo);
        uint idleMs = NativeMethods.GetTickCount() - lastInputInfo.dwTime;

        if (idleMs >= _intervalSeconds * 800)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE;
            inputs[0].mi.dx = _direction;
            inputs[0].mi.dy = 0;
            NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
            _direction *= -1;
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
    }

    private new void Hide()
    {
        base.Hide();
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon?.Dispose();
            _trayMenu?.Dispose();
            _timer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
