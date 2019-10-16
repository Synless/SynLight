using SynLight.Model;
using System.ComponentModel;
using System.Windows.Input;

namespace SynLight.ViewModel
{
    public class MainViewModel
    {
        public Process_SynLight synLight;
        public Process_SynLight SynLight
        {
            get
            {
                return synLight;
            }
            set
            {
                synLight = value;
                //OnPropertyChanged("SynLight");
            }
        }

        public MainViewModel()
        {
            SynLight = new Process_SynLight();
        }
    }
}
