using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BooksRec.BooksLibraryContext
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        public string Genres { get; set; }

        public string Author { get; set; }

        public int Year { get; set; }

        public double Rating { get; set; }
    }
}