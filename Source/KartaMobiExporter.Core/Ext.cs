using KartaMobiExporter.Dto;

namespace KartaMobiExporter.Core
{
    public static class Ext
    {
        /// <summary>
        /// Проверка заполнения настроек к серверу DDS
        /// </summary>
        /// <param name="option">настройки подключения к DDS</param>
        /// <returns>Заполнены ли настройки подключения к серверу DDS?</returns>
        public static bool IsSettingCompleted(this OptionDDS option)
        {
            return !string.IsNullOrEmpty(option.InitialCatalog)
                   && !string.IsNullOrEmpty(option.DataSource)
                   && !string.IsNullOrEmpty(option.Login)
                   && !string.IsNullOrEmpty(option.Password);
        }

        /// <summary>
        /// Проверка заполнения настроек к сервису Karta.Mobi
        /// </summary>
        /// <param name="option">настройки подключения Karta.Mobi</param>
        /// <returns>Заполнены ли настройки подключения к сервису Karta.Mobi</returns>
        public static bool IsSettingCompleted(this OptionKartaMobi option)
        {
            return !string.IsNullOrEmpty(option.Btoken)
                   && !string.IsNullOrEmpty(option.Login)
                   && !string.IsNullOrEmpty(option.Password);
        }
    }
}
