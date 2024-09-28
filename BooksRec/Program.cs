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
        enum Operation
        {
            None,
            FindByAuthor,
            FindByYear
        }

        private static readonly BooksDB _db = new BooksDB();
        private static TelegramBotClient bot;
        private static User me;
        private static Random random = new Random();

        private static Operation currentOperation = Operation.None;
        private static List<Book> booksList = new List<Book>();

        private static Dictionary<long, List<string>> userSelectedGenres = new Dictionary<long, List<string>>();
        private static Dictionary<long, List<Book>> userBooksList = new Dictionary<long, List<Book>>();

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
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("Find by author", "Find by genre").AddNewRow().AddButton("Find by year"));
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
            else if (currentOperation == Operation.FindByAuthor)
            {
                string authorName = msg.Text;

                if (string.IsNullOrEmpty(authorName))
                {
                    await bot.SendTextMessageAsync(msg.Chat, "You didn't provide an author name :(");
                }
                else
                {
                    booksList = _db.Books.Where(r => r.Author.Contains(authorName)).ToList();

                    await GetBookByAuthorAsync(booksList, booksList.Count,msg);
                }
                currentOperation = Operation.None;
            }
            else if (currentOperation == Operation.FindByYear)
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

                                await GetBookByYearAsync(booksList, booksList.Count, msg);
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

                            await GetBookByYearAsync(booksList, booksList.Count, msg);
                        }
                    }

                }

                currentOperation = Operation.None;
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
                    currentOperation =Operation.FindByAuthor;

                    await bot.SendTextMessageAsync(query.Message.Chat, "Please write the author's name: \n" +
                        "If you can't find author, check this list :) - https://telegra.ph/List-of-Author-for-BookRecBot-bot-09-27");
                } 

                if (query.Data.Contains("Repeat author"))
                {
                    await GetBookByAuthorAsync(booksList, booksList.Count, query.Message);
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
                    currentOperation = Operation.FindByYear;

                    await bot.SendTextMessageAsync(query.Message.Chat, "Please, write year");
                }

                if(query.Data.Contains("Repeat year"))
                {
                    await GetBookByYearAsync(booksList, booksList.Count, query.Message);
                }

                //By Genre

                if (query.Data.Contains("Find by genre"))
                {
                    List<string> genreList = _db.Books
                                 .AsEnumerable()
                                 .SelectMany(book => book.Genres.Split(','))
                                 .Select(genre => genre.Trim())
                                 .Distinct()
                                 .ToList();

                    userSelectedGenres[query.Message.Chat.Id] = new List<string>();

                    await bot.SendTextMessageAsync(query.Message.Chat, "Choose genre (max - 2)",
                        replyMarkup: GetGenreList(genreList).AddNewRow().AddButton("End choose"));
                }
                else if (query.Data.Contains("End choose"))
                {
                    if (userSelectedGenres.TryGetValue(query.Message.Chat.Id, out List<string> selectedGenres) && selectedGenres.Count > 0)
                    {
                        string selectedGenresText = string.Join(", ", selectedGenres);
                        await bot.SendTextMessageAsync(query.Message.Chat.Id, $"You chose genres: {selectedGenresText}");

                        IQueryable<Book> queryBooks = _db.Books.AsQueryable();

                        if (selectedGenres.Count == 1)
                        {
                            string genre = selectedGenres[0];
                            queryBooks = queryBooks.Where(g => g.Genres.Contains(genre));
                        }
                        else if (selectedGenres.Count == 2)
                        {
                            string genre1 = selectedGenres[0];
                            string genre2 = selectedGenres[1];
                            queryBooks = queryBooks.Where(g => g.Genres.Contains(genre1) && g.Genres.Contains(genre2));
                        }

                        var booksList = queryBooks.ToList();

                        userBooksList[query.Message.Chat.Id] = booksList;

                        if (booksList.Count > 0)
                        {
                            Random random = new Random();
                            Book randomBook = booksList[random.Next(booksList.Count)];

                            string resultMessage = $"Book title - {randomBook.Title} \n" +
                                                   $"Book author - {randomBook.Author} \n" +
                                                   $"Book genres - {randomBook.Genres} \n" +
                                                   $"Book published year - {randomBook.Year} \n" +
                                                   $"Book description - {randomBook.Description} \n\n" +
                                                   $"Choose next option: \n";

                            await bot.SendTextMessageAsync(query.Message.Chat.Id, resultMessage,
                                replyMarkup: new InlineKeyboardMarkup()
                                    .AddButton("Repeat this genre").AddNewRow().AddButton("Find by genre")
                                    .AddNewRow().AddButton("Came back to /start"));
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(query.Message.Chat.Id, "No books found for the selected genres.", replyMarkup: new InlineKeyboardMarkup().AddButton("Find by genre"));
                        }
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(query.Message.Chat.Id, "You haven't chosen any genres :(");
                    }
                }
                else if (query.Data.Contains("Repeat this genre"))
                {
                    if (userBooksList.TryGetValue(query.Message.Chat.Id, out List<Book> booksList) && booksList.Count > 0)
                    {
                        Random random = new Random();
                        Book randomBook = booksList[random.Next(booksList.Count)];

                        string resultMessage = $"Book title - {randomBook.Title} \n" +
                                               $"Book author - {randomBook.Author} \n" +
                                               $"Book genres - {randomBook.Genres} \n" +
                                               $"Book published year - {randomBook.Year} \n" +
                                               $"Book description - {randomBook.Description} \n\n" +
                                               $"Choose next option: \n";

                        await bot.SendTextMessageAsync(query.Message.Chat.Id, resultMessage,
                            replyMarkup: new InlineKeyboardMarkup()
                                .AddButton("Repeat this genre").AddNewRow().AddButton("Find by genre")
                                .AddNewRow().AddButton("Came back to /start"));
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(query.Message.Chat.Id, "No more books available for the selected genre.", replyMarkup: new InlineKeyboardMarkup().AddButton("Find by genre"));
                    }
                }
                else if (userSelectedGenres.TryGetValue(query.Message.Chat.Id, out List<string> userGenres))
                {
                    string selectedGenre = query.Data;

                    if (!userGenres.Contains(selectedGenre))
                    {
                        userGenres.Add(selectedGenre);
                        await bot.SendTextMessageAsync(query.Message.Chat.Id, $"You selected: {selectedGenre}");

                        if (userGenres.Count == 2)
                        {
                            await bot.SendTextMessageAsync(query.Message.Chat.Id, "You have selected two genres. Click 'End choose' to continue.");
                        }
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(query.Message.Chat.Id, "You have already selected this genre.");
                    }
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

        private static InlineKeyboardMarkup GetGenreList(List<string> genreStringList)
        {
            List<List<InlineKeyboardButton>> markups = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < genreStringList.Count; i += 2)
            {
                List<InlineKeyboardButton> markupRow = new List<InlineKeyboardButton>();

                markupRow.Add(InlineKeyboardButton.WithCallbackData(genreStringList[i], genreStringList[i]));

                if (i + 1 < genreStringList.Count)
                {
                    markupRow.Add(InlineKeyboardButton.WithCallbackData(genreStringList[i + 1], genreStringList[i + 1]));
                }

                markups.Add(markupRow);
            }

            InlineKeyboardMarkup replyMark = new InlineKeyboardMarkup(markups);

            return replyMark;
        }

        private static async Task GetBookByAuthorAsync(List<Book> result, int count, Message msg)
        {
            string resultMessage = "Here are the books by the author:\n";

            if (result.Count == 1)
            {
                resultMessage += $"Book title - {result[0].Title} \n" +
                                        $"Book author - {result[0].Author} \n" +
                                        $"Book genres - {result[0].Genres} \n" +
                                        $"Book published year - {result[0].Year} \n" +
                                        $"Book description - {result[0].Description} \n\n" +
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
                                        $"Book description - {result[randomBook].Description} \n\n" +
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

        private static async Task GetBookByYearAsync(List<Book> result, int count, Message msg)
        {
            string resultMessage = "Here are the books by the year:\n";

            if (result.Count == 1)
            {
                resultMessage += $"Book title - {result[0].Title} \n" +
                                        $"Book author - {result[0].Author} \n" +
                                        $"Book genres - {result[0].Genres} \n" +
                                        $"Book published year - {result[0].Year} \n" +
                                        $"Book description - {result[0].Description} \n\n" +
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
                                        $"Book description - {result[randomBook].Description} \n\n" +
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
            await GetBookByYearAsync(booksList, booksList.Count, msg);
        }
    }
}