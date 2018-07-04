using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hardcodet.Wpf.TaskbarNotification;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Core;
using KartaMobiExporter.Log;
using KartaMobiExporter.Option;
using KartaMobiExporter.Properties;
using Prism.Commands;

namespace KartaMobiExporter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly TaskbarIcon _taskbarIcon;

        public MainWindowViewModel()
        {
            _taskbarIcon = new TaskbarIcon {Icon = Resources.Roundicons_100_Free_Solid_Care_for_recycling , ToolTipText = "Karta.Mobi Exporter"};
            var communication = new Communication();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            Title = "Karta.Mobi exporter ver: " + version;

            TabViewModels = new ObservableCollection<ITabViewModel>
            {
                new OptionViewModel {Header = "Настройки"},
                new LogViewModel {Header = "Журнал"}
            };

            SelectedTabViewModel = TabViewModels[0];

            StartCommand = new DelegateCommand(() => {
                communication.Start();
            });

            StopCommand = new DelegateCommand(() => {
                communication.Stop();
            });

            communication.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "State")
                {
                    Notify(((Communication)sender).State);
                }
            };

        }

        private void Notify(Communication.EnumState state)
        {
            switch (state)
            {
                case Communication.EnumState.ErrorOption:
                    _taskbarIcon.ShowBalloonTip(Title, "Выполните настройку!", BalloonIcon.Error);
                    break;
                case Communication.EnumState.ErrorDDSConnection:
                    _taskbarIcon.ShowBalloonTip(Title, "Ошибка подключения к серверу DDS!", BalloonIcon.Error);
                    break;
                case Communication.EnumState.ErrorKartaMobiConnection:
                    _taskbarIcon.ShowBalloonTip(Title, "Ошибка подключения к Karta.Mobi!", BalloonIcon.Error);
                    break;
                case Communication.EnumState.Done:
                    _taskbarIcon.ShowBalloonTip(Title, "Синхронизация выполнена!", BalloonIcon.Error);
                    break;
                default:
                    _taskbarIcon.ShowBalloonTip(Title, state.ToString(), BalloonIcon.Info);
                    break;
            }            
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