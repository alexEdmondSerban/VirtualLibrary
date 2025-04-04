using System;
using System.ComponentModel.DataAnnotations;
namespace VirtualLibrary.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Author { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        [StringLength(50)]
        public Genre Genre { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        public string FilePath { get; set; }

        public string? CoverImagePath { get; set; }

        public string UploadedByUserId { get; set; }
    }
}
