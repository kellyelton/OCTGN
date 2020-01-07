using System.ComponentModel;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Octgn.Library;

namespace Octgn.Controls
{

    /// <summary>
    /// Interaction logic for UpdateBar.xaml
    /// </summary>
    public partial class UpdateBar : INotifyPropertyChanged
    {
        private string _message;

        public string Message {
            get { return _message; }
            set {
                if (_message == value) return;
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        public UpdateBar()
        {
            InitializeComponent();
            UpdateManager.Current.UpdateAvailable += InstanceOnUpdateAvailable;
            this.Visibility = Visibility.Collapsed;
        }

        private void InstanceOnUpdateAvailable(object sender, UpdateDetails updateDetails)
        {
            Message = String.Format("There is a new version of OCTGN available, {0}.", updateDetails.Version);
            Dispatcher.Invoke(new Action(
                () =>
                    this.Visibility = Visibility.Visible)
                );
            OnPropertyChanged("Message");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RestartClick(object sender, MouseButtonEventArgs e)
        {
            UpdateManager.Current.UpdateAndRestart();
        }
    }
}
