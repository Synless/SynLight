using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SynLight.MonitorState
{
    public class PowerBroadcastListener : IDisposable
    {
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;

        private static Guid GUID_CONSOLE_DISPLAY_STATE =
            new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");

        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private HwndSource _source;

        public event Action<bool> MonitorStateChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(
            IntPtr hRecipient,
            ref Guid PowerSettingGuid,
            int Flags);

        public PowerBroadcastListener()
        {
            var parameters = new HwndSourceParameters("PowerListener")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0x800000, // WS_OVERLAPPED (no UI)
            };

            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);

            RegisterPowerSettingNotification(
                _source.Handle,
                ref GUID_CONSOLE_DISPLAY_STATE,
                DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_POWERBROADCAST &&
                wParam.ToInt32() == PBT_POWERSETTINGCHANGE)
            {
                var ps = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(lParam);

                if (ps.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
                {
                    int state = ps.Data[0];

                    MonitorStateChanged?.Invoke(state != 0);
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Data;
        }
    }
}
