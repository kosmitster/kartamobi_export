using System;
using RestSharp;
using RestSharp.Authenticators;

namespace ExportToService
{
    public class RestApiClient
    {
        private string b_token = "";
        private string login = "";
        private string password = "";

        public RestApiClient()
        {
            var path = "/api/v1/client/get-utoken-by-phone?" + "b_token=" + b_token + "&" + "phone=" +
                       "79151407345";

            GetHttp(path);
        }


        private void GetUTokenClient()
        {
            
        }


        private IRestResponse GetHttp(string path)
        {
            var client = new RestClient
            {
                BaseUrl = new Uri("http://dev.karta.mobi"),
                Authenticator = new HttpBasicAuthenticator(login, password)
            };
            
            var request = new RestRequest(path);
            request.AddHeader("Connection", "Keep-Alive");
            request.AddHeader("Content-Language", "ru");
            request.AddHeader("Accept-Language", "ru");
            request.AddHeader("Content-Type", "application/json charset=UTF-8");
            request.AddHeader("Accept", "application/json");

            return client.Execute(request);
        }
    }
}
