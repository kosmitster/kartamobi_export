using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExportToService;
using Hardcodet.Wpf.TaskbarNotification;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Log;
using KartaMobiExporter.Option;
using Prism.Commands;

namespace KartaMobiExporter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly Communication _communication;

        private TaskbarIcon tb;

        public MainWindowViewModel()
        {
           _communication = new Communication();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            Title = "Karta.Mobi exporter ver: " + version;

            TabViewModels = new ObservableCollection<ITabViewModel>
            {
                new OptionViewModel {Header = "Настройки"},
                new LogViewModel {Header = "Журнал"}
            };

            SelectedTabViewModel = TabViewModels[0];

            StartCommand = new DelegateCommand(() => {
                _communication.Start();
            });

            StopCommand = new DelegateCommand(() => {
                _communication.Stop();
            });

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
                OnPropertyChanged();
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

        public DelegateCommand StartCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}