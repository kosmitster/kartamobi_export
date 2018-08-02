using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Core.Db;
using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Log
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private readonly DbSqlite _dbSqlite;
        private Dispatcher _dispatcher;

        public LogViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _dbSqlite = new DbSqlite();
            Items = new ObservableCollection<LogItem>(_dbSqlite.GetSentTransactions());
        }


        internal void UpdateLogItem()
        {
            _dispatcher.Invoke(new Action(() =>
                        {
                            Items.Clear();
                            foreach (var item in _dbSqlite.GetSentTransactions())
                            {
                                Items.Add(item);
                            }
                            OnPropertyChanged(nameof(Items));
                        }));
        }

        private ObservableCollection<LogItem> _items;
        public ObservableCollection<LogItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

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