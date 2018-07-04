using System.ComponentModel;
using System.Runtime.CompilerServices;
using KartaMobiExporter.Annotations;
using KartaMobiExporter.Core.Db;
using KartaMobiExporter.Dto;
using Prism.Commands;

namespace KartaMobiExporter.Option
{
    public class OptionViewModel : INotifyPropertyChanged
    {
        private readonly DbSqlite _dbSqlite;
        public OptionViewModel()
        {
            _dbSqlite = new DbSqlite();
            Option = _dbSqlite.GetOptionDDS();
            OptionKartaMobi = _dbSqlite.GetOptionKartaMobi();

            SaveCommand = new DelegateCommand(Save);
        }

        private void Save()
        {
            _dbSqlite.SetOptionDDS(Option);
            _dbSqlite.SetOptionKartaMobi(OptionKartaMobi);
        }
        
        public DelegateCommand SaveCommand { get; set; }

        public OptionDDS Option { get; set; }
        public OptionKartaMobi OptionKartaMobi { get; set; }

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