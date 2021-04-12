using SynLight.Model;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace SynLight.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Model.Startup.StartOrKill();
            if (Model.Startup.ShowOrHide())
                Hide();

            SetLanguageDictionary();
            InitializeComponent();
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

        protected override void OnClosed(EventArgs e)
        {
            Model.Param_SynLight.Close();
            base.OnClosed(e);
            Environment.Exit(0);
        }

        private void PositiveNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"\-*\d+");
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
            Param_SynLight.debug = true;
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

        // State change\\\\\\\
        private void MainWindowStateChangeRaised(object sender, EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness(8);
                //MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness(0);
                //MaximizeButton.Visibility = Visibility.Visible;
            }
        }
    }
}
