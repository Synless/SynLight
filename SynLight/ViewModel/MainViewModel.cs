using SynLight.Model;
using System.ComponentModel;
using System.Windows.Input;

namespace SynLight.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
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

        #region ICommand declaration
        public ICommand CommandPlay         { get; set; }
        public ICommand AddLongeur          { get; set; }
        public ICommand RemoveLongeur       { get; set; }
        public ICommand AddHauteur          { get; set; }
        public ICommand RemoveHauteur       { get; set; }
        public ICommand AddShifting         { get; set; }
        public ICommand RemoveShifting      { get; set; }
        #endregion

        public MainViewModel()
        {
            SynLight            = new Process_SynLight();
            CommandPlay         = new Command(ActionPlay);
            AddLongeur          = new Command(IncrementerLongeur);
            RemoveLongeur       = new Command(DecrementerLongeur);
            AddHauteur          = new Command(IncrementerHauteur);
            RemoveHauteur       = new Command(DecrementerHauteur);
            AddShifting         = new Command(IncrementerShifting);
            RemoveShifting      = new Command(DecrementerShifting);
        }

        #region Command
        private void ActionPlay(object parametre)
        {
            //SynLight.PlayPausePushed();
        }
        private void IncrementerLongeur(object parametre)
        {
            SynLight.Width += 1; //DO NOT USE "Width++;"
        }
        private void DecrementerLongeur(object parametre)
        {
            SynLight.Width -= 1;
        }
        private void IncrementerHauteur(object parametre)
        {
            SynLight.Height += 1;
        }
        private void DecrementerHauteur(object parametre)
        {
            SynLight.Height -= 1;
        }
        private void IncrementerShifting(object parametre)
        {
            SynLight.Shifting += 1;
        }
        private void DecrementerShifting(object parametre)
        {
            SynLight.Shifting -= 1;
        }
        #endregion

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
