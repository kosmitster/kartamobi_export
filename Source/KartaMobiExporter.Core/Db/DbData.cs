using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using KartaMobiExporter.Core.Dto;
using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Core.Db
{
    /// <summary>
    /// Связист с ДДС
    /// </summary>
    public class DbData
    {
        // ReSharper disable once InconsistentNaming
        private readonly OptionDDS _optionDDS;

        // ReSharper disable once InconsistentNaming
        public DbData(OptionDDS optionDDS) { _optionDDS = optionDDS; }

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

                transactions.AddRange(GetTransactions(sqlConnection, TypeTransaction.AddToCard));
                transactions.AddRange(GetTransactions(sqlConnection, TypeTransaction.InCard));
                transactions.AddRange(GetTransactions(sqlConnection, TypeTransaction.OutCard));

                sqlConnection.Close();
            }

            return transactions;
        }

        /// <summary>
        /// ПолучитьSQLПодключение
        /// </summary>
        /// <returns>подключение</returns>
        internal SqlConnection GetSqlConnection()
        {
            return new SqlConnection(@"Integrated Security=True;Trusted_Connection=True;User ID=" + _optionDDS.Login +
                                     ";Password=" + _optionDDS.Password + ";Initial Catalog=" + _optionDDS.InitialCatalog +
                                     ";Data Source=" + _optionDDS.DataSource + ";");
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
                result.TypeTransaction = (TypeTransaction) myReader["TransactionType"];
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
        /// <param name="typeTransaction">Тип транзакции</param>
        /// <returns></returns>
        private static IEnumerable<TransactionInfo> GetTransactions(SqlConnection sqlConnection,
            TypeTransaction typeTransaction)
        {
            var exportData = new List<TransactionInfo>();
            var storageProcedure = new SqlCommand
                ("ds_GetTransactions", sqlConnection)
                {CommandType = CommandType.StoredProcedure};

            var retVal = storageProcedure.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            //@OrderBy
            var paramOrderBy = storageProcedure.Parameters.Add("@OrderBy", SqlDbType.Int);
            paramOrderBy.Direction = ParameterDirection.Input;
            paramOrderBy.Value = 0;

            //@BeginDate
            var paramBeginDate = storageProcedure.Parameters.Add("@BeginDate", SqlDbType.DateTime);
            paramBeginDate.Direction = ParameterDirection.Input;
            paramBeginDate.Value = new DbSqlite().GetLatestSendDate(typeTransaction);

            //@RecordCount
            var paramRecordCount = storageProcedure.Parameters.Add("@RecordCount", SqlDbType.Int);
            paramRecordCount.Direction = ParameterDirection.Input;
            paramRecordCount.Value = 10000;

            //@TransactionType
            var paramTransactionType = storageProcedure.Parameters.Add("@TransactionType", SqlDbType.Int);
            paramTransactionType.Direction = ParameterDirection.Input;
            paramTransactionType.Value = (int) typeTransaction;


            var myReader = storageProcedure.ExecuteReader();
            while (myReader.Read())
            {

                exportData.Add(new TransactionInfo
                {
                    TransactionId = (string) myReader["TransactionID"],
                    CardId = (string) myReader["CardID"],
                    Amount = (decimal) myReader["Sum"],
                    TypeTransaction = (TypeTransaction) myReader["TransactionType"],
                    TransactionDateTime = (DateTime) myReader["TransactionDateTime"]
                });

            }
            myReader.Close();

            //заполняем дополнительные реквизиты
            foreach (var transactionInfo in exportData)
            {
                transactionInfo.CardNumber = GetCardNumberByCardId(sqlConnection, transactionInfo.CardId);
                transactionInfo.PhoneNumber = GetPhoneByCard(sqlConnection, transactionInfo.CardNumber);
                transactionInfo.BalanceOnTransaction =
                    GetBalanceCardByTransactionId(sqlConnection, transactionInfo.TransactionId);
            }

            var copyExportData = new List<TransactionInfo>();

            //исключаю уже отправленные транзакции
            foreach (var info in exportData)
            {
                if (!new DbSqlite().GetSentTransactions(typeTransaction).Contains(info.TransactionId))
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
        internal static string GetPhoneByCard(SqlConnection sqlConnection, string cardNumber)
        {
            string phoneNumber = string.Empty;

            var storageProcedure = new SqlCommand
                ("ds_GetCardByCode", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = storageProcedure.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var code = storageProcedure.Parameters.Add("@Code", SqlDbType.VarChar, 100);
            code.Direction = ParameterDirection.Input;
            code.Value = cardNumber;

            var myReader = storageProcedure.ExecuteReader();
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

            var storageProcedure = new SqlCommand
                ("ds_GetCard", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = storageProcedure.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var cardId = storageProcedure.Parameters.Add("@CardID", SqlDbType.VarChar, 38);
            cardId.Direction = ParameterDirection.Input;
            cardId.Value = idCard;

            var myReader = storageProcedure.ExecuteReader();
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

            var storageProcedure = new SqlCommand
                ("ds_GetTransaction", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = storageProcedure.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramTransactionId = storageProcedure.Parameters.Add("@TransactionID", SqlDbType.VarChar, 38);
            paramTransactionId.Direction = ParameterDirection.Input;
            paramTransactionId.Value = transactionId;

            var myReader = storageProcedure.ExecuteReader();
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

            var storageProcedure = new SqlCommand
                ("ds_GetBalance", sqlConnection)
                { CommandType = CommandType.StoredProcedure };

            var retVal = storageProcedure.Parameters.Add("RetVal", SqlDbType.Int);
            retVal.Direction = ParameterDirection.ReturnValue;

            var paramCardId = storageProcedure.Parameters.Add("@CardID", SqlDbType.VarChar, 38);
            paramCardId.Direction = ParameterDirection.Input;
            paramCardId.Value = cardId;

            var paramCode = storageProcedure.Parameters.Add("@Code", SqlDbType.VarChar, 100);
            paramCode.Direction = ParameterDirection.Input;
            paramCode.Value = DBNull.Value;

            var paramMode = storageProcedure.Parameters.Add("@Mode", SqlDbType.Int);
            paramMode.Direction = ParameterDirection.Input;
            paramMode.Value = 0;

            var paramWalletId = storageProcedure.Parameters.Add("@WalletID", SqlDbType.VarChar, 38);
            paramWalletId.Direction = ParameterDirection.Input;
            paramWalletId.Value = DBNull.Value;

            var myReader = storageProcedure.ExecuteReader();
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