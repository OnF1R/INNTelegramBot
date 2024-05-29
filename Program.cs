using Dadata;
using System.IO;
using System.Runtime.InteropServices;
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
        private static ITelegramBotClient _telegramBotClient;

        private static SuggestClientAsync _dadataClient;

        private static ReceiverOptions _receiverOptions;

        private static Dictionary<string, CommandInfo> _usersLastCommand = new Dictionary<string, CommandInfo>();

        private static async Task Main(string[] args)
        {
            using (FileStream fStream =
            new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "telegram_api_key.conf"), FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fStream.Length];
                await fStream.ReadAsync(buffer);
                _telegramBotClient = new TelegramBotClient(Encoding.Default.GetString(buffer));
            }

            using (FileStream fStream =
                new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "dadata_api_key.conf"), FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fStream.Length];
                await fStream.ReadAsync(buffer);
                _dadataClient = new SuggestClientAsync(Encoding.Default.GetString(buffer));
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

            _telegramBotClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _telegramBotClient.GetMeAsync();

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
            await _telegramBotClient.SendTextMessageAsync(
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
                    $"\n/okved — Не выполнено, невозможно выполнить с бесплатным доступом к Dadata." +
                    $"\n/egrul — Отправляет PDF-файл с выпиской ЕГРЮЛ по ИНН.";

                    await SendTextMessage(helpText, message, cancellationToken);
                    break;
                case CommandType.hello:
                    await SendTextMessage("Николаев Андрей\nE-mail: bender987@bk.ru\nGithub: https://github.com/OnF1R", message, cancellationToken);
                    break;
                case CommandType.inn:
                    string[] innInns = message.Text.Split(' ');

                    if (innInns.Length < 2)
                    {
                        await SendTextMessage("Не указан ИНН.", message, cancellationToken);
                        break;
                    }

                    string innResultMessage = "";

                    for (int i = 1; i < innInns.Length; i++)
                    {
                        bool isNumber = long.TryParse(innInns[i], out var number);

                        if (isNumber)
                        {
                            var result = await _dadataClient.FindParty(innInns[i], cancellationToken);

                            if (result.suggestions.Count > 0)
                            {
                                innResultMessage += $"{i}: {result.suggestions[0].data.name.full_with_opf}, {result.suggestions[0].data.address.value}";
                            }
                            else
                            {
                                innResultMessage += $"{i}: Ничего не найдено.";
                            }
                        }
                        else
                        {
                            innResultMessage += $"{i}: ИНН введен неправильно.";
                            break;
                        }

                        innResultMessage += "\n";
                    }

                    await SendTextMessage(innResultMessage, message, cancellationToken);
                    break;
                case CommandType.last:
                    await SendTextMessage("Повтор последней команды", message, cancellationToken);
                    await ProcessCommand(_usersLastCommand[message.From.Username].CommandType, _usersLastCommand[message.From.Username].Message, cancellationToken);
                    break;
                //case CommandType.okved:
                //    string[] okvedInns = message.Text.Split(' ');

                //    if (okvedInns.Length < 2)
                //    {
                //        await SendTextMessage("Не указан ИНН.", message, cancellationToken);
                //        break;
                //    }

                //    string okvedResultMessage = "";

                //    for (int i = 1; i < okvedInns.Length; i++)
                //    {
                //        bool isNumber = long.TryParse(okvedInns[i], out var number);

                //        if (isNumber)
                //        {
                //            var result = await _dadataClient.FindParty(okvedInns[i], cancellationToken);

                //            okvedResultMessage += $"Коды ОКВЭД и виды деятельности {i}:\n";

                //            if (result.suggestions.Count > 0)
                //            {
                //                foreach (var okved in result.suggestions[0].data.okveds)
                //                {
                //                    okvedResultMessage += $" Код: {okved.code}";
                //                    okvedResultMessage += $" Наименование деятельности:{okved.name}\n";
                //                }
                //            }
                //            else
                //            {
                //                okvedResultMessage += $"{i}: Ничего не найдено.";
                //            }
                //        }
                //        else
                //        {
                //            okvedResultMessage += $"{i}: ИНН введен неправильно.";
                //            break;
                //        }

                //        okvedResultMessage += "\n";
                //    }
                //    await SendTextMessage(okvedResultMessage, message, cancellationToken);

                //    Начал делать, но понял что с базовой версией Dadata API не получиться реализовать.
                //    break;
                case CommandType.egrul:
                    string[] inn = message.Text.Split(' ');

                    if (inn.Length == 2)
                    {
                        await InteractWithEGRUL.DownloadPDF(inn[1]);
                        var fileInfo = InteractWithEGRUL.GetPDFFromDirectory();

                        if (fileInfo is null)
                        {
                            await SendTextMessage("Ничего не найдено.", message, cancellationToken);
                            InteractWithEGRUL.DeleteAllFromDirectory();
                            break;
                        }

                        using (var stream = System.IO.File.Open(fileInfo.FullName, FileMode.Open))
                        {
                            InputFile inputFile = InputFile.FromStream(stream, inn[1] + ".pdf");
                            var send = await _telegramBotClient.SendDocumentAsync(
                                message.Chat.Id, inputFile, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                        }

                        InteractWithEGRUL.DeleteAllFromDirectory();
                    }
                    else if (inn.Length < 2)
                    {
                        await SendTextMessage("Не указан ИНН.", message, cancellationToken);
                    }
                    else
                    {
                        await SendTextMessage("Введено большо одного ИНН.", message, cancellationToken);
                    }

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
