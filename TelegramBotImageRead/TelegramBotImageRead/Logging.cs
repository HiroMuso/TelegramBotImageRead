using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramBotImageRead
{
    public class Logging
    {
        private static string folder_path = "TelegramLog";
        private static string name_file_error_log = "ErrorLog.txt";
        private static string name_file_username_log = "UserLog.txt";
        private static Dictionary<long, string> user_list = new Dictionary<long, string>();

        public static void WriteError(string message)
        {
            if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);

            string file_path = Path.Combine(folder_path, name_file_error_log);
            string full_message = $"[{DateTime.Now}][Error: {message}]\n";

            File.WriteAllText(file_path, full_message);
        } 

        public static void WriteUser(long chat_id, string username, string first_name, string last_name)
        {
            if (!user_list.Any(x => x.Key == chat_id))
            {
                user_list.Add(chat_id, username);
                if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);

                string file_path = Path.Combine(folder_path, name_file_username_log);
                string full_message = $"[{DateTime.Now}][Chat Id: {chat_id}, Username: {username}, First Nmae: {first_name}, Last Name: {last_name}]\n";

                File.WriteAllText(file_path, full_message);
            }
        }
    }
}
