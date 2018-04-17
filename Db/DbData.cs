using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ExportToService.Dto;

namespace ExportToService.Db
{
    /// <summary>
    /// Связист с ДДС
    /// </summary>
    public class DbData
    {
        private readonly string _initCatalog;
        private readonly string _dataSource;
        private readonly string _login;
        private readonly string _password;

        public DbData()
        {
            _initCatalog = ConfigurationManager.AppSettings["DB_InitialCatalog"];
            _dataSource = ConfigurationManager.AppSettings["DB_dataSource"];
            _login = ConfigurationManager.AppSettings["DB_login"];
            _password = ConfigurationManager.AppSettings["DB_password"];
        }

        /// <summary>
        /// ПолучитьДанныеОТранзакциях
        /// </summary>
        /// <param name="brokenTransactions">Список транзакций</param>
        /// <returns>список информации о транзакциях</returns>
        internal List<TransactionInfo> GetTransactionFromDb(List<string> brokenTransactions = null)
        {
            var transactions = new List<TransactionInfo>();

            using (var sqlConnection = GetSqlConnection())
            {
                sqlConnection.Open();

                //Если присутсвуют "сломанные" транзакции то добавляем в список их  
                if(brokenTransactions != null)
                    transactions.AddRange(brokenTransactions.Select(brokenTransaction => GetTransaction(brokenTransaction, sqlConnection)));

                transactions.AddRange(GetTransactions(sqlConnection));


                sqlConnection.Close();
            }

            return transactions;
        }

        /// <summary>
        /// ПолучитьSQLПодключение
        /// </summary>
        /// <returns>подключение</returns>
        private SqlConnection GetSqlConnection()
        {
            return new SqlConnection(@"Integrated Security=True;Trusted_Connection=True;User ID=" + _login +
                                     ";Password=" + _password + ";Initial Catalog=" + _initCatalog +
                                     ";Data Source=" + _dataSource + ";");
        }


        /// <summary>
        /// ПолучитьТранзакциюПоID
        /// </summary>
        /// <param name="idTransaction">ID транзакции</param>
        /// <param name="sqlConnection">подключение</param>
        /// <returns></returns>
        private static TransactionInfo GetTransaction(string idTransaction, SqlConnection sqlConnection)
        {
            var result = new TransactionInfo();
            var testCmd = new SqlCommand
                ("ds_GetTransaction", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramTransactionId = testCmd.Parameters.Add("@TransactionID", SqlDbType.VarChar, 38);
            paramTransactionId.Direction = ParameterDirection.Input;
            paramTransactionId.Value = idTransaction;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                result.TransactionId = (string) myReader["TransactionID"];
                result.CardId = (string) myReader["CardID"];
                result.Amount = (decimal) myReader["Sum"];
                result.TypeBonus = (TypeBonus) myReader["TransactionType"];
                result.TransactionDateTime = (DateTime) myReader["TransactionDateTime"];
            }
            
            myReader.Close();

            //заполняем дополнительные реквизиты
            result.CardNumber = GetCardNumberByCardId(sqlConnection, result.CardId);
            result.PhoneNumber = GetPhoneByCard(sqlConnection, result.CardNumber);
            result.BalanceOnTransaction = GetBalanceCardByTransactionId(sqlConnection, result.TransactionId);
            
            return result;
        }

        /// <summary>
        /// ПолучитьТранзакцииПоНачислениюИСписаниюБонусов
        /// </summary>
        /// <param name="sqlConnection">SQL подключение</param>
        /// <returns></returns>
        private static IEnumerable<TransactionInfo> GetTransactions(SqlConnection sqlConnection)
        {
            var exportData = new List<TransactionInfo>();
            var testCmd = new SqlCommand
                ("ds_GetTransactions", sqlConnection)
                {CommandType = CommandType.StoredProcedure};

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            //@OrderBy
            var paramOrderBy = testCmd.Parameters.Add("@OrderBy", SqlDbType.Int);
            paramOrderBy.Direction = ParameterDirection.Input;
            paramOrderBy.Value = 0;

            //@BeginDate
            var paramBeginDate = testCmd.Parameters.Add("@BeginDate", SqlDbType.DateTime);
            paramBeginDate.Direction = ParameterDirection.Input;
            paramBeginDate.Value = new DbSqlite().GetLatestSendDate();

            //@RecordCount
            var paramRecordCount = testCmd.Parameters.Add("@RecordCount", SqlDbType.Int);
            paramRecordCount.Direction = ParameterDirection.Input;
            paramRecordCount.Value = 10000;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                //Если тип транзакции = "Начисление бонусов" или "Списание бонусов"
                if ((int) myReader["TransactionType"] == 7 || (int) myReader["TransactionType"] == 2)
                {
                    exportData.Add(new TransactionInfo
                    {
                        TransactionId = (string) myReader["TransactionID"],
                        CardId = (string) myReader["CardID"],
                        Amount = (decimal) myReader["Sum"],
                        TypeBonus = (TypeBonus) myReader["TransactionType"],
                        TransactionDateTime = (DateTime) myReader["TransactionDateTime"]
                    });
                }
            }
            myReader.Close();

            //заполняем дополнительные реквизиты
            foreach (var transactionInfo in exportData)
            {
                transactionInfo.CardNumber = GetCardNumberByCardId(sqlConnection, transactionInfo.CardId);
                transactionInfo.PhoneNumber = GetPhoneByCard(sqlConnection, transactionInfo.CardNumber);
                transactionInfo.BalanceOnTransaction = GetBalanceCardByTransactionId(sqlConnection, transactionInfo.TransactionId);
            }

            var copyExportData = new List<TransactionInfo>();

            //исключаю уже отправленные транзакции
            foreach (var info in exportData)
            {
                if (!new DbSqlite().GetSentTransactions().Contains(info.TransactionId))
                {
                    copyExportData.Add(info);
                }
            }


            return copyExportData.AsEnumerable();
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
        /// ПолучитьБалансКартыНаМоментТранзакции
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

        /// <summary>
        /// ПолучитьБалансКартыНаМоментВыполнения
        /// </summary>
        /// <param name="cardId">уникальный идентификатор карты</param>
        /// <returns></returns>
        public decimal GetBalanceCardByOnRealTime(string cardId)
        {
            decimal balance = 0;

            var sqlConnection = GetSqlConnection();
            sqlConnection.Open();

            var testCmd = new SqlCommand
                ("ds_GetBalance", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = testCmd.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramCardId = testCmd.Parameters.Add("@CardID", SqlDbType.VarChar, 38);
            paramCardId.Direction = ParameterDirection.Input;
            paramCardId.Value = cardId;

            var paramCode = testCmd.Parameters.Add("@Code", SqlDbType.VarChar, 100);
            paramCode.Direction = ParameterDirection.Input;
            paramCode.Value = DBNull.Value;

            var paramMode = testCmd.Parameters.Add("@Mode", SqlDbType.Int);
            paramMode.Direction = ParameterDirection.Input;
            paramMode.Value = 0;

            var paramWalletId = testCmd.Parameters.Add("@WalletID", SqlDbType.VarChar, 38);
            paramWalletId.Direction = ParameterDirection.Input;
            paramWalletId.Value = DBNull.Value;

            var myReader = testCmd.ExecuteReader();
            while (myReader.Read())
            {
                balance = (decimal)myReader["Balance"];
            }
            myReader.Close();

            sqlConnection.Close();

            return balance;
        }
    }
}