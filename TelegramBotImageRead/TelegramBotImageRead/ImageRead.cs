using System;
using System.IO;
using Patagames.Ocr;
using System.Collections.Generic;
using Patagames.Ocr.Enums;
namespace TelegramBotImageRead
{
    public class ImageRead
    {
        private string text { get; set; }
        private List<int> index_new_lines = new List<int>();
        private int words { get; set; }

        private string path_image_read { get; set; }

        private Patagames.Ocr.Enums.Languages language { get; set; }

        public ImageRead(string path_image_read, Languages language)
        {
            this.path_image_read = path_image_read;

            if (language == Languages.Russian)
                this.language = Languages.Russian;
            else if (language == Languages.English)
                this.language = Languages.English;
        }

        public void UpdatePathReadImage(string path_image_read) => this.path_image_read = path_image_read;
        public void Scanning()
        {
            if (!File.Exists(path_image_read))
                Console.WriteLine($"File on the {path_image_read} does not exist. Specify the correct path.");
            try
            {
                string full_path = Path.GetFullPath(path_image_read);
                using (var scanning_api_image = OcrApi.Create())
                {
                    scanning_api_image.Init(language);
                    Console.WriteLine("FullPath: " + full_path);
                    text = scanning_api_image.GetTextFromImage(full_path);
                    text = text.Replace("\"", "'");
                }
            }

            catch (Exception ex) { Console.WriteLine("The file could not be read, or you chose the wrong language. Exception: " + ex.Message); }

            string text_words = text.Substring(0, text.Length - 1);
            text_words = text_words.Replace("\n", " ");
            string[] words_array = text_words.Split(' ');
            words = words_array.Length;
            words_array = null;
        }

        public string GetText()
        {
            if (text == null) { return "The text is empty."; }
            return text;
        }

        public int GetWords() => words;

        public string SpellCheck()
        {
            if (language == Languages.Russian)
                return SpellCheckText.Check(text, SpellCheckText.LenguageSpellCheck.Russian);
            else if (language == Languages.English)
                return SpellCheckText.Check(text, SpellCheckText.LenguageSpellCheck.English);
            return null;
        }
    }
}
