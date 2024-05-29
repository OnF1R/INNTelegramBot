using Telegram.Bot.Types;

namespace INNTelegramBot
{
    public enum CommandType
    {
        start,
        help,
        hello,
        inn,
        last,
        okved,
        egrul,
        unknown,
    }

    public class CommandInfo
    {
        public CommandType CommandType { get; set; }
        public Message Message { get; set; }
    }
}
