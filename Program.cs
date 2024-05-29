using Dadata;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace INNTelegramBot
{
    internal class Program
    {
        private static ITelegramBotClient _client;

        private static ReceiverOptions _receiverOptions;

        private static Dictionary<string, CommandInfo> _usersLastCommand = new Dictionary<string, CommandInfo>();

        private static async Task Main(string[] args)
        {
            using (FileStream fStream = 
                new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "telegram_api_key.conf"), FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fStream.Length];
                await fStream.ReadAsync(buffer);
                _client = new TelegramBotClient(Encoding.Default.GetString(buffer));
            }

            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
            {
                UpdateType.Message,
            },
                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();

            _client.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _client.GetMeAsync();

            Console.WriteLine($"{me.FirstName} запущен!");

            await Task.Delay(-1);
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        var message = update.Message;

                        if (message.Text != "/last" && message.Text.StartsWith('/'))
                        {
                            AddOrEditLastCommand(message.From.Username, message);
                        }

                        CommandType commandType = CommandType.unknown;

                        if (message.Text.StartsWith("/start"))
                            commandType = CommandType.start;
                        if (message.Text.StartsWith("/help"))
                            commandType = CommandType.help;
                        if (message.Text.StartsWith("/hello"))
                            commandType = CommandType.hello;
                        if (message.Text.StartsWith("/inn"))
                            commandType = CommandType.inn;
                        if (message.Text.StartsWith("/last"))
                            commandType = CommandType.last;
                        if (message.Text.StartsWith("/okved"))
                            commandType = CommandType.okved;
                        if (message.Text.StartsWith("/egrul"))
                            commandType = CommandType.egrul;

                        await ProcessCommand(commandType, message, cancellationToken);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task SendTextMessage(string text, Message message, CancellationToken cancellationToken)
        {
            await _client.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: text,
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken
                );

            return;
        }

        private static async Task ProcessCommand(CommandType commandType, Message message, CancellationToken cancellationToken)
        {
            switch (commandType)
            {
                case CommandType.start:
                    await SendTextMessage(
                    $"Привет @{message.From.Username}!\n\nЧтобы ознакомитсья с моим функционалом используй команду /help",
                    message, cancellationToken);
                    break;
                case CommandType.help:
                    string helpText = $"Доступные команды:" +
                    $"\n/hello — Выведет информацию о создателе бота." +
                    $"\n/last — Повторит последнее действие бота." +
                    $"\n/inn <ИНН Организации/Организаций> — Выведет наименование организаций и их адреса. (ИНН необходимо вводить через пробел)" +
                    $"\n/okved — В разработке" +
                    $"\n/egrul — В разработке.";

                    await SendTextMessage(helpText, message, cancellationToken);
                    break;
                case CommandType.hello:
                    await SendTextMessage("Николаев Андрей\nE-mail: bender987@bk.ru\nGithub: https://github.com/OnF1R", message, cancellationToken);
                    break;
                case CommandType.inn:
                    string[] inns = message.Text.Split(' ');

                    if (inns.Length < 2)
                    {
                        await SendTextMessage("Не указан ИНН.", message, cancellationToken);
                        break;
                    }

                    string token;

                    using (FileStream fStream =
                        new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "dadata_api_key.conf"), FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[fStream.Length];
                        await fStream.ReadAsync(buffer);
                        token = Encoding.Default.GetString(buffer);
                    }

                    var api = new SuggestClientAsync(token);

                    string resultMessage = "";

                    for (int i = 1; i < inns.Length; i++)
                    {
                        bool isNumber = long.TryParse(inns[i], out var number);

                        if (isNumber)
                        {
                            var result = await api.FindParty(inns[i], cancellationToken);

                            if (result.suggestions.Count > 0)
                            {
                                resultMessage += $"{i}: {result.suggestions[0].data.name.full_with_opf}, {result.suggestions[0].data.address.value}";
                            }
                            else
                            {
                                resultMessage += $"{i}: Ничего не найдено.";
                            }
                        }
                        else
                        {
                            resultMessage += $"{i}: ИНН введен неправильно.";
                            break;
                        }

                        resultMessage += "\n";
                    }

                    await SendTextMessage(resultMessage, message, cancellationToken);
                    break;
                case CommandType.last:
                    await SendTextMessage("Повтор последней команды", message, cancellationToken);
                    await ProcessCommand(_usersLastCommand[message.From.Username].CommandType, _usersLastCommand[message.From.Username].Message, cancellationToken);
                    break;
                case CommandType.okved:
                    break;
                case CommandType.egrul:
                    break;
                default:
                    await SendTextMessage("Неизвестная команда. /help - для просмотра доступных команд.", message, cancellationToken);
                    break;
            }
        }

        private static void AddOrEditLastCommand(string username, Message message)
        {
            CommandType commandType = CommandType.unknown;

            if (message.Text.StartsWith("/start"))
                commandType = CommandType.start;
            if (message.Text.StartsWith("/help"))
                commandType = CommandType.help;
            if (message.Text.StartsWith("/hello"))
                commandType = CommandType.hello;
            if (message.Text.StartsWith("/inn"))
                commandType = CommandType.inn;
            if (message.Text.StartsWith("/okved"))
                commandType = CommandType.okved;
            if (message.Text.StartsWith("/egrul"))
                commandType = CommandType.egrul;

            CommandInfo commandInfo = new CommandInfo()
            {
                Message = message,
                CommandType = commandType,
            };
            
            var isContains = _usersLastCommand.ContainsKey(username);

            if (isContains)
            {
                _usersLastCommand[username] = commandInfo;
            }
            else
            {
                _usersLastCommand.Add(username, commandInfo);
            }

        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
