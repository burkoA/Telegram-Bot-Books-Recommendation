using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksRec.BooksLibraryContext
{
    internal class BooksDB : DbContext
    {
        public BooksDB() 
        {
            Database.EnsureCreated();
        }

        public DbSet<Book> Books { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=MSI\\SQLEXPRESS; Initial Catalog = BookLibrary; Integrated Security=True;Trust Server Certificate=True");
        }
    }
}
