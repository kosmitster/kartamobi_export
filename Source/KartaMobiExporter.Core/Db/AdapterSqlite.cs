using System.Data.SQLite;

namespace KartaMobiExporter.Core.Db
{
    internal static class AdapterSqlite
    {
        /// <summary>
        /// Получить Подключение
        /// </summary>
        /// <returns></returns>
        internal static SQLiteConnection GetSqLiteConnection(string dbFileName)
        {
            return new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
        }

        /// <summary>
        /// Выполнить Команду Молча
        /// </summary>
        /// <param name="sql">команда</param>
        /// <param name="mDbConnection">подключение</param>
        internal static void SetCommand(string sql, SQLiteConnection mDbConnection)
        {
            var command = new SQLiteCommand(sql, mDbConnection);
            command.ExecuteNonQuery();
        }

    }
}
