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
        private string b_token;
        private string login;
        private string password;

        public RestApiClient()
        {
            b_token = ConfigurationManager.AppSettings["KartaMobi_btoken"];
            login = ConfigurationManager.AppSettings["KartaMobi_login"];
            password = ConfigurationManager.AppSettings["KartaMobi_password"];
        }

        /// <summary>
        /// ПрямоеОбновлениеБонусногоБаланса
        /// </summary>
        /// <param name="dto">информация о движении</param>
        /// <param name="uToken">токен клиента</param>
        private void UpdateBalance(Dto dto, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                var path = "/api/v1/client/balance";
                var answer = ExecuteHttp(path, SerializeBalaceInfoBonus(dto, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    var data = JsonConvert.DeserializeObject<AnswerSetAmount>(answer.Content).data;
                    Log.LogWriter.Write(@"[OK] ПРЯМОЕ ОБНОВЛЕНИЕ БОНУСНОГО БАЛАНСА " + data.u_token + " - " + data.bonuses);
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
            if (answer.StatusCode == HttpStatusCode.OK)
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
        /// ОтправитьНачисленияБонусов
        /// </summary>
        /// <param name="dto">информация о начислении</param>
        /// <param name="uToken">токен клиента</param>
        public void SetAmountInCard(Dto dto, string uToken)
        {
            if (!string.IsNullOrEmpty(uToken))
            {
                UpdateBalance(dto, uToken);
                var path = "/api/v1/market/bonuses/force-accrual";
                var answer = ExecuteHttp(path, SerializeMoveInfoBonus(dto, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    var data = JsonConvert.DeserializeObject<AnswerSetAmount>(answer.Content).data;
                    Log.LogWriter.Write(@"[OK] Произведено начисление бонуса " + data.u_token + " на сумму " + dto.Amount);
                }
                else
                {
                    Log.LogWriter.Write(@"[Error] При начисленнии произошла ошибка " + answer.StatusCode + " " + answer.Content);
                }
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
                UpdateBalance(dto, uToken);
                var path = "/api/v1/market/bonuses/force-writeoff";
                var answer = ExecuteHttp(path, SerializeMoveInfoBonus(dto, uToken));
                if (answer.StatusCode == HttpStatusCode.OK && (bool)JObject.Parse(answer.Content)["status"])
                {
                    var data = JsonConvert.DeserializeObject<AnswerSetAmount>(answer.Content).data;
                    Log.LogWriter.Write(@"[OK] Произведено списание бонуса " + data.u_token + " на сумму " + dto.Amount);                    
                }
                else
                {
                    Log.LogWriter.Write(@"[Error] При списании произошла ошибка " + dto.PhoneNumber + " " +
                                        (string) JObject.Parse(answer.Content)["message"]);
                }
            }
        }

        /// <summary>
        /// СериализоватьИнформациюОДвиженииБонусов
        /// </summary>
        /// <param name="dto">информация о  движении</param>
        /// <param name="uToken">токен клиента</param>
        /// <returns></returns>
        private string SerializeMoveInfoBonus(Dto dto, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoCardBonus
            {
                b_token = b_token,
                u_token = uToken,
                order_id = "1",
                bonuses = dto.Amount.ToString(CultureInfo.InvariantCulture)
            });
        }

        /// <summary>
        /// СериализоватьИнформациюООст
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="uToken"></param>
        /// <returns></returns>
        private string SerializeBalaceInfoBonus(Dto dto, string uToken)
        {
            return JsonConvert.SerializeObject(new InfoCardBonusBalance
            {
                b_token = b_token,
                u_token = uToken,
                summ = dto.Amount.ToString(CultureInfo.InvariantCulture),
                total = dto.Balance.ToString(CultureInfo.InvariantCulture)
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
