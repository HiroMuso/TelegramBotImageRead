using Telegram.Bot.Types;

namespace TelegramBotImageRead
{
    internal class TelegramUserModel
    {
        public Message photo_or_text { get; set; }
        public bool is_spell_check { get; set; }
        public bool is_spell_check_text { get; set; }
    }
}
