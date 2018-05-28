using System;
using System.Collections.Generic;
using System.Linq;
using ExportToService.Db;
using ExportToService.Dto;
using ExportToService.KartaMobi;
using ExportToService.Log;

namespace ExportToService
{
    class Program
    {
        static void Main()
        {
            try
            {
                LogWriter.Write(DateTime.Now + " ***Начало импорта***");

                var dbData = new DbData();

                var restApiClient = new RestApiClient(dbData);

                var allTransactionForSession = dbData.GetTransactionFromDb(new DbSqlite().GetErrorTransactions());

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
                if (!string.IsNullOrEmpty(transaction.PhoneNumber))
                {
                    var uToken = restApiClient.GetUTokenClient(transaction.PhoneNumber);
                    if (!string.IsNullOrEmpty(uToken))
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