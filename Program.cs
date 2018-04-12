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
                var dbSqlite = new DbSqlite();
                dbSqlite.SaveSentTransaction(new TransactionInfo
                {
                    Amount = new decimal(15.2),
                    Balance = 150,
                    PhoneNumber = "79151407306",
                    CardId = "{F504BD18-C0DD-4B10-B9EB-886C81400DAF}",
                    CardNumber = "3214170411931",
                    TypeBonus = TypeBonus.InCard,
                    TransactionDateTime = DateTime.Now,
                    TransactionId = "B2B9DA07-2C5F-4C6A-AC5D-37D1525418E4"
                });
                dbSqlite.SaveErrorTransaction(new TransactionInfo
                {
                    Amount = new decimal(15.2),
                    Balance = 150,
                    PhoneNumber = "79151407306",
                    CardId = "{F504BD18-C0DD-4B10-B9EB-886C81400DAF}",
                    CardNumber = "3214170411931",
                    TypeBonus = TypeBonus.InCard,
                    TransactionDateTime = DateTime.Now,
                    TransactionId = "B2B9DA07-2C5F-4C6A-AC5D-37D1525418E4"
                });                

                LogWriter.Write(DateTime.Now + " ***Начало импорта***");

                var restApiClient = new RestApiClient();

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