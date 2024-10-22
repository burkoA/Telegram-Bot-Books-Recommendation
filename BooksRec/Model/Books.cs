﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BooksRec.Model
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

        public string Description { get; set; }
    }
}