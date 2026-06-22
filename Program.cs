using System.Runtime.InteropServices;

class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            Console.WriteLine("\nDetenido. Adiós!");
            Environment.Exit(0);
        };

        NativeMethods.SetThreadExecutionState(
            NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);

        Console.WriteLine("=== KeepAwake ===");
        Console.WriteLine("Presiona Ctrl+C para detener.");
        Console.WriteLine();

        int mouseMoveIntervalSeconds = 60;
        int direction = 1;
        bool warned = false;

        if (args.Length > 0 && int.TryParse(args[0], out int customInterval) && customInterval > 0)
        {
            mouseMoveIntervalSeconds = customInterval;
        }

        Console.WriteLine($"Intervalo de movimiento del mouse: {mouseMoveIntervalSeconds} segundos");
        Console.WriteLine();

        while (true)
        {
            var lastInputInfo = new NativeMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            NativeMethods.GetLastInputInfo(ref lastInputInfo);
            uint idleTimeMs = NativeMethods.GetTickCount() - lastInputInfo.dwTime;
            double idleTimeSeconds = idleTimeMs / 1000.0;

            bool userIsAway = idleTimeSeconds >= mouseMoveIntervalSeconds * 0.8;

            if (userIsAway)
            {
                NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
                inputs[0].type = NativeMethods.INPUT_MOUSE;
                inputs[0].mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE;
                inputs[0].mi.dx = 1 * direction;
                inputs[0].mi.dy = 0;
                NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

                direction *= -1;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Mouse movido (idle: {idleTimeSeconds:F0}s)");

                if (!warned)
                {
                    Console.WriteLine("  El movimiento evita que Teams muestre estado 'Ausente'");
                    warned = true;
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(mouseMoveIntervalSeconds));
        }
    }
}

static class NativeMethods
{
    public const uint INPUT_MOUSE = 0;
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint ES_CONTINUOUS = 0x80000000;
    public const uint ES_SYSTEM_REQUIRED = 0x00000001;
    public const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint SetThreadExecutionState(uint esFlags);

    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();
}
