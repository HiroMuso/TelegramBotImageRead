using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TelegramBotImageRead
{
    public class SpellCheckText
    {
        private static List<string> LIST_LENGUAGE = new List<string>() { "en-US", "ru-RU" };

        public enum LenguageSpellCheck
        {
            English = 0,
            Russian = 1,
        }

        public static string Check(string text, LenguageSpellCheck lenguage)
        {
            SpellCheckApi.SpellCheckerFactoryClass factory = null;
            SpellCheckApi.ISpellCheckerFactory ifactory = null;
            SpellCheckApi.ISpellChecker checker = null;
            SpellCheckApi.ISpellingError error = null;
            SpellCheckApi.IEnumSpellingError errors = null;
            SpellCheckApi.IEnumString suggestions = null;
            StringBuilder sb = new StringBuilder(text.Length * 10);

            try
            {

                factory = new SpellCheckApi.SpellCheckerFactoryClass();
                ifactory = (SpellCheckApi.ISpellCheckerFactory)factory;

                int res = ifactory.IsSupported(LIST_LENGUAGE[(int)lenguage]);
                if (res == 0) { throw new Exception($"Fatal error: {LIST_LENGUAGE[(int)lenguage]} language not supported!"); }

                checker = ifactory.CreateSpellChecker(LIST_LENGUAGE[(int)lenguage]);

                errors = checker.Check(text);
                while (true)
                {
                    if (error != null) { Marshal.ReleaseComObject(error); error = null; }

                    error = errors.Next();
                    if (error == null) break;

                    //получаем слово с ошибкой
                    string word = text.Substring((int)error.StartIndex, (int)error.Length);
                    sb.AppendLine("Ошибка в слове: " + word);

                    //получаем рекомендуемое действие
                    switch (error.CorrectiveAction)
                    {
                        case SpellCheckApi.CORRECTIVE_ACTION.CORRECTIVE_ACTION_DELETE:
                            sb.AppendLine("Рекомендуемое действие: удалить");
                            break;

                        case SpellCheckApi.CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE:
                            sb.AppendLine("Рекомендуемое действие: заменить на " + error.Replacement);
                            break;

                        case SpellCheckApi.CORRECTIVE_ACTION.CORRECTIVE_ACTION_GET_SUGGESTIONS:
                            sb.AppendLine("Рекомендуемое действие: заменить на одно из следующих слов");

                            if (suggestions != null) { Marshal.ReleaseComObject(suggestions); suggestions = null; }

                            suggestions = checker.Suggest(word);

                            sb.Append(" ");
                            while (true)
                            {
                                string suggestion;
                                uint count = 0;
                                suggestions.Next(1, out suggestion, out count);
                                if (count == 1) sb.Append(suggestion + " ");
                                else break;
                            }
                            sb.AppendLine();
                            break;
                    }
                    sb.AppendLine();
                }
            }
            finally
            {
                if (suggestions != null) { Marshal.ReleaseComObject(suggestions); }
                if (factory != null) { Marshal.ReleaseComObject(factory); }
                if (ifactory != null) { Marshal.ReleaseComObject(ifactory); }
                if (checker != null) { Marshal.ReleaseComObject(checker); }
                if (error != null) { Marshal.ReleaseComObject(error); }
                if (errors != null) { Marshal.ReleaseComObject(errors); }
            }

            return sb.ToString();
        }
    }
}
