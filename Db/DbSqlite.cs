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
            var command = new SQLiteCommand(sql, mDbConnection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Конструктор {создаёт базу в случае если её нет и все потраха}
        /// </summary>
        public DbSqlite()
        {
            _dbFileName = ConfigurationManager.AppSettings["SqliteFilePath"];

            if (!File.Exists(_dbFileName))
                SQLiteConnection.CreateFile(_dbFileName);

            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            //Создать таблицу для успешно отправленных транзакций
            SetCommand(
                "CREATE TABLE IF NOT EXISTS SentTransactions (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2))",
                mDbConnection);
            //Создать таблицу для сбойных транзакций
            SetCommand(
                "CREATE TABLE IF NOT EXISTS ErrorTransactions (TransactionID [VARCHAR](38), CardID [VARCHAR](38), TransactionType [INT], Sum [DECIMAL](17,2))",
                mDbConnection);
            //Создать триггер для удаления сбойных транзакций в случае если транзакция успешно отправлена
            SetCommand(
                "CREATE TRIGGER ErrorTransactionDisabled AFTER INSERT ON SentTransactions BEGIN DELETE FROM ErrorTransactions WHERE TransactionID = NEW.TransactionID; END;",
                mDbConnection);

            mDbConnection.Close();
        }

        /// <summary>
        /// СохранитьУспешноОтправленныеТранзакции
        /// </summary>
        /// <param name="transactionInfo">Информация о транзакции</param>
        public void SaveSentTransaction(TransactionInfo transactionInfo)
        {
            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            SetCommand("INSERT INTO SentTransactions(TransactionID, CardID, TransactionType, Sum) VALUES ('" +
                       transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                       (int) transactionInfo.TypeBonus + ", " + transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) + ")", mDbConnection);

            mDbConnection.Close();            
        }

        /// <summary>
        /// СохранитьСбойныеТранзакции
        /// </summary>
        /// <param name="transactionInfo">Информация о транзакции</param>
        public void SaveErrorTransaction(TransactionInfo transactionInfo)
        {
            var mDbConnection = GetSqLiteConnection();
            mDbConnection.Open();

            SetCommand("INSERT INTO ErrorTransactions(TransactionID, CardID, TransactionType, Sum) VALUES ('" +
                       transactionInfo.TransactionId + "', '" + transactionInfo.CardId + "', " +
                       (int)transactionInfo.TypeBonus + ", " + transactionInfo.Amount.ToString(CultureInfo.InvariantCulture) + ")", mDbConnection);

            mDbConnection.Close();
        }        

    }
}
