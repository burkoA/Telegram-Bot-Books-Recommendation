using BooksRec.BooksLibraryContext;
using BooksRec.Model;
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
        private static TelegramBotClient _botClient;
        private static User _botInfo;
        private static Random _random = new Random();

        private static Operation _currentUserOperation = Operation.None;
        private static List<Book> _booksList = new List<Book>();

        private static Dictionary<long, List<string>> _userSelectedGenres = new Dictionary<long, List<string>>();
        private static Dictionary<long, List<Book>> _userBooksList = new Dictionary<long, List<Book>>();

        private const string linkToAuthors = "https://telegra.ph/List-of-Author-for-BookRecBot-bot-09-27";

        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            _botClient = new TelegramBotClient("TOKEN");

            _botInfo = await _botClient.GetMeAsync();
            Console.WriteLine($"@{_botInfo.Username} is running... Press Enter to terminate");

            _botClient.OnError += OnError;
            _botClient.OnMessage += OnMessage;
            _botClient.OnUpdate += OnUpdate;

            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
        }

        private static async Task OnMessage(Message msg, UpdateType type)
        {
            switch(msg.Text)
            {
                case "/start":
                    await SendStartMessage(msg);
                    break;
                case "/findbook":
                    await SendFindBookOptions(msg);
                    break;
                case "/help":
                    await SendHelpMessage(msg);
                    break;
                case "/description":
                    await SendDescriptionMessage(msg);
                    break;
            }
            
            if (_currentUserOperation == Operation.FindByAuthor)
            {
                string authorName = msg.Text;

                if (string.IsNullOrEmpty(authorName))
                {
                    await _botClient.SendTextMessageAsync(msg.Chat, "You didn't provide an author name :(", 
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("Find by author","Back to /start"));
                }
                else
                {
                    _booksList = _db.Books.Where(r => r.Author.Contains(authorName)).ToList();

                    await GetBooksAsync(_booksList, msg, "author");
                }
                _currentUserOperation = Operation.None;
            }
            else if (_currentUserOperation == Operation.FindByYear)
            {
                string writtenYear = msg.Text;

                if (string.IsNullOrEmpty(writtenYear))
                {
                    await _botClient.SendTextMessageAsync(msg.Chat, "You didn't provide a year :(");
                }
                else
                {
                    await YearCheck(writtenYear, msg);
                }

                _currentUserOperation = Operation.None;
            }
        }

        private static async Task SendStartMessage(Message message)
        {
            string welcomeText = $"Hi! I'm {_botInfo.Username}. \n I'm here to help you find a book for reading :) \n" +
                    $"Please choose an option below to continue: \n" +
                    $"/findbook - helps you find an interesting book \n" +
                    $"/description - show description for this bot \n" +
                    $"/help - show available commands";

            await _botClient.SendTextMessageAsync(message.Chat, welcomeText);
        }

        private static async Task SendFindBookOptions(Message message)
        {
            InlineKeyboardMarkup options = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Find by author"), InlineKeyboardButton.WithCallbackData("Find by genre") },
                new[] { InlineKeyboardButton.WithCallbackData("Find by year") }
            });

            await _botClient.SendTextMessageAsync(message.Chat, "Choose a category:", replyMarkup: options);
        }

        private static async Task SendHelpMessage(Message message)
        {
            string helpText = "Here are the commands you can use:\n" +
                              "/start - Restart interaction\n" +
                              "/findbook - Find books by author, genre, or year\n" +
                              "/description - Get to know about this bot\n" +
                              "/help - Show this help menu";

            await _botClient.SendTextMessageAsync(message.Chat, helpText);
        }

        private static async Task SendDescriptionMessage(Message message)
        {
            string descriptionText = "I'm a bot created to help you find interesting books. :)\n" +
                "I offer options for finding books by different categories. 0_0\n" +
                "Start by pressing or typing /findbook to explore. <-";

            await _botClient.SendTextMessageAsync(message.Chat, descriptionText);
        }

        private static async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                //Find by Author

                if (query.Data!.Contains("Find by author") || query.Data.Contains("another author"))
                {
                    _currentUserOperation = Operation.FindByAuthor;

                    await _botClient.SendTextMessageAsync(query.Message.Chat, "Please write the author's name: \n" +
                        $"If you can't find author, check this list :) - {linkToAuthors}");
                } 

                if (query.Data.Contains("Repeat author"))
                {
                    await GetBooksAsync(_booksList, query.Message, "author");
                }

                //Find by Year

                if (query.Data.Contains("Find by year"))
                {
                    int currentYear = DateTime.Now.Year;

                    await _botClient.SendTextMessageAsync(query.Message.Chat, "Please choose range of year or write year by yourself:",
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
                    _currentUserOperation = Operation.FindByYear;

                    await _botClient.SendTextMessageAsync(query.Message.Chat, "Please, write year");
                }

                if(query.Data.Contains("Repeat year"))
                {
                    await GetBooksAsync(_booksList, query.Message,"year");
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

                    _userSelectedGenres[query.Message.Chat.Id] = new List<string>();

                    await _botClient.SendTextMessageAsync(query.Message.Chat, "Choose genre (max - 2)",
                        replyMarkup: GetGenreList(genreList).AddNewRow().AddButton("End choose"));
                }
                else if (query.Data.Contains("End choose"))
                {
                    await GetBookByGenreAsync(_userSelectedGenres, query);
                }
                else if (query.Data.Contains("Repeat this genre"))
                {
                    await GenresRepeatAsync(query);
                }
                else if (_userSelectedGenres.TryGetValue(query.Message.Chat.Id, out List<string> userGenres))
                {
                    string selectedGenre = query.Data;

                    if (!userGenres.Contains(selectedGenre))
                    {
                        userGenres.Add(selectedGenre);
                        await _botClient.SendTextMessageAsync(query.Message.Chat.Id, $"You selected: {selectedGenre}. Click 'End Choose' if you want only 1 genre :)");

                        if (userGenres.Count == 2)
                        {
                            await _botClient.SendTextMessageAsync(query.Message.Chat.Id, "You have selected two genres. Click 'End choose' to continue.");
                        }
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(query.Message.Chat.Id, "You have already selected this genre.");
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

        private static async Task GenresRepeatAsync(CallbackQuery callback)
        {
            if (_userBooksList.TryGetValue(callback.Message.Chat.Id, out List<Book> booksList) && booksList.Count > 0)
            {
                Book randomBook = booksList[_random.Next(booksList.Count)];

                string resultMessage = $"Book title - {randomBook.Title} \n" +
                                       $"Book author - {randomBook.Author} \n" +
                                       $"Book genres - {randomBook.Genres} \n" +
                                       $"Book published year - {randomBook.Year} \n" +
                                       $"Book description - {randomBook.Description} \n\n" +
                                       $"Choose next option: \n";

                await _botClient.SendTextMessageAsync(callback.Message.Chat.Id, resultMessage,
                    replyMarkup: new InlineKeyboardMarkup()
                        .AddButton("Repeat this genre").AddNewRow().AddButton("Find by genre")
                        .AddNewRow().AddButton("Came back to /start"));
            }
            else
            {
                await _botClient.SendTextMessageAsync(callback.Message.Chat.Id, "No more books available for the selected genre. :(", replyMarkup: new InlineKeyboardMarkup().AddButton("Find by genre"));
            }
        }

        private static async Task GetBookByGenreAsync(Dictionary<long,List<string>> selectGenres, CallbackQuery update)
        {
            if (_userSelectedGenres.TryGetValue(update.Message.Chat.Id, out List<string> selectedGenres) && selectedGenres.Count > 0)
            {
                string selectedGenresText = string.Join(", ", selectedGenres);

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

                _userBooksList[update.Message.Chat.Id] = booksList;

                if (booksList.Count > 0)
                {
                    Book randomBook = booksList[_random.Next(booksList.Count)];

                    string resultMessage = $"Book title - {randomBook.Title} \n" +
                                           $"Book author - {randomBook.Author} \n" +
                                           $"Book genres - {randomBook.Genres} \n" +
                                           $"Book published year - {randomBook.Year} \n" +
                                           $"Book description - {randomBook.Description} \n\n" +
                    $"Choose next option: \n";

                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, resultMessage,
                        replyMarkup: new InlineKeyboardMarkup()
                            .AddButton("Repeat this genre").AddNewRow().AddButton("Find by genre")
                            .AddNewRow().AddButton("Came back to /start"));
                }
                else
                {
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "No books found for the selected genres. :(",
                        replyMarkup: new InlineKeyboardMarkup().AddButton("Find by genre"));
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "You haven't chosen any genres :(", replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("Find by genre", "Back to /start"));
            }
        }

        private static async Task YearCheck(string userMessage, Message msg)
        {
            if (userMessage.Contains("-"))
            {
                string[] years = userMessage.Split('-');

                if (years[0].GetType() == typeof(int) || years[1].GetType() == typeof(int))
                {
                    if (years.Length == 2)
                    {
                        int startYear, endYear;

                        if (int.TryParse(years[0].Trim(), out startYear) && int.TryParse(years[1].Trim(), out endYear))
                        {
                            _booksList = _db.Books.Where(y => y.Year >= startYear && y.Year <= endYear).ToList();

                            await GetBooksAsync(_booksList, msg, "year");
                        }
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(msg.Chat, "Invalid format of date (e.g 1950 - 1960 or 1960)");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(msg.Chat, "Wrong type :(", replyMarkup: new InlineKeyboardMarkup().AddButton("Find by year"));
                }
            }
            else
            {
                if(int.TryParse(userMessage, out int year))
                {
                    _booksList = _db.Books.Where(y => y.Year == year).ToList();

                    await GetBooksAsync(_booksList, msg, "year");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(msg.Chat, "Wrong type :(", replyMarkup: new InlineKeyboardMarkup().AddButton("Find by year").AddNewRow().AddButton("Back to /start"));
                }
            }
        }

        private static async Task GetBooksAsync(List<Book> result, Message msg, string searchType)
        {
            string resultMessage = $"Here are the books by {searchType}:\n";

            if (result.Count == 1)
            {
                resultMessage += $"Book title - {result[0].Title} \n" +
                                        $"Book author - {result[0].Author} \n" +
                                        $"Book genres - {result[0].Genres} \n" +
                                        $"Book published year - {result[0].Year} \n" +
                                        $"Book description - {result[0].Description} \n\n" +
                                        $"Choose next option: \n";

                await _botClient.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton($"Repeat {searchType}(books can repeat)").AddNewRow()
                    .AddButton($"Write another {searchType}")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else if (result.Count > 1)
            {
                int randomBook = _random.Next(0, result.Count);

                resultMessage += $"Book title - {result[randomBook].Title} \n" +
                                        $"Book author - {result[randomBook].Author} \n" +
                                        $"Book genres - {result[randomBook].Genres} \n" +
                                        $"Book published year - {result[randomBook].Year} \n" +
                                        $"Book description - {result[randomBook].Description} \n\n" +
                                        $"Choose next option: \n";

                await _botClient.SendTextMessageAsync(msg.Chat, resultMessage, replyMarkup: new InlineKeyboardMarkup()
                    .AddButton($"Repeat {searchType}(books can repeat)").AddNewRow()
                    .AddButton($"Write another {searchType}")
                    .AddNewRow().AddButton("Came back to /start"));
            }
            else
            {
                await _botClient.SendTextMessageAsync(msg.Chat, $"No books found for this {searchType}. :(", replyMarkup:
                    new InlineKeyboardMarkup().AddButton($"Write another {searchType}").AddNewRow()
                    .AddButton("Came back to /start"));
            }
        }

        private static async Task HandleYearRangeQuery(Message msg, int startYear, int endYear)
        {
            _booksList = _db.Books.Where(b => b.Year >= startYear && b.Year <= endYear).ToList();
            await GetBooksAsync(_booksList, msg, "year");
        }
    }
}
