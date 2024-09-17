using BooksRec.BooksLibraryContext;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BooksRec
{
    class Program
    {
        private static readonly BooksDB _db = new BooksDB();
        private static TelegramBotClient bot;
        private static User me;
        private static Random random = new Random();

        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var botOptions = new TelegramBotClientOptions("7510414878:AAGGTwL5_8Wd0deslAHQ2x-hW_hKwsh7LHo");
            bot = new TelegramBotClient(botOptions);

            me = await bot.GetMeAsync();

            bot.OnError += OnError;
            bot.OnMessage += OnMessage;
            bot.OnUpdate += OnUpdate;

            Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
        }

        private static string currentOperation = null;

        private static async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text == "/start")
            {
                await bot.SendTextMessageAsync(msg.Chat, $"Hi! I'm {me.Username}. \n I'm here to help you find a book for reading :) \n" +
                    $"Please choose an option below to continue: \n" +
                    $"/findbook - helps you find an interesting book \n" +
                    $"/description - show description for this bot \n" +
                    $"/help - show available commands");
            }
            else if (msg.Text == "/findbook")
            {
                await bot.SendTextMessageAsync(msg.Chat, "Choose one option:",
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("Find by author", "Find by genre").AddNewRow().AddButtons("Find by year", "Find by all categories"));
            }
            else if (msg.Text == "/description")
            {
                await bot.SendTextMessageAsync(msg.Chat, "I'm a bot created to help you find interesting books. \n" +
                                                        "I offer options for finding books by different categories. \n" +
                                                        "Start by pressing or typing /findbook to explore.");
            }
            else if (msg.Text == "/help")
            {
                await bot.SendTextMessageAsync(msg.Chat, "Here is my command list: \n" +
                                                        "/start - start interacting with me to find some books :) \n" +
                                                        "/findbook - search for books by various categories \n" +
                                                        "/description - get a description of what I can do :)");
            }
            else if (currentOperation == "find_by_author")
            {
                // User is providing the author name
                string authorName = msg.Text;

                if (string.IsNullOrEmpty(authorName))
                {
                    await bot.SendTextMessageAsync(msg.Chat, "You didn't provide an author name :(");
                }
                else
                {
                    // Query the database to find books by the given author
                    var resultBooks = _db.Books.Where(r => r.Author.Contains(authorName)).ToList();
                    string resultMessage = "Here are the books by the author:\n";

                    if (resultBooks.Count == 1)
                    {
                        resultMessage += $"Book title - {resultBooks[0].Title} \n" +
                                                $"Book author - {resultBooks[0].Author} \n" +
                                                $"Book genres - {resultBooks[0].Genres} \n" +
                                                $"Book published year - {resultBooks[0].Year} \n" +
                                                $"Choose next option: \n";

                        await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup().AddButtons("Repeat this author", "Write another").AddNewRow().AddButton("Came back to /start"));
                    }
                    else if(resultBooks.Count > 1)
                    {
                        int randomBook = random.Next(0, resultBooks.Count);

                        resultMessage += $"Book title - {resultBooks[randomBook].Title} \n" +
                                                $"Book author - {resultBooks[randomBook].Author} \n" +
                                                $"Book genres - {resultBooks[randomBook].Genres} \n" +
                                                $"Book published year - {resultBooks[randomBook].Year} \n" +
                                                $"Choose next option: \n";

                        resultBooks.Remove(resultBooks[randomBook]);

                        await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup().AddButtons("Repeat this author", "Write another").AddNewRow().AddButton("Came back to /start"));
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(msg.Chat, "No books found for this author.");
                    }
                }

                // Reset the operation after completion
                currentOperation = null;
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "Please choose a valid command or write /start.");
            }
        }

        private static async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                await bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");
                await bot.SendTextMessageAsync(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");

                if (query.Data.Contains("author"))
                {
                    // Set the operation context
                    currentOperation = "find_by_author";

                    // Ask the user to provide the author's name
                    await bot.SendTextMessageAsync(query.Message.Chat, "Please write the author's name:");
                }
            }
        }
    }
}