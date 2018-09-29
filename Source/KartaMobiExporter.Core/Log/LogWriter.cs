using System;
using System.Diagnostics;
using System.IO;

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
            try
            {
                string specialFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\KartaMobi";
                if (!Directory.Exists(specialFolder)) Directory.CreateDirectory(specialFolder);

                DeleteOldFile(specialFolder);

                using (StreamWriter file = new StreamWriter(specialFolder + @"\Log" + DateTime.Now.Month + ".txt", true))
                {
                    file.WriteLine(x);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine(x);
            }
        }

        /// <summary>
        /// Удалить старые файлы лог за предыдущие месяцы
        /// </summary>
        /// <param name="specialFolder"></param>
        private static void DeleteOldFile(string specialFolder)
        {
            foreach(var file in Directory.GetFiles(specialFolder, "*.txt"))
            {
                if (!file.Contains(DateTime.Now.Month.ToString()))
                {
                    File.Delete(file);
                }
            }
        }
    }
}