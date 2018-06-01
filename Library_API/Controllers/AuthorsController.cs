using AutoMapper;
using Library.API.Entities;
using Library.API.Services;
using Library_API.Helpers;
using Library_API.Models;
using Library_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        private IUrlHelper urlHelper;
        private IPropertyMappingService propertyMappingService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, IPropertyMappingService propertyMappingService)
        {
            this.libraryRepository = libraryRepository;
            this.urlHelper = urlHelper;
            this.propertyMappingService = propertyMappingService;
        }

        [HttpGet("{authorID}", Name = "GetAuthor")]
        public IActionResult GetAuthor([FromRoute]Guid authorID)
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


        [HttpGet(Name = "GetAuthors")]
        ////public IActionResult GetAuthors([FromQuery(Name = "Page")]int pageNumber = 1, [FromQuery]int pageSize = 10)
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {

            if (!propertyMappingService.ValidateMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }


            var authorsFromRepo = libraryRepository.GetAuthors(authorsResourceParameters);
            var previousPageLink = authorsFromRepo.HasPrevious ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;
            var nextPageLink = authorsFromRepo.HasNext ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;
            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return new JsonResult(authors);

        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });

                case ResourceUriType.NextPage:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                default:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
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
