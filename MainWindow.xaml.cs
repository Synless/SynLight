using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SynLight
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            StartOrKill();
            ShowOrHide();
            SetLanguageDictionary();
            InitializeComponent();
            InitializeSystemTray();
        }

        private void StartOrKill()
        {
            try
            {
                Process[] xaml = Process.GetProcesses().OrderBy(m => m.ProcessName).ToArray();

                foreach (Process p in xaml)
                {
                    if (p.ProcessName.Contains("XAML Designer") || p.ProcessName.Contains("XDesProc"))
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                string procName = Process.GetCurrentProcess().ProcessName;
                List<Process> processes = Process.GetProcessesByName(procName).ToList();

                foreach (Process p in processes)
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        p.Kill();
                    }
                }
            }
            catch
            {
            }
        }

        private const string param = "param.txt";
        private void ShowOrHide()
        {
            if (File.Exists(param))
            {
                using (StreamReader sr = new StreamReader(param))
                {
                    string[] lines = sr.ReadToEnd().Split('\n');

                    foreach (string line in lines)
                    {
                        try
                        {
                            string[] subLine = line.ToUpper().Trim('\r').Split('=');

                            if (subLine[0] == "HIDE" && subLine[0] != "//HIDE")
                            {
                                Hide();
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("The file 'param.txt' was not found. Please create it in the same directory as the executable and add 'HIDE=true' to hide the window on startup.", "SynLight", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

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

        #region System tray
        System.Windows.Forms.NotifyIcon m_notifyIcon;
        System.Windows.WindowState m_storedWindowState = 0;
        private void InitializeSystemTray()
        {
            m_notifyIcon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.BalloonTipText = "SynLight has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "SynLight";
            m_notifyIcon.Text = "SynLight";
            var iconPath = Path.Combine(AppContext.BaseDirectory, "SY.ico");
            m_notifyIcon.Icon = new Icon(iconPath);
            m_notifyIcon.Visible = true;
            m_notifyIcon.Click += m_notifyIcon_Click;
        }
        void OnClose(object sender, CancelEventArgs args)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }
        void OnStateChanged(object sender, EventArgs args)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                Hide();

                if (m_notifyIcon != null)
                {
                    m_notifyIcon.ShowBalloonTip(2000);
                }
            }
            else
            {
                m_storedWindowState = WindowState;
            }
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
            {
                m_notifyIcon.Visible = show;
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _dropDownButton.IsOpen = false;
        }
        private void PositiveNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}