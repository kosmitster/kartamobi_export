using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ExportToService.Db
{
    public static class DbData
    {
        /// <summary>
        /// ПолучитьДанныеОДвижениях
        /// </summary>
        public static List<Dto> GetTransactionFromDb()
        {
            var initCatalog = ConfigurationManager.AppSettings["DB_InitialCatalog"];
            var dataSource = ConfigurationManager.AppSettings["DB_dataSource"];
            var login = ConfigurationManager.AppSettings["DB_login"];
            var password = ConfigurationManager.AppSettings["DB_password"];

            var transactions = new List<Dto>();
            
            using (var sqlConnection = new SqlConnection(@"Integrated Security=True;Trusted_Connection=True;User ID=" + login +
                                                         ";Password=" + password + ";Initial Catalog=" + initCatalog +
                                                         ";Data Source=" + dataSource + ";"))
            {
                sqlConnection.Open();

                transactions.AddRange(GetTransaction(sqlConnection));

                sqlConnection.Close();
            }

            return transactions;
        }

        /// <summary>
        /// ПолучитьТранзакцииПоНачислениюИСписаниюБонусов
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <returns></returns>
        private static IEnumerable<Dto> GetTransaction(SqlConnection sqlConnection)
        {
            var exportData = new List<Dto>();
            var testCmd = new SqlCommand
                ("ds_GetTransactions", sqlConnection)
                {CommandType = CommandType.StoredProcedure};

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramOrderBy = testCmd.Parameters.Add("@OrderBy", SqlDbType.Int);
            paramOrderBy.Direction = ParameterDirection.Input;
            paramOrderBy.Value = 0;

            var paramBeginDate = testCmd.Parameters.Add("@BeginDate", SqlDbType.DateTime);
            paramBeginDate.Direction = ParameterDirection.Input;
            //paramBeginDate.Value = DateTime.Now.AddMinutes(-5);
            paramBeginDate.Value = new DateTime(2018, 04, 11);

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                //Если тип транзакции = "Начисление бонусов" или "Списание бонусов"
                if ((int) myReader["TransactionType"] == 7 || (int) myReader["TransactionType"] == 2)
                {
                    exportData.Add(new Dto
                    {
                        TransactionId = (string) myReader["TransactionID"],
                        CardId = (string) myReader["CardID"],
                        Amount = (decimal) myReader["Sum"],
                        OrderId = (string) myReader["Description"],
                        TypeBonus = (TypeBonus) myReader["TransactionType"],
                        TransactionDateTime = (DateTime) myReader["TransactionDateTime"]
                    });
                }
            }
            myReader.Close();

            //заполняем дополнительные реквизиты
            foreach (var dto in exportData)
            {
                dto.CardNumber = GetCardNumberByCardId(sqlConnection, dto.CardId);
                dto.PhoneNumber = GetPhoneByCard(sqlConnection, dto.CardNumber);
                dto.Balance = GetBalanceCardByTransactionId(sqlConnection, dto.TransactionId);
            }

            return exportData.AsEnumerable();
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
                ("ds_GetCardByCode", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var code = testCmd.Parameters.Add("@Code", SqlDbType.VarChar, 100);
            code.Direction = ParameterDirection.Input;
            code.Value = cardNumber;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                phoneNumber = (string)myReader["PhoneNumber"];
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
        /// <param name="transactionId">уникальный идентификатор транзакции</param>
        /// <returns></returns>
        private static decimal GetBalanceCardByTransactionId(SqlConnection sqlConnection, string transactionId)
        {
            decimal balance = 0;

            var testCmd = new SqlCommand
                ("ds_GetTransaction", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramTransactionId = testCmd.Parameters.Add("@TransactionID", SqlDbType.VarChar, 38);
            paramTransactionId.Direction = ParameterDirection.Input;
            paramTransactionId.Value = transactionId;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                balance = (decimal)myReader["CardBalance"];
            }
            myReader.Close();

            return balance;
        }
    }
}
