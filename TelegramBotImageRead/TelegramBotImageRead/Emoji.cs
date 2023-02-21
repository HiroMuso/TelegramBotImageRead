using System.Collections.Generic;

namespace TelegramBotImageRead
{
    public class Emoji
    {
        public enum Enum
        {
            RadioButton = 0,
            BallotBox = 1,
            HeaveMark = 2,
            Setting = 3,
            Close = 4,
        }
        
        public static Dictionary<Enum, string> list = new Dictionary<Enum, string>(){
            { Enum.RadioButton , "🔘" },
            { Enum.BallotBox , "☑️" },
            { Enum.HeaveMark , "✔️" },
            { Enum.Setting, "🛠" },
            { Enum.Close, "↩️" }
            };
    }
}
