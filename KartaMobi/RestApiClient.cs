using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using ExportToService.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace ExportToService.KartaMobi
{
    public class RestApiClient
    {
        private readonly string b_token;
        private readonly string login;
        private readonly string password;

        public RestApiClient()
        {
            b_token = ConfigurationManager.AppSettings["KartaMobi_btoken"];
            login = ConfigurationManager.AppSettings["KartaMobi_login"];
            password = ConfigurationManager.AppSettings["KartaMobi_password"];
        }

        /// <summary>
        /// ПрямоеОбновлениеБонусногоБаланса
        /// </summary>
        /// <param name="balance">остаток на карте</param>
        /// <param name="uToken">токен клиента</param>
        private void UpdateBalance(decimal balance, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                var path = "/api/v1/client/balance";
                var answer = ExecuteHttp(path, SerializeBalaceInfoBonus(balance, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] ПРЯМОЕ ОБНОВЛЕНИЕ БОНУСНОГО БАЛАНСА " + balance +
                                        " - результат на сервере bonuses=" + (string) JObject.Parse(answer.Content)["data"]["bonuses"] +
                                        " - результат на сервере delta=" + (string)JObject.Parse(answer.Content)["data"]["delta"]);
                }
                else
                {
                    Log.LogWriter.Write(@"[Error] Ошибка ПРЯМОЕ ОБНОВЛЕНИЕ БОНУСНОГО БАЛАНСА " + answer.StatusCode + " " + answer.Content);
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
            var path = "/api/v1/client/get-utoken-by-phone?" + "b_token=" + b_token + "&" + "phone=" + phone;
            var answer = ExecuteHttp(path);
            if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
            {
                var data = JsonConvert.DeserializeObject<AnswerToken>(answer.Content).data;
                uToken = data.u_token;
                Log.LogWriter.Write(@"[OK] Получение U_TOKEN клиента по телефону " + data.u_token + " " + phone);
            }
            else
            {
                Log.LogWriter.Write(@"[Error] Ошибка Получение U_TOKEN клиента по телефону " + answer.StatusCode + " " + answer.Content);
            }

            return uToken;
        }


        /// <summary>
        /// Отправка номера карты клиента
        /// </summary>
        /// <returns></returns>
        public void SetNumberCard(Dto dto, string uToken)
        {
            var path = "/api/v1/cards/number";
            var answer = ExecuteHttp(path, SerializeInfoCard(dto, uToken));
            if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
            {
                Log.LogWriter.Write(@"[OK] Обновление номера карты клиента " + dto.CardNumber);
            }
            else
            {
                Log.LogWriter.Write(@"[Error] Ошибка Обновление номера карты клиента " + JObject.Parse(answer.Content)["status"]);
            }
        }

        /// <summary>
        /// ОтправитьНачисленияБонусов
        /// </summary>
        /// <param name="dto">информация о начислении</param>
        /// <param name="uToken">токен клиента</param>
        public void SetAmountInCard(Dto dto, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                var path = "/api/v1/market/bonuses/force-accrual";
                var answer = ExecuteHttp(path, SerializeMoveBonusInCard(dto, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] Произведено начисление бонуса " + dto.PhoneNumber + 
                        " на сумму " + dto.Amount + 
                        " - на сервере bonuses = " + (string) JObject.Parse(answer.Content)["data"]["bonuses"]);
                }
                else
                {
                    Log.LogWriter.Write(@"[Error] При начисленнии произошла ошибка телефон=" + dto.PhoneNumber +
                                        " сумма=" + dto.Amount + 
                                        " - сообщение сервиса=" + (string) JObject.Parse(answer.Content)["message"]);
                }
                UpdateBalance(dto.Balance + dto.Amount, uToken);
            }
        }

        /// <summary>
        /// ОтправитьСписанияБонусов
        /// </summary>
        /// <param name="dto">информация о списании</param>
        /// <param name="uToken">токен клиента</param>
        public void SetAmountOutCard(Dto dto, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                UpdateBalance(dto.Balance, uToken);
                var path = "/api/v1/market/bonuses/force-writeoff";
                var answer = ExecuteHttp(path, SerializeMoveBonusOutCard(dto, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    Log.LogWriter.Write(@"[OK] Произведено списание бонуса " + dto.PhoneNumber + 
                        " на сумму " + dto.Amount + 
                        " - на сервере bonuses = " + (string) JObject.Parse(answer.Content)["data"]["bonuses"] +
                        " - на сервере writeoff = " + (string) JObject.Parse(answer.Content)["data"]["writeoff"]);
                }
                else
                {
                    Log.LogWriter.Write(@"[Error] При списании произошла ошибка телефон=" + dto.PhoneNumber +
                                        " сумма=" + dto.Amount + 
                                        " - сообщение сервиса=" + (string) JObject.Parse(answer.Content)["message"]);
                }                
            }
        }

        /// <summary>
        /// СериализоватьИнформациюОДвиженииНачисленияБонусов
        /// </summary>
        /// <param name="dto">информация о  движении</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeMoveBonusInCard(Dto dto, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusInCard
            {
                b_token = b_token,
                u_token = uToken,
                bonuses = dto.Amount.ToString(CultureInfo.InvariantCulture)
            });
        }

        /// <summary>
        /// СериализоватьИнформациюОДвиженииСписанияБонусов
        /// </summary>
        /// <param name="dto">информация о  движении</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeMoveBonusOutCard(Dto dto, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusOutCard
            {
                b_token = b_token,
                u_token = uToken,
                bonuses = dto.Amount.ToString(CultureInfo.InvariantCulture)
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
                b_token = b_token,
                u_token = uToken,
                total = balance.ToString(CultureInfo.InvariantCulture)
            });
        }

        private string SerializeInfoCard(Dto dto, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoBonusCard
            {
                b_token = b_token,
                u_token = uToken,
                card_num = dto.CardNumber
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
            var client = new RestClient
            {
                BaseUrl = new Uri("http://dev.karta.mobi"),
                Authenticator = new HttpBasicAuthenticator(login, password)
            };
            
            var request = new RestRequest(path);
            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.Method = Method.POST;
                request.AddParameter("application/json; charset=utf-8",  jsonBody, ParameterType.RequestBody);
            }
            request.AddHeader("Content-Type", "application/json");

            return client.Execute(request);
        }
    }
}
