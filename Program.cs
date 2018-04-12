using System;
using System.Collections.Generic;
using ExportToService.Db;
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

                List<Dto> inCardBonuses;
                List<Dto> outCardBonuses;

                DbData.GetData(out inCardBonuses, out outCardBonuses);

                foreach (var inCard in inCardBonuses)
                {
                    if (!string.IsNullOrEmpty(inCard.PhoneNumber))
                    {
                        var uToken = restApiClient.GetUTokenClient(inCard.PhoneNumber);
                        if (!string.IsNullOrEmpty(uToken))
                        {
                            restApiClient.SetNumberCard(inCard, uToken);
                            restApiClient.SetAmountInCard(inCard, uToken);
                        }
                        else
                        {
                            LogWriter.Write("[Error] Ошибка получения u_token " + inCard.PhoneNumber);
                        }
                    }
                    else
                    {
                        LogWriter.Write("[Error] Не привязан номер телефона к карте " + inCard.CardNumber);
                    }
                }

                foreach (var outCard in outCardBonuses)
                {
                    if (!string.IsNullOrEmpty(outCard.PhoneNumber))
                    {
                        var uToken = restApiClient.GetUTokenClient(outCard.PhoneNumber);
                        if (!string.IsNullOrEmpty(uToken))
                        {
                            restApiClient.SetNumberCard(outCard, uToken);
                            restApiClient.SetAmountOutCard(outCard, uToken);
                        }
                        else
                        {
                            LogWriter.Write("[Error] Ошибка получения u_token " + outCard.PhoneNumber);
                        }
                    }
                    else
                    {
                        LogWriter.Write("[Error] Не привязан номер телефона к карте " + outCard.CardNumber);
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