
using SynLight.Model;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace SynLight.View
{
    public partial class MainWindow : Window
    {



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

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
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
            //Param_SynLight.Close();
            //base.OnClosed(e);
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
