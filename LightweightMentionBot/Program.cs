using Microsoft.Extensions.Configuration;
using PurpleSpamBot_SpamDetector;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LightweightMentionBot
{
    public class Program
    {
        public static BotConfiguration? BotConfiguration { get; set; }

        public static void Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            //DateTime LastWorkingTime = DateTime.MinValue;

            IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false, true).Build();

            //var rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //var workingTime = System.IO.File.ReadAllText(Path.Combine(rootDirectory, "LastWorkingTime.txt"));
            //DateTime.TryParse(workingTime, out LastWorkingTime);

            BotConfiguration = config.GetRequiredSection("BotConfiguration").Get<BotConfiguration>();
            var cancellationToken = new CancellationTokenSource();

            try
            {
                if (token == null)
                {
                    throw new KeyNotFoundException("Token wasn't found");
                }

                var botClient = new TelegramBotClient(token);
                botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    cancellationToken: cancellationToken.Token);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            Task.Delay(int.MaxValue).Wait();
            cancellationToken.Cancel();
        }

        static async Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Log(arg2.Message);
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message == null)
            {
                return;
            }

            var messageType = update.Type;
            if (messageType != UpdateType.Message && messageType != UpdateType.EditedMessage)
            {
                return;
            }

            var chatType = update.Message.Chat.Type;
            if(chatType!= ChatType.Supergroup)
            {
                return;
            }

            var message = update.Message;
            if (message.Text == null)
            {
                return;
            }

            var sampleData = new AllChatHistoryModel.ModelInput()
            {
                Message = message.Text
            };

            Log($"message :{message.Text}");
            Log($"from :{message.From.Id} {message.From.Username}");

            var result = AllChatHistoryModel.Predict(sampleData);
            Log($"prediction :{result.Prediction}");
            Log($"Вероятность, что спам - {result.Score[1]}");
            Log($"Вероятность, что не спам - {result.Score[0]}");
            if (result.Score[1] < BotConfiguration.Boundary)
            {
                return;
            }

            try
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "[Кажется, кто-то нарушает правила чата](tg://user?id=455498975)", ParseMode.Markdown, replyToMessageId: message.MessageId);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        static void Log(string text)
        {
            Console.WriteLine(text);
            var log = $"{DateTime.Now} |{text} {Environment.NewLine}";

            System.IO.File.AppendAllText("log.txt", log);
        }
    }
}