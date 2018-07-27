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
        private Communication Communication;

        public MainWindowViewModel()
        {
            _taskbarIcon = new TaskbarIcon { Icon = Resources.FreeIconExample, ToolTipText = "Karta.Mobi Exporter" };
            Communication = new Communication();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            Title = "Karta.Mobi exporter ver: " + version;

            OptionViewModel = new OptionViewModel();
            LogViewModel = new LogViewModel();

            StartCommand = new DelegateCommand(() =>
            {
                Communication.Start();
            }, () => !execute);

            StopCommand = new DelegateCommand(() =>
            {
                Communication.Stop();
            }, () => execute);

            Communication.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "State")
                {
                    //Оповещаю при изменении состояния
                    Notify(((Communication)sender).State);
                    //Если синхронизация выполнена обновляю логи
                    if (((Communication)sender).State == Communication.EnumState.Start)
                        LogViewModel.UpdateLogItem();

                    RefreshCommand(!execute);
                }
            };

            StartCommand.Execute();
        }

        private bool execute => (Communication.State == Communication.EnumState.Start || Communication.State == Communication.EnumState.Working);

        /// <summary>
        /// Взбодрить комманды
        /// </summary>
        private void RefreshCommand(bool value)
        {
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();

            OptionViewModel.RefreshCommand(value);
        }

        /// <summary>
        /// Оповестить в System Tray 
        /// </summary>
        /// <param name="state">Состояние</param>
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
                default:
                    //_taskbarIcon.ShowBalloonTip(Title, state.ToString(), BalloonIcon.Warning);
                    break;
            }
        }

        private LogViewModel _logViewModel;
        public LogViewModel LogViewModel
        {
            get => _logViewModel;
            set
            {
                if (Equals(value, _logViewModel)) return;
                _logViewModel = value;
                OnPropertyChanged();
            }
        }

        private OptionViewModel _optionViewModel;
        public OptionViewModel OptionViewModel
        {
            get => _optionViewModel;
            set
            {
                if (Equals(value, _optionViewModel)) return;
                _optionViewModel = value;
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

        private bool _isWork;
        public bool IsWork { get => _isWork; set  { if (value == _isWork) return; _isWork = value; OnPropertyChanged(); } }

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