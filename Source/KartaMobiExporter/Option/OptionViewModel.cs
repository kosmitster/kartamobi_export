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
        private bool _canExecute = true;
        private readonly DbSqlite _dbSqlite;
        public OptionViewModel()
        {
            _dbSqlite = new DbSqlite();
            Option = _dbSqlite.GetOptionDDS();
            OptionKartaMobi = _dbSqlite.GetOptionKartaMobi();

            SaveCommand = new DelegateCommand(Save, () => _canExecute);
        }

        private void Save()
        {
            _dbSqlite.SetOptionDDS(Option);
            _dbSqlite.SetOptionKartaMobi(OptionKartaMobi);
        }
        
        /// <summary>
        /// Взбодрить комманду 
        /// </summary>
        /// <param name="value"></param>
        public void RefreshCommand(bool value)
        {
            _canExecute = value;
            SaveCommand.RaiseCanExecuteChanged();
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