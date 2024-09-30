# 📚 Books Recommendation Bot
<p>Welcome to the <strong>Books Recommendation Bot</strong>! This Telegram bot helps you discover interesting books by filtering them by author, genre, or year. It’s designed to make finding your next read easy. In this project, I use MS SQL, .NET 8, LINQ, and the last version of Telegram Bot Library (21.11.0)</p>
<h1>Features 🪛</h1>
<ul>
  <li>🔎 Find books: Search for books by author, genre, or year</li>
  <li>📋 Author List: In bot you can find a link to view a list of authors</li>
  <li>🎮 Interactive: The bot includes interactive buttons and choices </li>
  <li>🎯 Genre Selection: Choose up to two genres available in the database to find books</li>
  <li>📆 Year Filter: Enter 1 or 2 dates to search for books</li>
  <li>🗄️ Books Database: The bot fetches data from an internal BooksDB for recommendations</li>
</ul>

<h1>Setup Instructions ⚙️</h1>
<ol>
  <li>Clone the repository by pressing green button <code>Code🟩</code></li>
  <li>Set up your bot token in the <code>_botClient</code> field in <code>Main</code>. For better security (since it's hardcoded in this code), you can change it to <code>Environment.GetEnvironmentVariable("")</code> or configure it in your <code>appsettings.json</code></li>
  <li>Change connection string in <code>BooksDB</code> to match your configuration</li>
  <li>Finally, run your telegram bot😄</li>
</ol>

<h1>Commands Overview 🗔</h1>
<ul>
  <li><strong>/start</strong> - get a welcome message with available options</li>
  <li><strong>/findbook</strong> - start the process of finding book by selecting author, genre, or year</li>
  <li><strong>/description</strong> - learn about the bot</li>
  <li><strong>/help</strong> - get a list of all available commands</li>
</ul>

<h1>How It Works ❓</h1>
<ol>
  <li><strong>Start the Bot:</strong> Type /start to receive introduction and a menu of options</li>
  <li>Press <code>/findbook</code></li>
  <li>Choose Your Search Method: 
    <ul>
      <li><strong>Author</strong>: Enter the name of the author, if you can't find author go to the view with authors list</li>
      <li><strong>Genre</strong>: Select from a list of genres
      <li><strong>Year</strong>: Choose a specific year or range
    </ul>
  </li>
  <li>Get your recommendation 😆</li>
</ol>

<h1>Contributing 🤝</h1>
<p>Feel free to open issues or submit pull requests for any suggestions or improvements. I welcome your feedback! 😃</p>
<p>You can send me mail: arsenburko67@gmail.com or find me in telegram @arssssssssssssssssssss</p>
