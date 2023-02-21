using System;
using System.IO;
using System.Linq;
using System.Threading;


namespace TelegramBotImageRead
{
    public class TimerDeleteCash
    {
        private static string error_log_path = @"TelegramLog/ErrorLog.txt";
        private static Timer timer;
        public static void StartTimer()
        {
            timer = new Timer(Callback, null, new TimeSpan(0, 0, 0, 0, 500), new TimeSpan(0, 1, 30, 0, 0));
        }

        public static void Callback(object? obj)
        {
            Console.WriteLine("UserModel length было: " +  TelegramBot.user_model.Count);
            TelegramBot.user_model.Clear();
            TelegramBot.user_model.Clear();
            Console.WriteLine("UserModel length стало: " + TelegramBot.user_model.Count);

            if (!Directory.Exists(TelegramBot.folder_path))
            {
                Console.WriteLine($"Папка по пути: {TelegramBot.folder_path} не существует");
            }
            else
            {
                TelegramBot.user_model.Clear();
                Directory.Delete(TelegramBot.folder_path, true);
            }

            if (!File.Exists(error_log_path))
            {
                Console.WriteLine($"Папки по пути: {error_log_path} не существует");
            }
            else  File.Delete(error_log_path);
        }

    }
}
