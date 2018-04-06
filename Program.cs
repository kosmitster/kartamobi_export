using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ExportToService
{
    class Program
    {
        static void Main(string[] args)
        {


            var restApiClient = new RestApiClient();



            var inCardBonuses = new List<Dto>();
            var outCardBonuses = new List<Dto>();

            using (var sqlConnection = new SqlConnection(@"Integrated Security=true;Initial Catalog=ddd;Data Source=localhost;"))
            {
                sqlConnection.Open();

                inCardBonuses = GetTransaction(sqlConnection, TypeBonus.InCard);
                outCardBonuses = GetTransaction(sqlConnection, TypeBonus.OutCard);
            }

            Console.ReadKey();
        }

        /// <summary>
        /// ПолучитьДвиженияБонусовНаКарту
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <param name="typeBonus">Тип бонуса</param>
        /// <returns></returns>
        private static List<Dto> GetTransaction(SqlConnection sqlConnection, TypeBonus typeBonus)
        {
            var exportData = new List<Dto>();
            var testCmd = new SqlCommand
                ("ds_GetTransactions", sqlConnection)
                {CommandType = CommandType.StoredProcedure};

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var transactionType = testCmd.Parameters.Add("@TransactionType", SqlDbType.Int, (int) typeBonus);
            transactionType.Direction = ParameterDirection.Input;
            //code.Value = cardNumber;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                exportData.Add(new Dto
                {
                    CardId = (string)myReader["CardID"],
                    Amount = (decimal)myReader["Sum"],
                }); 
            }
            myReader.Close();

            //заполняем дополнительные реквизиты
            foreach (var dto in exportData)
            {
                dto.CardNumber = GetCardNumberByCardId(sqlConnection, dto.CardId);
                dto.PhoneNumber = GetPhoneByCard(sqlConnection, dto.CardNumber);
                dto.Balance = GetBalanceCardByCardId(sqlConnection, dto.CardId);
            }

            return exportData;
        }

        /// <summary>
        /// ПолучитьТелефонПользователяКарты
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <param name="cardNumber">номер карты</param>
        /// <returns></returns>
        private static string GetPhoneByCard(SqlConnection sqlConnection, string cardNumber)
        {
            string phoneNumber = string.Empty;

            var testCmd = new SqlCommand
                ("ds_GetCardByCode", sqlConnection) {CommandType = CommandType.StoredProcedure};

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var code = testCmd.Parameters.Add("@Code", SqlDbType.VarChar, 100);
            code.Direction = ParameterDirection.Input;
            code.Value = cardNumber; 
            
            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                phoneNumber = (string) myReader["PhoneNumber"];
            }
            myReader.Close();

            return phoneNumber;
        }

        /// <summary>
        /// ПолучитьКодКартыПоУникальномуИдентификаторуКарты
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <param name="idCard">уникальный идентификатор карты</param>
        /// <returns></returns>
        private static string GetCardNumberByCardId(SqlConnection sqlConnection, string idCard)
        {
            string codeNumber = string.Empty;

            var testCmd = new SqlCommand
                ("ds_GetCard", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var cardId = testCmd.Parameters.Add("@CardID", SqlDbType.VarChar, 38);
            cardId.Direction = ParameterDirection.Input;
            cardId.Value = idCard;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                codeNumber = (string)myReader["Code"];
            }
            myReader.Close();

            return codeNumber;
        }

        /// <summary>
        /// ПолучитьБалансКарты
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <param name="idCard">уникальный идентификатор карты</param>
        /// <returns></returns>
        private static decimal GetBalanceCardByCardId(SqlConnection sqlConnection, string idCard)
        {
            decimal balance = 0;

            var testCmd = new SqlCommand
                ("ds_GetCard", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var cardId = testCmd.Parameters.Add("@CardID", SqlDbType.VarChar, 38);
            cardId.Direction = ParameterDirection.Input;
            cardId.Value = idCard;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                balance = (decimal)myReader["Balance"];
            }
            myReader.Close();

            return balance;
        }

        public enum TypeBonus { InCard = 7, OutCard = 2 }

    }
}
