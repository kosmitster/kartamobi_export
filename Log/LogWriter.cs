﻿using System.Configuration;
using System.IO;

namespace ExportToService.Log
{
    public static class LogWriter
    {
        /// <summary>
        /// Записать новую строку в Log
        /// </summary>
        /// <param name="x">строка для записи</param>
        public static void Write(string x)
        {
            using (StreamWriter file =
                new StreamWriter(ConfigurationManager.AppSettings["LogFilePath"] + @"\Log.txt", true))
            {
                file.WriteLine(x);
            }
        }
    }
}