using KartaMobiExporter.Dto;
using System.Data.SQLite;
using System.IO;

namespace ExportToService.Db
{
    public class OptionRepository
    {
        private readonly string _dbFileName;

        internal const string commandCreateSqlOption = @"CREATE TABLE IF NOT EXISTS OptionDDS 
            (InitialCatalog [VARCHAR](38), dataSource [VARCHAR](38), login [VARCHAR](38), password [VARCHAR](38))";

        internal const string commandCreateKartaMobiOption = @"CREATE TABLE IF NOT EXISTS OptionKartaMobi 
            (btoken [VARCHAR](38), login [VARCHAR](38), password [VARCHAR](38))";


        public OptionRepository(string currentDirectory)
        {
            _dbFileName = currentDirectory + "Option.sqlite";

            if (!File.Exists(_dbFileName))
            {
                SQLiteConnection.CreateFile(_dbFileName);

                var mDbConnection = AdapterSqlite.GetSqLiteConnection(_dbFileName);
                mDbConnection.Open();

                //Создать таблицу хранения настроек для доступа к базе DDS 
                AdapterSqlite.SetCommand(commandCreateSqlOption, mDbConnection);
                //Создать таблицу для сбойных транзакций
                AdapterSqlite.SetCommand(commandCreateKartaMobiOption, mDbConnection);

                mDbConnection.Close();
            }

        }

        public OptionItem GetCurrentOption()
        {


            return new OptionItem();
        }
    }
}
