using System;
using System.Globalization;
using ExportToService.JSON;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace ExportToService
{
    public class RestApiClient
    {
        private string b_token;
        private string login;
        private string password;

        public RestApiClient()
        {

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
                var path = "/api/v1/client/balance/";
                var x = ExecuteHttp(path, SerializeBalaceInfoBonus(dto, uToken));
            }
        }

        /// <summary>
        /// ПолучитьТокенКлиента
        /// </summary>
        /// <param name="phone">номер телефона</param>
        /// <returns></returns>
        public string GetUTokenClient(string phone)
        {
            var path = "/api/v1/client/get-utoken-by-phone?" + "b_token=" + b_token + "&" + "phone=" +
                       phone;
            return JsonConvert.DeserializeObject<AnswerToken>(ExecuteHttp(path)?.Content)?.data.u_token;
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
                var x = ExecuteHttp(path, SerializeMoveInfoBonus(dto, uToken));
                UpdateBalance(dto, uToken);
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
                var path = "/api/v1/market/bonuses/force-writeoff";
                var x = ExecuteHttp(path, SerializeMoveInfoBonus(dto, uToken));
                UpdateBalance(dto, uToken);
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
