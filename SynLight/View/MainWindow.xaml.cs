
using SynLight.Model;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SynLight.View
{
    public partial class MainWindow : Window
    {
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;

        private static Guid GUID_CONSOLE_DISPLAY_STATE =
            new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(
            IntPtr hRecipient,
            ref Guid PowerSettingGuid,
            int Flags);

        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Data;
        }
        private IntPtr WndProc(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam, ref bool handled)
        {
            if (msg == WM_POWERBROADCAST &&
                wParam.ToInt32() == PBT_POWERSETTINGCHANGE)
            {
                POWERBROADCAST_SETTING ps =
                    Marshal.PtrToStructure<POWERBROADCAST_SETTING>(lParam);

                if (ps.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
                {
                    int state = ps.Data[0];

                    var process = DataContext as Process_SynLight;

                    if (process != null)
                    {
                        if (state == 0) // monitor OFF
                            process.SetMonitorState(false);

                        if (state == 1) // monitor ON
                            process.SetMonitorState(true);
                    }
                }
            }

            return IntPtr.Zero;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(WndProc);

            RegisterPowerSettingNotification(source.Handle, ref GUID_CONSOLE_DISPLAY_STATE, DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        private System.Windows.Forms.NotifyIcon m_notifyIcon;

        public MainWindow()
        {
            Startup.StartOrKill();

            SetLanguageDictionary();
            InitializeComponent();

            // System tray stuff here
            m_notifyIcon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.BalloonTipText = "SynLight has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "SynLight";
            m_notifyIcon.Text = "SynLight"; System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri("..\\Images\\SY.ico", UriKind.Relative)).Stream);
            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);

            if (Startup.ShowOrHide())
            {
                SystemCommands.MinimizeWindow(this);
                this.WindowState = (WindowState)System.Windows.Forms.FormWindowState.Minimized;
                //Hide();
            }

            return;
        }



        #region System tray
        void OnClose(object sender, CancelEventArgs args)
        {
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }
        private System.Windows.WindowState m_storedWindowState = 0;
        void OnStateChanged(object sender, EventArgs args)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                Hide();
                if (m_notifyIcon != null)
                    m_notifyIcon.ShowBalloonTip(2000);
            }
            else
                m_storedWindowState = WindowState;
        }
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }
        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }
        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }
        #endregion

        private void SetLanguageDictionary()
        {
            ResourceDictionary dict = new ResourceDictionary();
            try
            {
                dict.Source = new Uri("..\\Resources\\StringResources." + Thread.CurrentThread.CurrentCulture.ToString() + ".xaml", UriKind.Relative);
            }
            catch
            {
                dict.Source = new Uri("..\\Resources\\StringResources.xaml", UriKind.Relative);
            }
            Resources.MergedDictionaries.Add(dict);
        }
        protected override void OnClosed(EventArgs e)
        {
            Environment.Exit(0);
        }
        private void PositiveNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _dropDownButton.IsOpen = false;
        }
        // Can execute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
            //Param_SynLight.debug = true;
        }
        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }
        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }
        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}
