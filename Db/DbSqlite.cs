using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using ExportToService.Dto;

namespace ExportToService.Db
{
    public class DbSqlite
    {
        private readonly string _dbFileName;

        /// <summary>
        /// ПолучитьПодключение
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection GetSqLiteConnection()
        {
            return new SQLiteConnection("Data Source=" + _dbFileName + ";Version=3;");
        }

        /// <summary>
        /// ВыполнитьКоммандуМолча
        /// </summary>
        /// <param name="sql">команда</param>
        /// <param name="mDbConnection">подключение</param>
        private static void SetCommand(string sql, SQLiteConnection mDbConnection)
        {
            Log.LogWriter.Write("[SQL] " + sql);
            var command = new SQLiteCommand(sql, mDbConnection);
            command.ExecuteNonQuery();            
        }

        /// <summary>
        /// ПолучитьСписокТранзакций
        /// </summary>
        /// <param name="sql">команда</param>
        /// <param name="mDbConnection">подключение</param>
        /// <returns></returns>
        private List<string> GetFromDbErrorTransactions(string sql, SQLiteConnection mDbConnection)
        {
            var result = new List<string>();
            var command = new SQLiteCommand(sql, mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                result.Add((string) myReader["TransactionID"]);
            }
            myReader.Close();

            return result;
        }

        /// <summary>
        /// Конструктор {создаёт базу в случае если её нет и все потраха}
        /// </summary>
        public DbSqlite()
        {
            _dbFileName = ConfigurationManager.AppSettings["SqliteFilePath"];

            if (!File.Exists(_dbFileName))
            {
                SQLiteConnection.CreateFile(_dbFileName);

                var mDbConnection = GetSqLiteConnection();
                mDbConnection.Open();

                //Создать таблицу для успешно отправленных транзакций
                SetCommand(
                    "CREATE TABLE IF NOT EXISTS SentTransactions (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2), TransactionDateTime [DateTime])",
                    mDbConnection);
                //Создать таблицу для сбойных транзакций
                SetCommand(
                    "CREATE TABLE IF NOT EXISTS ErrorTransactions (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2), TransactionDateTime [DateTime])",
                    mDbConnection);
                //Создать триггер для удаления сбойных транзакций в случае если транзакция успешно отправлена
                SetCommand(
                    "CREATE TRIGGER IF NOT EXISTS ErrorTransactionDisabled AFTER INSERT ON SentTransactions BEGIN DELETE FROM ErrorTransactions WHERE TransactionID = NEW.TransactionID; END;",
                    mDbConnection);

                mDbConnection.Close();
            }
        }

        /// <summary>
        /// СохранитьУспешноОтправленныеТранзакции
        /// </summary>
        /// <param name="transactionInfo">Информация о транзакции</param>
        public void SaveSentTransaction(TransactionInfo transactionInfo)
        {
            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            SetCommand(
                "INSERT INTO SentTransactions(TransactionID, CardID, TransactionType, Sum, TransactionDateTime) VALUES ('" +
                transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                (int) transactionInfo.TypeTransaction + ", " + transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                ", '" + transactionInfo.TransactionDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "')", mDbConnection);

            mDbConnection.Close();            
        }

        /// <summary>
        /// СохранитьСбойныеТранзакции
        /// </summary>
        /// <param name="transactionInfo">Информация о транзакции</param>
        public void SaveErrorTransaction(TransactionInfo transactionInfo)
        {
            //Исключим добавление дублей
            if (!GetErrorTransactions().Contains(transactionInfo.TransactionId))
            {
                var mDbConnection = GetSqLiteConnection();
                mDbConnection.Open();

                SetCommand(
                    "INSERT INTO ErrorTransactions(TransactionID, CardID, TransactionType, Sum, TransactionDateTime) VALUES ('" +
                    transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                    (int)transactionInfo.TypeTransaction + ", " + transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                    ", '" + transactionInfo.TransactionDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "')", mDbConnection);

                mDbConnection.Close();
            }
            else
            {
                Log.LogWriter.Write(
                    "[Continue] Сохранение сбойной транзакции пропущено, так как она уже есть в списке " +
                    transactionInfo.TransactionId);
            }
        }

        /// <summary>
        /// ПолучитьСписокСбойныхТранзакций
        /// </summary>
        /// <returns>Список сбойных транзакций</returns>
        public List<string> GetErrorTransactions()
        {
            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            var errorTransactions = GetFromDbErrorTransactions("SELECT TransactionID FROM ErrorTransactions", mDbConnection);

            mDbConnection.Close();

            return errorTransactions;
        }

        /// <summary>
        /// ПолучитьСписокОтправленныхТранзакций
        /// </summary>
        /// <param name="typeTransaction">Тип транзакции</param>
        /// <returns>Список отправленных транзакций</returns>
        public List<string> GetSentTransactions(TypeTransaction typeTransaction)
        {
            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            var errorTransactions = GetFromDbErrorTransactions("SELECT TransactionID FROM SentTransactions WHERE TransactionType = " + (int) typeTransaction, mDbConnection);

            mDbConnection.Close();

            return errorTransactions;
        }

        /// <summary>
        /// ПолучитьПоследнююДатуОтправки
        /// </summary>
        /// <param name="typeTransaction">Тип транзакции</param>
        /// <returns>последняя дата отправки данных</returns>
        public DateTime GetLatestSendDate(TypeTransaction typeTransaction)
        {
            var latesSendDateTime = new DateTime(2000,1,1);

            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();


            var command = new SQLiteCommand(
                "SELECT MAX(TransactionDateTime) time FROM SentTransactions WHERE TransactionType = " + (int) typeTransaction,
                mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                if (myReader["time"] != DBNull.Value)
                {
                    latesSendDateTime =
                        DateTime.ParseExact((string) myReader["time"], "yyyy-MM-dd HH:mm:ss.fff",
                            CultureInfo.InvariantCulture);
                    Log.LogWriter.Write("[Debug] Начало периода = " + latesSendDateTime);
                }
            }
            myReader.Close();

            mDbConnection.Close();

            return latesSendDateTime;
        }
    }
}
