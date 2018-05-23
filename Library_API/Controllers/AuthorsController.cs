using AutoMapper;
using Library.API.Entities;
using Library.API.Services;
using Library_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        public ILibraryRepository libraryRepository;
        public AuthorsController(ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
        }

        [HttpGet("{authorID}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid authorID)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();

            }

            var authorFromRepo = libraryRepository.GetAuthor(authorID);
            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(author);
            ////return new JsonResult(author);

        }

        [HttpGet()]
        public IActionResult GetAuthors()
        {

            var authorsFromRepo = libraryRepository.GetAuthors();
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return new JsonResult(authors);

        }

        //// creating parent resource without child resorce
        ////[HttpPost()]
        ////public IActionResult CreateAuthor([FromBody] AuthorCreationDto author)
        ////{
        ////    if (author == null)
        ////    {
        ////        return BadRequest();
        ////    }

        ////    var authorEntity = Mapper.Map<Author>(author);
        ////    libraryRepository.AddAuthor(authorEntity);

        ////    if (!libraryRepository.Save())
        ////    {
        ////        throw new Exception("error !!!");
        ////        ////return StatusCode(500, "some error");
        ////    }

        ////    var authorDto = Mapper.Map<AuthorDto>(authorEntity);

        ////    // below line of code provide location URL in response body for newly created resource
        ////    // return CreatedAtRoute("GetAuthor", new { authorID = authorDto.Id }, authorDto);

        ////    return new JsonResult(authorDto);

        ////}


        // creating child resorce togather with parent resource
        // i.e. creating Auther and book togather
        [HttpPost()]
        public IActionResult CreateAuthor([FromBody] AuthorCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);
            libraryRepository.AddAuthor(authorEntity);

            if (!libraryRepository.Save())
            {
                throw new Exception("error !!!");
                ////return StatusCode(500, "some error");
            }

            var authorDto = Mapper.Map<AuthorDto>(authorEntity);

            // below line of code provide location URL in response body for newly created resource
            // return CreatedAtRoute("GetAuthor", new { authorID = authorDto.Id }, authorDto);

            return new JsonResult(authorDto);

        }


        [HttpPost("{authorID}")]
        public IActionResult BlockAuthor(Guid authorID)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{authorID}")]
        public IActionResult DeleteAuthor(Guid authorID)
        {
            var author = libraryRepository.GetAuthor(authorID);
            if (author == null)
            {
                return new StatusCodeResult(StatusCodes.Status404NotFound);
            }

            libraryRepository.DeleteAuthor(author);

            if (!libraryRepository.Save())
            {
                throw new Exception();
            }
            return NoContent();
        }


    }
}
