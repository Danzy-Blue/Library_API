using AutoMapper;
using Library.API.Entities;
using Library.API.Services;
using Library_API.Helpers;
using Library_API.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Controllers
{
    // api/authors/76053df4-6687-4353-8937-b45556748abe/books
    [Route("api/authors/{authorID}/books")]
    public class BooksController : Controller
    {
        ILogger<BooksController> logger;
        public ILibraryRepository libraryRepository;
        public BooksController(ILibraryRepository libraryRepository, ILogger<BooksController> logger)
        {
            this.logger = logger;
            this.libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorID)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var booksForAuthorFromRepo = libraryRepository.GetBooksForAuthor(authorID);
            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            ////JsonResult result = new JsonResult(booksForAuthor);
            ////return result;

            return Ok(booksForAuthor); ;
        }

        [HttpGet("{bookID}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorID, Guid bookID)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorID, bookID);
            var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            ////JsonResult result = new JsonResult(booksForAuthor);
            ////return result;

            return Ok(bookForAuthor);
        }

        [HttpPost()]
        public IActionResult CreateBookForAuthor(Guid authorID, [FromBody] BookCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var bookEntity = Mapper.Map<Book>(book);

            libraryRepository.AddBookForAuthor(authorID, bookEntity);
            if (!libraryRepository.Save())
            {
                throw new Exception("Error !!!");
            }

            var bookDto = Mapper.Map<BookDto>(bookEntity);

            // below line of code provide location URL in response body for newly created resource
            return CreatedAtRoute("GetBookForAuthor", new { authorID = authorID, bookID = bookDto.Id }, bookDto);
        }

        [HttpDelete("{bookID}")]
        public IActionResult DeleteBookForAuthor(Guid authorID, Guid bookID)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var bookforAuthor = libraryRepository.GetBookForAuthor(authorID, bookID);
            if (bookforAuthor == null)
            {
                return NotFound();
            }

            libraryRepository.DeleteBook(bookforAuthor);
            if (!libraryRepository.Save())
            {
                throw new Exception($"deleting book {bookID} for author{authorID} failed on save");
            }

            return NoContent();
        }

        [HttpPut("{bookID}")]
        public IActionResult UpdateBookForAuthor(Guid authorID, Guid bookID, [FromBody] BookUpdationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var bookforAuthor = libraryRepository.GetBookForAuthor(authorID, bookID);
            if (bookforAuthor == null)
            {
                // below piece of code defines UPSERTING in  put, for complete IF block
                var bookToAdd = Mapper.Map<BookUpdationDto, Book>(book);
                bookToAdd.Id = bookID;

                libraryRepository.AddBookForAuthor(authorID, bookToAdd);

                if (!libraryRepository.Save())
                {
                    throw new Exception($"updating book {bookID} for author{authorID} failed on save");
                }

                var bookToReturn = Mapper.Map<Book, BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new { authorID = bookToReturn.AuthorId, bookID = bookToReturn.Id }, bookToReturn);
            }

            Mapper.Map<BookUpdationDto, Book>(book, bookforAuthor);

            libraryRepository.UpdateBookForAuthor(bookforAuthor);
            if (!libraryRepository.Save())
            {
                throw new Exception($"updating book {bookID} for author{authorID} failed on save");
            }

            return NoContent();
        }


        [HttpPatch("{bookID}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorID, Guid bookID, [FromBody] JsonPatchDocument<BookUpdationDto> patchDoc)
        {
            //always use content type (media type) as application/json-patch+json
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();
            }

            var bookforAuthor = libraryRepository.GetBookForAuthor(authorID, bookID);
            if (bookforAuthor == null)
            {
                //return NotFound();
                // UPSERTING with PATCH in this if block
                var bookDto = new BookUpdationDto();
                patchDoc.ApplyTo(bookDto);

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = bookID;

                libraryRepository.AddBookForAuthor(authorID, bookforAuthor);
                if (!libraryRepository.Save())
                {
                    throw new Exception($"patching book {bookID} for author{authorID} failed on save");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorID = authorID, bookID = bookToReturn.Id }, bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookUpdationDto>(bookforAuthor);
            ////patchDoc.ApplyTo(bookToPatch, ModelState);

            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Title == bookToPatch.Description)
            {
                ModelState.AddModelError(nameof(BookUpdationDto), "title and description cant be same");
            }

            TryValidateModel(bookToPatch);


            //add validation
            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }


            Mapper.Map<BookUpdationDto, Book>(bookToPatch, bookforAuthor);

            libraryRepository.UpdateBookForAuthor(bookforAuthor);
            if (!libraryRepository.Save())
            {
                throw new Exception($"patching book {bookID} for author{authorID} failed on save");
            }

            return NoContent();
        }

    }
}
