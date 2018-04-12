using System;
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

                var errorTransaction = new DbSqlite().GetErrorTransactions();

                var transactions = DbData.GetTransactionFromDb();

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
                LogWriter.Write(DateTime.Now + " ***Окончание импорта***");
            }
            catch (Exception e)
            {
                LogWriter.Write("[Error] " + e.Message);
            }
        }
    }
}