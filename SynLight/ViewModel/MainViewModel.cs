using SynLight.Model;
using System.ComponentModel;
using System.Windows.Input;

namespace SynLight.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public Process_SynLight synLight = new Process_SynLight();
        public Process_SynLight SynLight
        {
            get
            {
                return synLight;
            }
            set
            {
                synLight = value;
                OnPropertyChanged("SynLight");
            }
        }

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}