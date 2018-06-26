using System.ComponentModel;
using System.Runtime.CompilerServices;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Model;
using Prism.Commands;

namespace KartaMobiExporter.Option
{
    public class OptionViewModel : INotifyPropertyChanged, ITabViewModel
    {

        public OptionViewModel()
        {
            Option = new OptionItem();
            SaveCommand = new DelegateCommand(Save);
        }

        private void Save()
        {
            
        }


        public DelegateCommand SaveCommand { get; set; }

        public OptionItem Option { get; set; }

        private string _header;
        public string Header
        {
            get => _header;
            set
            {
                if (value == _header) return;
                _header = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}