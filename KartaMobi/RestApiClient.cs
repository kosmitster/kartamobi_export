using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using ExportToService.Db;
using ExportToService.Dto;
using ExportToService.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace ExportToService.KartaMobi
{
    /// <summary>
    /// Клиент Rest API Karta.Mobi
    /// </summary>
    public class RestApiClient
    {
        private readonly string _bToken;
        private readonly string _login;
        private readonly string _password;
        private readonly DbSqlite _dbSqlite;
        private readonly DbData _dbData;
        private readonly List<BalanceInServiceInfo> _inServiceBalanses;

        /// <summary>
        /// Создать клиента Rest API Karta.Mobi
        /// </summary>
        /// <param name="dbData"></param>
        public RestApiClient(DbData dbData)
        {
            _dbData = dbData;
            _dbSqlite = new DbSqlite();

            _bToken = ConfigurationManager.AppSettings["KartaMobi_btoken"];
            _login = ConfigurationManager.AppSettings["KartaMobi_login"];
            _password = ConfigurationManager.AppSettings["KartaMobi_password"];

            _inServiceBalanses = new List<BalanceInServiceInfo>();
        }

        /// <summary>
        /// ПрямоеОбновлениеБонусногоБаланса
        /// </summary>
        /// <param name="balance">остаток на карте</param>
        /// <param name="uToken">токен клиента</param>
        /// <param name="phoneNumber">номер телефона клиента</param>
        private void UpdateBalance(decimal balance, string uToken, string phoneNumber)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                var path = "/api/v1/client/balance";
                var answer = ExecuteHttp(path, SerializeBalaceInfoBonus(balance, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool) JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] ПРЯМОЕ ОБНОВЛЕНИЕ БОНУСНОГО БАЛАНСА " + balance +
                        " - телефон = " + phoneNumber +
                        " - результат на сервере bonuses = " + (string) JObject.Parse(answer.Content)["data"]["bonuses"] +
                        " - результат на сервере delta = " + (string) JObject.Parse(answer.Content)["data"]["delta"]);
                }
                else
                {
                    Log.LogWriter.Write(@"[ERROR] Ошибка ПРЯМОЕ ОБНОВЛЕНИЕ БОНУСНОГО БАЛАНСА " + answer.StatusCode +
                                        " " + answer.Content);
                }
            }
        }

        /// <summary>
        /// ПолучитьТокенКлиента
        /// </summary>
        /// <param name="phone">номер телефона</param>
        /// <returns></returns>
        public string GetUTokenClient(string phone)
        {
            string uToken = string.Empty;
            var path = "/api/v1/client/get-utoken-by-phone?" + "b_token=" + _bToken + "&" + "phone=" + phone;
            var answer = ExecuteHttp(path);
            if (answer.StatusCode == HttpStatusCode.OK && (bool) JObject.Parse(answer.Content)["status"])
            {
                var data = JsonConvert.DeserializeObject<AnswerToken>(answer.Content).data;
                uToken = data.u_token;
                Log.LogWriter.Write(@"[OK] Получение U_TOKEN клиента по телефону " + data.u_token + " " + phone);
            }
            else
            {
                Log.LogWriter.Write(@"[ERROR] Ошибка Получение U_TOKEN клиента по телефону " + phone + " answer " +
                                    JObject.Parse(answer.Content));
            }

            return uToken;
        }


        /// <summary>
        /// Отправка номера карты клиента
        /// </summary>
        public void SetNumberCard(TransactionInfo transactionInfo, string uToken)
        {
            var path = "/api/v1/cards/number";
            var answer = ExecuteHttp(path, SerializeInfoCard(transactionInfo, uToken));
            if (answer.StatusCode == HttpStatusCode.OK && (bool) JObject.Parse(answer.Content)["status"])
            {
                Log.LogWriter.Write(@"[OK] Обновление номера карты клиента " + transactionInfo.CardNumber);
            }
            else
            {
                Log.LogWriter.Write(@"[ERROR] Ошибка Обновление номера карты клиента " +
                                    SerializeInfoCard(transactionInfo, uToken) + " answer" +
                                    JObject.Parse(answer.Content));
            }
        }

        /// <summary>
        /// ОтправитьНачисленияБонусов
        /// </summary>
        /// <param name="transactionInfo">информация о начислении</param>
        /// <param name="uToken">токен клиента</param>
        public void SetAmountInCard(TransactionInfo transactionInfo, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                var path = "/api/v1/market/bonuses/force-accrual";
                var answer = ExecuteHttp(path, SerializeMoveBonusInCard(transactionInfo, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool) JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] Произведено начисление бонуса " + transactionInfo.PhoneNumber +
                                        " на сумму " + transactionInfo.Amount +
                                        " - на сервере bonuses = " +
                                        (string) JObject.Parse(answer.Content)["data"]["bonuses"]);
                    _dbSqlite.SaveSentTransaction(transactionInfo);
                    AddIntoServiceBalanceInfo(transactionInfo.PhoneNumber, transactionInfo.CardId, uToken,
                        decimal.Parse((string) JObject.Parse(answer.Content)["data"]["bonuses"],
                            new NumberFormatInfo {NumberDecimalSeparator = "."}));
                }
                else
                {
                    _dbSqlite.SaveErrorTransaction(transactionInfo);
                    Log.LogWriter.Write(@"[ERROR] При начисленнии произошла ошибка телефон=" +
                                        transactionInfo.PhoneNumber + " сумма=" +
                                        transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                                        JObject.Parse(answer.Content));
                }
                UpdateBalance(transactionInfo.BalanceOnTransaction + transactionInfo.Amount, uToken, transactionInfo.PhoneNumber);
            }
        }

        /// <summary>
        /// ОтправитьСписанияБонусов
        /// </summary>
        /// <param name="transactionInfo">информация о списании</param>
        /// <param name="uToken">токен клиента</param>
        public void SetAmountOutCard(TransactionInfo transactionInfo, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                UpdateBalance(transactionInfo.BalanceOnTransaction, uToken, transactionInfo.PhoneNumber);
                var path = "/api/v1/market/bonuses/force-writeoff";
                var answer = ExecuteHttp(path, SerializeMoveBonusOutCard(transactionInfo, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool) JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] Произведено списание бонуса " + transactionInfo.PhoneNumber +
                                        " на сумму " + transactionInfo.Amount +
                                        " - на сервере bonuses = " +
                                        (string) JObject.Parse(answer.Content)["data"]["bonuses"] +
                                        " - на сервере writeoff = " +
                                        (string) JObject.Parse(answer.Content)["data"]["writeoff"]);
                    _dbSqlite.SaveSentTransaction(transactionInfo);
                    AddIntoServiceBalanceInfo(transactionInfo.PhoneNumber, transactionInfo.CardId, uToken,
                        decimal.Parse((string) JObject.Parse(answer.Content)["data"]["bonuses"],
                            new NumberFormatInfo {NumberDecimalSeparator = "."}));
                }
                else
                {
                    _dbSqlite.SaveErrorTransaction(transactionInfo);
                    Log.LogWriter.Write(@"[ERROR] При списании произошла ошибка телефон=" +
                                        transactionInfo.PhoneNumber +
                                        " сумма=" + transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                                        JObject.Parse(answer.Content));
                }
            }
        }


        /// <summary>
        /// Сохранить информацию о балансах Karta.Mobi
        /// </summary>
        /// <param name="phoneNumber">номер телефона</param>
        /// <param name="cardId">уникальный идентификатор карты клиента</param>
        /// <param name="uToken">токен клиента</param>
        /// <param name="bonuses">количество бонусов</param>
        private void AddIntoServiceBalanceInfo(string phoneNumber, string cardId, string uToken, decimal bonuses)
        {
            if (_inServiceBalanses.Any(x => x.CardId == cardId))
            {
                _inServiceBalanses.Remove(_inServiceBalanses.Single(x => x.CardId == cardId));
            }

            _inServiceBalanses.Add(new BalanceInServiceInfo
            {
                PhoneNumber = phoneNumber,
                CardId = cardId,
                Bonuses = bonuses,
                UToken = uToken,
                BalanceOnRealTime = _dbData.GetBalanceCardByOnRealTime(cardId)
            });
        }

        /// <summary>
        /// СериализоватьИнформациюОДвиженииНачисленияБонусов
        /// </summary>
        /// <param name="transactionInfo">информация о  движении</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeMoveBonusInCard(TransactionInfo transactionInfo, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusInCard
            {
                order_id = "unknown",
                b_token = _bToken,
                u_token = uToken,
                bonuses = transactionInfo.Amount.ToString(CultureInfo.InvariantCulture)
            });
        }

        /// <summary>
        /// СериализоватьИнформациюОДвиженииСписанияБонусов
        /// </summary>
        /// <param name="transactionInfo">информация о  движении</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeMoveBonusOutCard(TransactionInfo transactionInfo, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusOutCard
            {
                b_token = _bToken,
                u_token = uToken,
                bonuses = transactionInfo.Amount.ToString(CultureInfo.InvariantCulture)
            });
        }


        /// <summary>
        /// СериализоватьИнформациюООстаткахБонусов
        /// </summary>
        /// <param name="balance">Остаток на карте</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeBalaceInfoBonus(decimal balance, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoCardBonusBalance
            {
                b_token = _bToken,
                u_token = uToken,
                total = balance.ToString(CultureInfo.InvariantCulture)
            });
        }

        /// <summary>
        /// СериализоватьИнформациюОКарте
        /// </summary>
        /// <param name="transactionInfo"></param>
        /// <param name="uToken"></param>
        /// <returns></returns>
        private string SerializeInfoCard(TransactionInfo transactionInfo, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusCard
            {
                b_token = _bToken,
                u_token = uToken,
                card_num = transactionInfo.CardNumber
            });
        }

        /// <summary>
        /// ОтправитьЗапросНаСервер
        /// </summary>
        /// <param name="path">путь к методу</param>
        /// <param name="jsonBody">сериализованные данные для отправки</param>
        /// <returns></returns>
        private IRestResponse ExecuteHttp(string path, string jsonBody = null)
        {
            System.Threading.Thread.Sleep(1000);
            var client = new RestClient
            {
                BaseUrl = new Uri("http://dev.karta.mobi"),
                Authenticator = new HttpBasicAuthenticator(_login, _password)
            };

            var request = new RestRequest(path);
            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.Method = Method.POST;
                request.AddParameter("application/json; charset=utf-8", jsonBody, ParameterType.RequestBody);
            }
            request.AddHeader("Content-Type", "application/json");

            return client.Execute(request);
        }

        /// <summary>
        /// Отправить все балансы карт если они не сходятся с информацией из Karta.Mobi
        /// </summary>
        public void CheckBalanceOnAllCard()
        {
            foreach (var balanceInServiceInfo in _inServiceBalanses)
            {
                if (balanceInServiceInfo.Bonuses != balanceInServiceInfo.BalanceOnRealTime)
                {
                    Log.LogWriter.Write("[Warning] Итоговая проверка баланса " +
                                        " PhoneNumber = " + balanceInServiceInfo.PhoneNumber +
                                        " CardID = " + balanceInServiceInfo.CardId +
                                        " balanceInKartaMobi.Bonuses = " + balanceInServiceInfo.Bonuses +
                                        " transactionInfo.BalanceOnRealTime = " +
                                        balanceInServiceInfo.BalanceOnRealTime);
                    UpdateBalance(balanceInServiceInfo.BalanceOnRealTime, balanceInServiceInfo.UToken,
                        balanceInServiceInfo.PhoneNumber);
                }
                else
                {
                    Log.LogWriter.Write("[OK] Итоговая проверка баланса прошла успешно " +
                                        " PhoneNumber = " + balanceInServiceInfo.PhoneNumber +
                                        " CardID = " + balanceInServiceInfo.CardId +
                                        " balanceInKartaMobi.Bonuses = " + balanceInServiceInfo.Bonuses +
                                        " transactionInfo.BalanceOnRealTime = " +
                                        balanceInServiceInfo.BalanceOnRealTime);
                }
            }
        }
    }
}