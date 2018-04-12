using System;
using System.Collections.Generic;
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

                var restApiClient = new RestApiClient();

                var dbData = new DbData();

                //Обработать транзакции (в том числе "сбойные")
                ProcessTransactions(dbData.GetTransactionFromDb(new DbSqlite().GetErrorTransactions()), restApiClient);

                LogWriter.Write(DateTime.Now + " ***Окончание импорта***");
            }
            catch (Exception e)
            {
                LogWriter.Write("[Error] " + e.Message);
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
                        switch (transaction.TypeBonus)
                        {
                            case TypeBonus.InCard:
                                restApiClient.SetAmountInCard(transaction, uToken);
                                break;
                            case TypeBonus.OutCard:
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