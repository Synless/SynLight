using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;

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
            Model.Startup.CleanFiles();
            if (Model.Startup.ShowOrHide())
                Hide();
            SetLanguageDictionary();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            //DataContext = new MainViewModel(); //ALREADY SET IN XAML
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
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}