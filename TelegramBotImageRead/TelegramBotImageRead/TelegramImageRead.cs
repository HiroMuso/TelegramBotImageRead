using System;
using Patagames.Ocr.Enums;

namespace TelegramBotImageRead
{
    public class TelegramImageRead
    {

        public static (string, string) ReadImage(string path_file, Languages language, bool is_spell_check)
        {
            Console.WriteLine("Путь фото для сканирования: " + path_file + ", язык: " + language.ToString());
            ImageRead image_read = new ImageRead(path_file, language);
            try
            {
                image_read.Scanning();

                string spell_check_text = null;
                if (is_spell_check)
                    spell_check_text = image_read.SpellCheck();

                return (image_read.GetText(), spell_check_text);
            }
            catch (Exception ex)
            {
                Logging.WriteError($"Выскачила ошибка при сканировании фото. {ex.Message}.");
                return (null, null);
            }
        }

        public static string SpellCheckTextTel(string text, SpellCheckText.LenguageSpellCheck lenguage)
        {
            try
            {
                return SpellCheckText.Check(text, lenguage);
            }
            catch (Exception ex)
            {
                Logging.WriteError($"Выскачила ошибка при поиске ошибки. {ex.Message}.");
                return null;
            }
        }
    }
}
