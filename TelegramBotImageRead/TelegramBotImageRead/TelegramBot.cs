using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using System;
using System.Threading;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Patagames.Ocr.Enums;

namespace TelegramBotImageRead
{
    internal class TelegramBot
    {
        public static Dictionary<long, TelegramUserModel> user_model = new Dictionary<long, TelegramUserModel>();
        public static string token_bot = "5846676526:AAHv8jW4n93fjUMm3TSqfquWow0gINCJH9I";
        public static string folder_path = "TelegramPhoto";

        static async Task Main(string[] args)
        {
            TimerDeleteCash.StartTimer();
            var bot_client = new TelegramBotClient(token_bot);
            using CancellationTokenSource cts = new CancellationTokenSource();

            ReceiverOptions receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            bot_client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await bot_client.GetMeAsync();

            Console.WriteLine($"Начал прослушку @{me.Username} {me.LanguageCode}");
            Console.ReadLine();

            cts.Cancel();

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type == UpdateType.Message && (update?.Message?.Text != null || update?.Message?.Photo != null))
                {
                    await HandleMessage(botClient, cancellationToken, update.Message);
                    return;
                } 
                if (update.Type == UpdateType.CallbackQuery && update?.CallbackQuery?.Data != null)
                {
                    await HandleCallback(bot_client, cancellationToken, update.CallbackQuery);
                    return;
                }
            }
        }

        public static async Task HandleMessage(ITelegramBotClient bot_client, CancellationToken cancellationToken, Message message)
        {
            long chat_id = message.Chat.Id;

            if (message.Text == "/start")
            {
                Logging.WriteUser(chat_id, message.From.Username, message.From.FirstName, message.From.LastName);
                await HandleMenu(bot_client, cancellationToken, message, false);
            } else if (message.Text == $"{Emoji.list[Emoji.Enum.Setting]} Инструкция")
            {
                await ManualMessage(bot_client, chat_id, cancellationToken);
            } else if (message.Text == $"{Emoji.list[Emoji.Enum.RadioButton]} Получить текст из Фото")
            {
                await bot_client.SendTextMessageAsync(chat_id, "Пришлите фото", cancellationToken: cancellationToken);

                if (user_model.Any(x => x.Key == chat_id))
                {
                    user_model[chat_id].is_spell_check = false;
                    user_model[chat_id].is_spell_check_text = false;
                }
                else { AddUser(chat_id, null, false, false); }
            }
            else if (message.Text == $"{Emoji.list[Emoji.Enum.BallotBox]} Проверить текст из Фото на Ошибки")
            {
                await bot_client.SendTextMessageAsync(chat_id, "Пришлите фото", cancellationToken: cancellationToken);

                if (user_model.Any(x => x.Key == chat_id))
                {
                    user_model[chat_id].is_spell_check = true;
                    user_model[chat_id].is_spell_check_text = false;
                }
                else { AddUser(chat_id, null, true, false); }
            }
            else if (message.Text == $"{Emoji.list[Emoji.Enum.HeaveMark]} Проверить текст на Ошибки")
            {
                await bot_client.SendTextMessageAsync(chat_id, "Пришлите текст", cancellationToken: cancellationToken);

                if (user_model.Any(x => x.Key == chat_id))
                {
                    user_model[chat_id].is_spell_check = false;
                    user_model[chat_id].is_spell_check_text = true;
                }
                else { AddUser(chat_id, null, false, true); }
            }
            else if (message.Photo != null && user_model.Any(x => x.Key == chat_id))
            {
                if (!user_model.Where(x => x.Key == chat_id).SingleOrDefault().Value.is_spell_check_text)
                {
                    user_model[chat_id].photo_or_text = message; // сохраянем фото в массив, чтобы потом его перевести 
                    InlineKeyboardMarkup reply_keyboard_markup = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Русский", "callback_ru"),
                            InlineKeyboardButton.WithCallbackData("Английский", "callback_en"),
                        },
                    });
                    await bot_client.SendTextMessageAsync(chat_id, "Выберите язык который изображён на Фото", replyMarkup: reply_keyboard_markup, cancellationToken: cancellationToken);
                }
            } else if(message.Text != null && user_model.Any(x => x.Key == chat_id) && message.Text != "Вернуться") //&& user_model.Where(x => x.Key == chat_id).SingleOrDefault().Value.is_spell_check_text)
            {
                if (user_model.Where(x => x.Key == chat_id).SingleOrDefault().Value.is_spell_check_text)
                {
                    user_model[chat_id].photo_or_text = message;
                    InlineKeyboardMarkup reply_keyboard_markup = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Русский", "callback_ru_text"),
                            InlineKeyboardButton.WithCallbackData("Английский", "callback_en_text"),
                        },
                    });
                    await bot_client.SendTextMessageAsync(chat_id, "Выберите язык на котором написан Текст", replyMarkup: reply_keyboard_markup, cancellationToken: cancellationToken);
                }
            }
            else if (message.Text == $"{Emoji.list[Emoji.Enum.Close]} Вернуться")
            {
                await HandleMenu(bot_client, cancellationToken, message, true);
            }

        }

        public static async Task HandleCallback(ITelegramBotClient bot_client, CancellationToken cancellationToken, CallbackQuery callback_query)
        {
            long chat_id = callback_query.Message.Chat.Id;
            if (callback_query.Data == "callback_ru" && user_model.Any(x => x.Key == chat_id))
            {
                await bot_client.SendTextMessageAsync(chat_id, text: "Подождите немного...", cancellationToken: cancellationToken);
                string file_path = await DownloadFile(bot_client, user_model[chat_id].photo_or_text, cancellationToken);
                (string, string) file_text = TelegramImageRead.ReadImage(file_path, Languages.Russian, user_model[chat_id].is_spell_check);

                if (file_text.Item1 == null || file_text.Item1.Contains("\\") || file_text.Item1.Contains("~") || file_text.Item1.Contains("}"))
                {
                    Console.WriteLine("Файл не удалось прочитать, в нём хранится: " + file_text.Item1);
                    await bot_client.SendTextMessageAsync(chat_id, "Не удалось прочитать файл. Скорее всего\nэто связано с тем, что текст не распознан, либо вы выбрали не тот язык.", cancellationToken: cancellationToken);
                    user_model.Remove(chat_id);
                }
                else
                {
                    await bot_client.SendTextMessageAsync(chat_id, $"{file_text.Item1}\n\n{file_text.Item2}", cancellationToken: cancellationToken);
                }
                
                await DeleteMessage(bot_client, cancellationToken, callback_query.Message);
                user_model.Remove(chat_id);
            } else if (callback_query.Data == "callback_en" && user_model.Any(x => x.Key == chat_id))
            {
                await bot_client.SendTextMessageAsync(chat_id, text: "Подождите немного...", cancellationToken: cancellationToken);

                string file_path = await DownloadFile(bot_client, user_model[chat_id].photo_or_text, cancellationToken);
                (string, string) file_text = TelegramImageRead.ReadImage(file_path, Languages.English, user_model[chat_id].is_spell_check);

                if (file_text.Item1 == null || file_text.Item1.Contains("\\") || file_text.Item1.Contains("~") || file_text.Item1.Contains("}"))
                {
                    Console.WriteLine("Файл не удалось прочитать, в нём хранится: " + file_text.Item1);
                    await bot_client.SendTextMessageAsync(chat_id, "Не удалось прочитать файл. Скорее всего\nэто связано с тем, что текст не распознан, либо вы выбрали не тот язык.", cancellationToken: cancellationToken);
                    user_model.Remove(chat_id);
                }
                else
                {
                    await bot_client.SendTextMessageAsync(chat_id, $"{file_text.Item1}\n\n{file_text.Item2}", cancellationToken: cancellationToken);
                }
                await DeleteMessage(bot_client, cancellationToken, callback_query.Message);
                user_model.Remove(chat_id);
            } else if (callback_query.Data == "callback_ru_text" && user_model.Any(x => x.Key == chat_id))
            {
                string spell_check_text = TelegramImageRead.SpellCheckTextTel(user_model[chat_id].photo_or_text.Text, SpellCheckText.LenguageSpellCheck.Russian);
                if (string.Empty == spell_check_text)
                    await bot_client.SendTextMessageAsync(chat_id, "В данном тексте нет ошибок.", cancellationToken: cancellationToken);
                else await bot_client.SendTextMessageAsync(chat_id, $"{user_model[chat_id].photo_or_text.Text}\n\n{spell_check_text}", cancellationToken: cancellationToken);
                user_model.Remove(chat_id);
            } else if(callback_query.Data == "callback_en_text" && user_model.Any(x => x.Key == chat_id))
            {
                string spell_check_text = TelegramImageRead.SpellCheckTextTel(user_model[chat_id].photo_or_text.Text, SpellCheckText.LenguageSpellCheck.English);

                if (string.Empty == spell_check_text)
                    await bot_client.SendTextMessageAsync(chat_id, "В данном тексте нет ошибок.", cancellationToken: cancellationToken);
                else await bot_client.SendTextMessageAsync(chat_id, $"{user_model[chat_id].photo_or_text.Text}\n\n{spell_check_text}", cancellationToken: cancellationToken);
                user_model.Remove(chat_id);
            }
        }

        public static async Task DeleteMessage(ITelegramBotClient bot_client, CancellationToken cancellationToken, Message message)
        {
            await bot_client.DeleteMessageAsync(chatId: message.Chat.Id, messageId: message.MessageId, cancellationToken: cancellationToken);
        }

        public static async Task HandleMenu(ITelegramBotClient bot_client, CancellationToken cancellationToken, Message message, bool is_return)
        {
            ReplyKeyboardMarkup start_keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { $"{Emoji.list[Emoji.Enum.RadioButton]} Получить текст из Фото" },
                new KeyboardButton[] { $"{Emoji.list[Emoji.Enum.BallotBox]} Проверить текст из Фото на Ошибки" },
                new KeyboardButton[] { $"{Emoji.list[Emoji.Enum.HeaveMark]} Проверить текст на Ошибки" },
                new KeyboardButton[] { $"{Emoji.list[Emoji.Enum.Setting]} Инструкция" }
            });
            start_keyboard.ResizeKeyboard = true;

            if (is_return)
            {
                await bot_client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"{Emoji.list[Emoji.Enum.Close]} Вернуться",
                    replyMarkup: start_keyboard,
                    cancellationToken: cancellationToken
                    );
            } else
            {
                try
                {
                    string username = null;
                    if (message.From.Username == null) username = message.From.FirstName;
                    else username = message.From.Username[0].ToString().ToUpper() + message.From.Username.Substring(1);
                    await bot_client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Привет {username}! Я умею сканировать фото и выводить текст,\n" +
                          $"чтобы ты не писал его вручную, а также проверять его на ошибки. Просто пришли мне фото или текст!",
                    replyMarkup: start_keyboard,
                    cancellationToken: cancellationToken
                );
                }
                catch (Exception)
                {
                    Console.WriteLine("Вышел из подконтроля");
                }
            }
        }

        public static async Task ManualMessage(ITelegramBotClient bot_client, long chat_id, CancellationToken cancellationToken)
        {
            string manual_message = "Для того чтобы начать работать с ботом пропишите команду /start.\n" +
                                 "\n" +
                                 "Бот предлагает несколько функций:\n" +
                                 "\n" +
                                 "1.Получить текст из фото.\n" +
                                 "2.Проверить текст из фото на ошибки.\n" +
                                 "3.Проверить текст на ошибки.\n" +
                                 "\n" +
                                 "Вам следует выбрать одну из этих функций и предоставить то, что говорит вам бот.\n" +
                                 "\n" +
                                 "Частые ошибки и как их решить:\n" +
                                 "\n" +
                                 "1.Если при отправлении фото, бот прислал вам текст с неизвестными\n" +
                                 "символами, это значит, что вы выбрали не тот язык, которой\n" +
                                 "изображён на этой картинке. Либо текст был не распознан.\n" +
                                 "\n" +
                                 "2.Если при отправлении текста на ошибки, бот предложил вам\n" +
                                 "изменить текст, и в предложенных словах стоит точка, это\n" +
                                 "значит, что вы выбрали не тот язык, на котором написан этот\n" +
                                 "текст.\n";

            ReplyKeyboardMarkup start_keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { $"{Emoji.list[Emoji.Enum.Close]} Вернуться" } });
            start_keyboard.ResizeKeyboard = true;
            await bot_client.SendTextMessageAsync(chat_id, manual_message, cancellationToken: cancellationToken, replyMarkup: start_keyboard);
        }

        public static void AddUser(long chat_id, Message message, bool is_spell_check = false, bool is_spell_check_text = false)
        {
            user_model.Add(chat_id, new TelegramUserModel() { photo_or_text = message, is_spell_check = is_spell_check, is_spell_check_text = is_spell_check_text });
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }


        public static async Task<string> DownloadFile(ITelegramBotClient bot_client, Message message, CancellationToken cancellation_token)
        {
            try
            {
                var file_id = message.Photo.Last().FileId;
                string file_name = $"{message.From.Username}_{message.From.FirstName}_{message.From.LastName}_{Guid.NewGuid().ToString()}.png";
                if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);
                string file_path = Path.Combine(folder_path, file_name);

                var fileInfo = await bot_client.GetFileAsync(file_id);
                var filePath = fileInfo.FilePath;

                using (FileStream file_stream = new FileStream(file_path, FileMode.Create))
                {
                    await bot_client.DownloadFileAsync(filePath, file_stream, cancellation_token);
                    Console.WriteLine("Файл сохранился по пути: " + file_path);
                    return file_path;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteError($"Ошибка загрузки файла. {ex.Message}.");
                return null;
            }
        }
    }
}
