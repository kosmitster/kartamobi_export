namespace KartaMobiExporter.Core.Log
{
    public static class LogWriter
    {
        /// <summary>
        /// Записать новую строку в Log
        /// </summary>
        /// <param name="x">строка для записи</param>
        public static void Write(string x)
        {
            System.Diagnostics.Debug.WriteLine(x.Remove(100));
            
        }
    }
}