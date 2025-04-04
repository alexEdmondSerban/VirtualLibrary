using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using VirtualLibrary.Models;

namespace VirtualLibrary.ViewModels
{
    public class BookUploadViewModel
    {
        [Required]
        public string Title { get; set; }

        public string Author { get; set; }

        public string Description { get; set; }

        [Required]
        public Genre Genre { get; set; }

        [Display(Name = "Cover Image")]
        public IFormFile? CoverImage { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}
