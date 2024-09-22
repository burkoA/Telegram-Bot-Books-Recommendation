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

        private static string currentOperation = null;
        private static List<Book> booksList = new List<Book>();

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
                    booksList = _db.Books.Where(r => r.Author.Contains(authorName)).ToList();

                    await GetBookByAuthor(booksList, booksList.Count,msg);
                }
                currentOperation = null;
            }
            else if (currentOperation == "find_by_data")
            {
                string writtenYear = msg.Text;

                if (string.IsNullOrEmpty(writtenYear))
                {
                    await bot.SendTextMessageAsync(msg.Chat, "You didn't provide a year :(");
                }
                else
                {

                    if(writtenYear.Contains("-"))
                    {
                        string[] years = writtenYear.Split('-');

                        if(years.Length == 2)
                        {
                            int startYear, endYear;

                            if (int.TryParse(years[0].Trim(), out startYear) && int.TryParse(years[1].Trim(), out endYear))
                            {
                                booksList = _db.Books.Where(y => y.Year >= startYear && y.Year <= endYear).ToList();

                                await GetBookByYear(booksList, booksList.Count, msg);
                            }
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(msg.Chat, "Invalid format of date (e.g 1950 - 1960 or 1960)");
                        }
                    }
                    else
                    {
                        int year = int.Parse(writtenYear);

                        if(year.GetType() != typeof(int))
                        {
                            await bot.SendTextMessageAsync(msg.Chat, "Wrong type :(");
                        } 
                        else
                        {
                            booksList = _db.Books.Where(y => y.Year == year).ToList();

                            await GetBookByYear(booksList, booksList.Count, msg);
                        }
                    }

                }

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
                await bot.SendTextMessageAsync(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");

                //Find by Author

                if (query.Data!.Contains("Find by author") || query.Data.Contains("another author"))
                {
                    currentOperation = "find_by_author";

                    await bot.SendTextMessageAsync(query.Message.Chat, "Please write the author's name:");
                } 

                if (query.Data.Contains("Repeat author"))
                {
                    await GetBookByAuthor(booksList, booksList.Count, query.Message);
                }

                //Find by Year

                if (query.Data.Contains("Find by year"))
                {
                    int currentYear = DateTime.Now.Year;

                    await bot.SendTextMessageAsync(query.Message.Chat, "Please choose range of year or write year by yourself:",
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("1950 - 1970", "1970 - 1990").AddNewRow()
                        .AddButtons("1990 - 2010",$"2010 - {currentYear.ToString()}").AddNewRow().AddButton("Write yourself"));
                }
                else if (query.Data == "1950 - 1970")
                {
                    await HandleYearRangeQuery(query.Message, 1950, 1970);
                }
                else if (query.Data == "1970 - 1990")
                {
                    await HandleYearRangeQuery(query.Message, 1970, 1990);
                }
                else if (query.Data == "1990 - 2010")
                {
                    await HandleYearRangeQuery(query.Message, 1990, 2010);
                }
                else if (query.Data == $"2010 - {DateTime.Now.Year}")
                {
                    await HandleYearRangeQuery(query.Message, 2010, DateTime.Now.Year);
                }


                if (query.Data.Contains("Write yourself") || query.Data.Contains("another year"))
                {
                    currentOperation = "find_by_data";

                    await bot.SendTextMessageAsync(query.Message.Chat, "Please, write year");
                }

                if(query.Data.Contains("Repeat year"))
                {
                    await GetBookByYear(booksList, booksList.Count, query.Message);
                }

                //Start command

                if (query.Data.Contains("/start"))
                {
                    Message msg = new Message()
                    {
                        Chat = query.Message.Chat,
                        Text = "/start"
                    };

                    await OnMessage(msg, update.Type);
                }

            }
        }

        private static async Task GetBookByAuthor(List<Book> result, int count, Message msg)
        {
            string resultMessage = "Here are the books by the author:\n";

            if (result.Count == 1)
            {
                resultMessage += $"Book title - {result[0].Title} \n" +
                                        $"Book author - {result[0].Author} \n" +
                                        $"Book genres - {result[0].Genres} \n" +
                                        $"Book published year - {result[0].Year} \n" +
                                        $"Choose next option: \n";

                await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("Repeat author (books can repeat)").AddNewRow().AddButton("Write another author")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else if (result.Count > 1)
            {
                int randomBook = random.Next(0, result.Count);

                resultMessage += $"Book title - {result[randomBook].Title} \n" +
                                        $"Book author - {result[randomBook].Author} \n" +
                                        $"Book genres - {result[randomBook].Genres} \n" +
                                        $"Book published year - {result[randomBook].Year} \n" +
                                        $"Choose next option: \n";

                await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("Repeat author(books can repeat)").AddNewRow().AddButton("Write another author")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "No books found for this author.", replyMarkup: 
                    new InlineKeyboardMarkup().AddButton("Write another author").AddNewRow()
                    .AddButton("Came back to /start"));
            }
        }

        private static async Task GetBookByYear(List<Book> result, int count, Message msg)
        {
            string resultMessage = "Here are the books by the year:\n";

            if (result.Count == 1)
            {
                resultMessage += $"Book title - {result[0].Title} \n" +
                                        $"Book author - {result[0].Author} \n" +
                                        $"Book genres - {result[0].Genres} \n" +
                                        $"Book published year - {result[0].Year} \n" +
                                        $"Choose next option: \n";

                await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("Repeat year(books can repeat)").AddNewRow().AddButton("Write another year")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else if (result.Count > 1)
            {
                int randomBook = random.Next(0, result.Count);

                resultMessage += $"Book title - {result[randomBook].Title} \n" +
                                        $"Book author - {result[randomBook].Author} \n" +
                                        $"Book genres - {result[randomBook].Genres} \n" +
                                        $"Book published year - {result[randomBook].Year} \n" +
                                        $"Choose next option: \n";

                await bot.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("Repeat year(books can repeat)").AddNewRow().AddButton("Write another year")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "No books found for this year.", replyMarkup: 
                    new InlineKeyboardMarkup().AddButton("Write another year").AddNewRow()
                    .AddButton("Came back to /start"));
            }
        }

        private static async Task HandleYearRangeQuery(Message msg, int startYear, int endYear)
        {
            booksList = _db.Books.Where(b => b.Year >= startYear && b.Year <= endYear).ToList();
            await GetBookByYear(booksList, booksList.Count, msg);
        }
    }
}