using System;
using System.Collections.Generic;
using System.Threading;
using ExportToService.Db;
using ExportToService.Dto;
using ExportToService.KartaMobi;
using ExportToService.Log;

namespace ExportToService
{
    public class Communication
    {
        Timer _timer;
        DbSqlite _dbSqlite;
        DbData _dbData;

        /// <summary>
        /// Запустить работу 
        /// </summary>
        public void Start()
        {
            _dbSqlite = new DbSqlite();

            var option = _dbSqlite.GetOptionDDS();
            if (!option.IsWork())
            {
                   
            }

            _dbData = new DbData(_dbSqlite.GetOptionDDS());

            

            TimerCallback timeCb = DoIt;

            _timer = new Timer(timeCb, null, 0, 10000);
        }

        /// <summary>
        /// Остановить работу
        /// </summary>
        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void DoIt(object state)
        {
            try
            {
                LogWriter.Write(DateTime.Now + " ***Начало импорта***");

                var restApiClient = new RestApiClient(_dbData);

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
        private static void ProcessTransactions(List<TransactionInfo> transactions, RestApiClient restApiClient)
        {
            foreach (var transaction in transactions)
            {
                LogWriter.Write("[*] "+ transaction.CardNumber + " -----------------------------------------------------");
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
    }
}