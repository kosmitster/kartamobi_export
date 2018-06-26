namespace KartaMobiExporter.Dto
{
    public class OptionItem
    {
        /// <summary>
        /// наименование sql базы данных
        /// </summary>
        public string InitialCatalog { get; set; }

        /// <summary>
        /// путь к серверу sql
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// логин sql
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// пароль sql
        /// </summary>
        public string Password { get; set; }
    }
}