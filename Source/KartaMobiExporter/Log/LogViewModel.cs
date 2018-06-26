using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Model;

namespace KartaMobiExporter.Log
{
    public class LogViewModel : INotifyPropertyChanged, ITabViewModel
    {
        public LogViewModel()
        {

            Items = new ObservableCollection<LogItem>();
            Items.Add(new LogItem
            {
                Card = "123456",
                Phone = "+79151407306",
                Date = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                Amount = 500,
                Result = "Отправлено"

            });
        }


        private ObservableCollection<LogItem> _items;
        public ObservableCollection<LogItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged(nameof(Items));
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
                OnPropertyChanged(nameof(Header));
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