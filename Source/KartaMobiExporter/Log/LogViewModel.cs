using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExportToService.Db;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Log
{
    public class LogViewModel : INotifyPropertyChanged, ITabViewModel
    {
        readonly DbSqlite _dbSqlite;

        public LogViewModel()
        {
            _dbSqlite = new DbSqlite();
            Items = new ObservableCollection<LogItem>(UpdateLogItem());
        }


        private List<LogItem> UpdateLogItem()
        {
            return _dbSqlite.GetSentTransactions();
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