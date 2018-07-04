using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using KartaMobiExporter.Core.Annotations;
using KartaMobiExporter.Core.Db;
using KartaMobiExporter.Core.Dto;
using KartaMobiExporter.Core.KartaMobi;
using KartaMobiExporter.Core.Log;
using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Core
{
    public class Communication : INotifyPropertyChanged
    {

        Timer _timer = new Timer();
        DbSqlite _dbSqlite;
        DbData _dbData;

        private OptionDDS _optionDDS;
        private OptionKartaMobi _optionKartaMobi;

        public Communication()
        {
            _timer.Elapsed += OnTimedEvent;
            _timer.Interval = 5000;

            State = EnumState.Disabled;
        }


        /// <summary>
        /// Запустить работу 
        /// </summary>
        public void Start()
        {
            _dbSqlite = new DbSqlite();

            var _optionDDS = _dbSqlite.GetOptionDDS();
            var _optionKartaMobi = _dbSqlite.GetOptionKartaMobi();
            var restApiClient = new RestApiClient(_dbData, _optionKartaMobi);

            //Проверяю все ли настройки заполнены
            if (!_optionDDS.IsSettingCompleted() && !_optionKartaMobi.IsSettingCompleted())
            {
                State = EnumState.ErrorOption;
                return;
            }

            _dbData = new DbData(_dbSqlite.GetOptionDDS());
            //Проверяю возможность подключения к серверу DDS
            if (!_dbData.CheckConnection())
            {
                State = EnumState.ErrorDDSConnection;
                return;
            }

            //Проверяю есть ли подключение к Karta.Mobi
            if (!restApiClient.CheckConnection())
            {
                State = EnumState.ErrorKartaMobiConnection;
                return;
            }

            _timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            State = EnumState.Start;
            DoIt();
            State = EnumState.Done;
        }

        /// <summary>
        /// Остановить работу
        /// </summary>
        public void Stop()
        {
            _timer.Enabled = false;

            State = EnumState.Stop;
        }


        private void DoIt()
        {
            try
            {
                LogWriter.Write(DateTime.Now + " ***Начало импорта***");

                var restApiClient = new RestApiClient(_dbData, _optionKartaMobi);

                var allTransactionForSession = _dbData.GetTransactionFromDb(new DbSqlite().GetErrorTransactions());

                //Обработать транзакции (в том числе "сбойные")
                ProcessTransactions(allTransactionForSession, restApiClient);

                restApiClient.CheckBalanceOnAllCard();

                LogWriter.Write(DateTime.Now + " ***Окончание импорта***");
            }
            catch (Exception e)
            {
                LogWriter.Write("[Error] " + e.Message + " Stack = " + e.StackTrace);
            }
        }

        /// <summary>
        /// Обработать транзакции
        /// </summary>
        /// <param name="transactions">список транзакций</param>
        /// <param name="restApiClient">клиент Rest API Karta.Mobi</param>
        private static void ProcessTransactions(IEnumerable<TransactionInfo> transactions, RestApiClient restApiClient)
        {
            foreach (var transaction in transactions)
            {
                LogWriter.Write("[*] " + transaction.CardNumber +
                                " -----------------------------------------------------");
                if (!string.IsNullOrEmpty(transaction.PhoneNumber))
                {
                    var uToken = restApiClient.GetUTokenClient(transaction.PhoneNumber);
                    if (!string.IsNullOrEmpty(uToken) && transaction.CardNumber.Length <= 19 && transaction.Amount > 0)
                    {
                        restApiClient.SetNumberCard(transaction, uToken);
                        switch (transaction.TypeTransaction)
                        {
                            case TypeTransaction.InCard:
                                restApiClient.SetAmountInCard(transaction, uToken);
                                break;
                            case TypeTransaction.AddToCard:
                                restApiClient.SetAmountInCard(transaction, uToken);
                                break;
                            case TypeTransaction.OutCard:
                                restApiClient.SetAmountOutCard(transaction, uToken);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        LogWriter.Write("[Error] Ошибка получения u_token " + transaction.PhoneNumber);
                    }
                }
                else
                {
                    LogWriter.Write("[Error] Не привязан номер телефона к карте " + transaction.CardNumber);
                }
            }
        }

        public enum EnumState
        {
            Disabled,
            Start,
            Done,
            Stop,
            ErrorOption,
            ErrorDDSConnection,
            ErrorKartaMobiConnection,
            ErrorRunTime
        }

        private EnumState _state;

        public EnumState State
        {
            get => _state;
            set
            {
                if (value == _state) return;
                _state = value;
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