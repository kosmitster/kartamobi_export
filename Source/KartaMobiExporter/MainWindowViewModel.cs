using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Log;
using KartaMobiExporter.Option;

namespace KartaMobiExporter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {

        public MainWindowViewModel()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            Title = "Karta.Mobi exporter ver: " + version;

            TabViewModels = new ObservableCollection<ITabViewModel>
            {
                new OptionViewModel {Header = "Настройки"},
                new LogViewModel {Header = "Журнал"}
            };

            SelectedTabViewModel = TabViewModels[0];
        }

        private ITabViewModel _selectedTabViewModel;
        public ITabViewModel SelectedTabViewModel
        {
            get => _selectedTabViewModel;
            set
            {
                if (Equals(value, _selectedTabViewModel)) return;
                _selectedTabViewModel = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ITabViewModel> _tabViewModels;
        public ObservableCollection<ITabViewModel> TabViewModels
        {
            get => _tabViewModels;
            set
            {
                if (Equals(value, _tabViewModels)) return;
                _tabViewModels = value;
                OnPropertyChanged(nameof(TabViewModels));
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
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