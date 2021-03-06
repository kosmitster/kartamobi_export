﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using KartaMobiExporter.Core.Dto;
using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Core.Db
{
    public class DbSqlite
    {
        private readonly string _dbFileName;

        /*Информация о транзакциях*/
        internal const string CommandCreateSentTransaction = @"CREATE TABLE IF NOT EXISTS SentTransactions 
            (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2), TransactionDateTime [DateTime])";
        internal const string CommandCreateErrorTransaction = @"CREATE TABLE IF NOT EXISTS ErrorTransactions 
            (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2), TransactionDateTime [DateTime])";
        internal const string CommandCreateTrigger = @"CREATE TRIGGER IF NOT EXISTS ErrorTransactionDisabled 
            AFTER INSERT ON SentTransactions BEGIN DELETE FROM ErrorTransactions WHERE TransactionID = NEW.TransactionID; END;";

        /*Информация о настройках доступа к базе DDS*/
        internal const string CommandCreateSqlOption = @"CREATE TABLE IF NOT EXISTS OptionDDS 
            (InitialCatalog [VARCHAR](38), DataSource [VARCHAR](38), Login [VARCHAR](38), Password [VARCHAR](38))";
        /*Информация о настройках доступа к Karta.Mobi*/
        internal const string CommandCreateKartaMobiOption = @"CREATE TABLE IF NOT EXISTS OptionKartaMobi 
            (Btoken [VARCHAR](38), Login [VARCHAR](38), Password [VARCHAR](38))";

        /*Логи*/
        internal const string CommandCreateLog = @"CREATE TABLE IF NOT EXISTS Log 
            (LogDateTime [DateTime], LogValue [VARCHAR](100))";


        /// <summary>
        /// Конструктор {создаёт базу в случае если её нет и все потраха}
        /// </summary>
        public DbSqlite()
        {
            string specialFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\KartaMobi";

            if (!Directory.Exists(specialFolder)) Directory.CreateDirectory(specialFolder);


            _dbFileName = specialFolder + "\\Db.sqlite";


            if (!File.Exists(_dbFileName)) SQLiteConnection.CreateFile(_dbFileName);

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            //Создать таблицу для успешно отправленных транзакций
            AdapterSqlite.SetCommand(CommandCreateSentTransaction, mDbConnection);
            //Создать таблицу для сбойных транзакций
            AdapterSqlite.SetCommand(CommandCreateErrorTransaction, mDbConnection);
            //Создать триггер для удаления сбойных транзакций в случае если транзакция успешно отправлена
            AdapterSqlite.SetCommand(CommandCreateTrigger, mDbConnection);

            //Добавляем дополнительные колонки в случае если их нет в базе
            if (!IsFieldExist("SentTransactions", "Phone"))
            {
                AdapterSqlite.SetCommand("ALTER TABLE SentTransactions ADD COLUMN Phone [VARCHAR](15);", mDbConnection);
            }
            if (!IsFieldExist("SentTransactions", "Card"))
            {
                AdapterSqlite.SetCommand("ALTER TABLE SentTransactions ADD COLUMN Card [VARCHAR](15);", mDbConnection);
            }
            if (!IsFieldExist("ErrorTransactions", "Phone"))
            {
                AdapterSqlite.SetCommand("ALTER TABLE ErrorTransactions ADD COLUMN Phone [VARCHAR](15);", mDbConnection);
            }
            if (!IsFieldExist("ErrorTransactions", "Card"))
            {
                AdapterSqlite.SetCommand("ALTER TABLE ErrorTransactions ADD COLUMN Card [VARCHAR](15);", mDbConnection);
            }

            //Создать таблицу для хранения настройки доступа к базе DDS
            AdapterSqlite.SetCommand(CommandCreateSqlOption, mDbConnection);
            //Создать таблицу для хранения настройки доступа к Karta.Mobi
            AdapterSqlite.SetCommand(CommandCreateKartaMobiOption, mDbConnection);
            //Создать таблицу LOG
            AdapterSqlite.SetCommand(CommandCreateLog, mDbConnection);

            mDbConnection.Close();
        }

        /// <summary>
        /// Проверить есть ли у таблицы колонка
        /// </summary>
        /// <param name="tableName">Наименование таблицы</param>
        /// <param name="fieldName">Наименование колонки</param>
        /// <returns></returns>
        public bool IsFieldExist(String tableName, String fieldName)
        {
            var isExist = false;
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            var command = new SQLiteCommand("PRAGMA table_info(" + tableName + ")", mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                string currentColumn = (string) myReader[1];
                if (currentColumn.Equals(fieldName))
                {
                    isExist = true;
                }
            }
            myReader.Close();
            mDbConnection.Close();

            return isExist;
        }

        /// <summary>
        /// Получить список транзакций
        /// </summary>
        /// <returns></returns>
        public List<LogItem> GetSentTransactions()
        {
            var items = new List<LogItem>();

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            var command = new SQLiteCommand("SELECT * FROM SentTransactions Order By TransactionDateTime desc", mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                items.Add(new LogItem
                {
                    CardId = (string)myReader["CardID"],
                    Phone = (string)(myReader["Phone"] == DBNull.Value ? string.Empty : myReader["Phone"]),
                    Card = (string)(myReader["Card"] == DBNull.Value ? string.Empty : myReader["Card"]),
                    Amount = (decimal)myReader["Sum"],
                    Date = ((DateTime)myReader["TransactionDateTime"]).ToString("dd.MM.yyyy HH:mm:ss"),
                    Result = "Отправлено",
                    TransactionType = (TransactionType)myReader["TransactionType"]
                });
            }
            myReader.Close();
            mDbConnection.Close();

            return items;

        }


        /// <summary>
        /// Получить настройки доступа к базе DDS
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public OptionDDS GetOptionDDS()
        {
            var result = new OptionDDS();

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            var command = new SQLiteCommand("SELECT InitialCatalog, dataSource, login, password FROM OptionDDS LIMIT 1", mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                result.InitialCatalog = (string)myReader["InitialCatalog"];
                result.DataSource = (string)myReader["DataSource"];
                result.Login = (string)myReader["Login"];
                result.Password = (string)myReader["Password"];
            }
            myReader.Close();

            mDbConnection.Close();

            return result;
        }

        /// <summary>
        /// Сохранить настроойки доступа к базе DDS
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public void SetOptionDDS(OptionDDS optionDDS)
        {

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            //Удалить настройки
            AdapterSqlite.SetCommand("DELETE FROM OptionDDS", mDbConnection);

            //Сохранить новые настройки
            AdapterSqlite.SetCommand(
                "INSERT INTO OptionDDS(InitialCatalog, DataSource, Login, Password) VALUES ('" +
                optionDDS.InitialCatalog + "', '" + optionDDS.DataSource + "', '" + optionDDS.Login + "', '" +
                optionDDS.Password + "')", mDbConnection);

            mDbConnection.Close();
        }

        /// <summary>
        /// Получить настройки доступа к Karta.Mobi
        /// </summary>
        /// <returns></returns>
        public OptionKartaMobi GetOptionKartaMobi()
        {
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            var result = new OptionKartaMobi();
            var command = new SQLiteCommand("SELECT Btoken, login, password FROM OptionKartaMobi LIMIT 1", mDbConnection);
            var myReader = command.ExecuteReader();

            while (myReader.Read())
            {
                result.Btoken = (string)myReader["Btoken"];
                result.Login = (string)myReader["Login"];
                result.Password = (string)myReader["Password"];
            }
            myReader.Close();

            mDbConnection.Close();

            return result;
        }

        /// <summary>
        /// Сохранить настройки доступа к Karta.Mobi
        /// </summary>
        public void SetOptionKartaMobi(OptionKartaMobi optionKartaMobi)
        {
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            //Удалить настройки
            AdapterSqlite.SetCommand("DELETE FROM OptionKartaMobi", mDbConnection);

            //Сохранить новые настройки
            AdapterSqlite.SetCommand(
                "INSERT INTO OptionKartaMobi(Btoken, Login, Password) VALUES ('" +
                optionKartaMobi.Btoken + "', '" + optionKartaMobi.Login + "', '" + optionKartaMobi.Password + "')", mDbConnection);

            mDbConnection.Close();
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
        /// СохранитьУспешноОтправленныеТранзакции
        /// </summary>
        /// <param name="transactionInfo">Информация о транзакции</param>
        public void SaveSentTransaction(TransactionInfo transactionInfo)
        {
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            AdapterSqlite.SetCommand(
                "INSERT INTO SentTransactions(TransactionID, CardID, TransactionType, Sum, TransactionDateTime, Phone, Card) VALUES ('" +
                transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                (int) transactionInfo.TransactionType + ", " +
                transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                ", '" + transactionInfo.TransactionDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', '" +
                transactionInfo.PhoneNumber + "', '" + transactionInfo.CardNumber + "')", mDbConnection);

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
                var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
                mDbConnection.Open();

                AdapterSqlite.SetCommand(
                    "INSERT INTO ErrorTransactions(TransactionID, CardID, TransactionType, Sum, TransactionDateTime, Phone, Card) VALUES ('" +
                    transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                    (int) transactionInfo.TransactionType + ", " +
                    transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) +
                    ", '" + transactionInfo.TransactionDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', '" +
                    transactionInfo.PhoneNumber + "', '" + transactionInfo.CardNumber + "')", mDbConnection);

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
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
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
        internal List<string> GetSentTransactions(TransactionType typeTransaction)
        {
            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
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
        public DateTime GetLatestSendDate(TransactionType typeTransaction)
        {
            var latesSendDateTime = DateTime.Now.AddMinutes(-2);
            //var latesSendDateTime = new DateTime(2017,1,1);

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
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
                }
            }
            myReader.Close();

            mDbConnection.Close();

            Log.LogWriter.Write("[Debug] " + typeTransaction + " Начало периода = " + latesSendDateTime);
            return latesSendDateTime;
        }

        /// <summary>
        /// Сохранить строку LOG
        /// </summary>
        /// <param name="optionDDS"></param>
        public void SaveLog(string LogValue)
        {

            var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
            mDbConnection.Open();

            //Сохранить Log
            AdapterSqlite.SetCommand(
                "INSERT INTO Log(LogDateTime, LogValue) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                + "', '" + LogValue + "')", mDbConnection);

            mDbConnection.Close();
        }

    }
}
