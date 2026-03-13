using System;
using System.Runtime.InteropServices;
using System.Timers;

public class BrightnessWatcher : IDisposable
{
    private Timer _timer;
    private int _lastBrightness = -1;

    public event Action<int> BrightnessChanged;

    public BrightnessWatcher(int intervalMs = 300)
    {
        _timer = new Timer(intervalMs);
        _timer.Elapsed += TimerElapsed;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        int brightness = GetBrightness();

        if (brightness >= 0 && brightness != _lastBrightness)
        {
            _lastBrightness = brightness;
            BrightnessChanged?.Invoke(brightness);
        }
    }

    private int GetBrightness()
    {
        IntPtr hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);

        uint monitorCount;
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out monitorCount))
            return -1;

        PHYSICAL_MONITOR[] physicalMonitors = new PHYSICAL_MONITOR[monitorCount];

        if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, monitorCount, physicalMonitors))
            return -1;

        uint min, current, max;

        bool success = GetMonitorBrightness(
            physicalMonitors[0].hPhysicalMonitor,
            out min,
            out current,
            out max);

        DestroyPhysicalMonitors(monitorCount, physicalMonitors);

        if (!success)
            return -1;

        return (int)current;
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }

    private const int MONITOR_DEFAULTTOPRIMARY = 1;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint numberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint physicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] physicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetMonitorBrightness(
        IntPtr handle,
        out uint minimumBrightness,
        out uint currentBrightness,
        out uint maximumBrightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(
        uint physicalMonitorArraySize,
        PHYSICAL_MONITOR[] physicalMonitorArray);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }
}