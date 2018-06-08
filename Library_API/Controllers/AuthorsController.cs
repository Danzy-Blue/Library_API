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
        private ITypeHelperService typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, IPropertyMappingService propertyMappingService, ITypeHelperService typeHelperService)
        {
            this.libraryRepository = libraryRepository;
            this.urlHelper = urlHelper;
            this.propertyMappingService = propertyMappingService;
            this.typeHelperService = typeHelperService;
        }

        [HttpGet("{authorID}", Name = "GetAuthor")]
        public IActionResult GetAuthor([FromRoute]Guid authorID, [FromQuery] string fields)
        {
            if (!libraryRepository.AuthorExists(authorID))
            {
                return NotFound();

            }

            if (!typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = libraryRepository.GetAuthor(authorID);
            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            var links = CreateLinksForAuthor(authorID, fields);
            var linkResource = author.ShapeData(fields) as IDictionary<string, object>;
            linkResource.Add("links", links);
            return Ok(linkResource);
        }


        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters, [FromHeader(Name = "Accept")] string mediaType)
        {

            if (!propertyMappingService.ValidateMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = libraryRepository.GetAuthors(authorsResourceParameters);

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.danzy.hateoas+json")
            {
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));


                var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);
                var shapedAuthors = authors.ShapeData(authorsResourceParameters.Fields);
                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], authorsResourceParameters.Fields);
                    authorAsDictionary.Add("links", authorLinks);
                    return authorAsDictionary;
                });


                var linkedCollectionsResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };

                return Ok(linkedCollectionsResource);
            }
            else
            {
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

                return new JsonResult(authors.ShapeData(authorsResourceParameters.Fields));
            }
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
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
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.Current:
                default:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
        }

        // creating child resorce togather with parent resource
        // i.e. creating Auther and book togather
        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type", new[] { "application/vnd.danzy.author.full+json" })]
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

            var links = CreateLinksForAuthor(authorDto.Id, null);
            var linkedResourceToReturn = authorDto.ShapeData(null) as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new { authorID = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type",
            new[]{ "application/vnd.danzy.authorwithdateofdeath.full+json",
                   "application/vnd.danzy.authorwithdateofdeath.full+xml" })]
        public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorCreationDto author)
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

            var links = CreateLinksForAuthor(authorDto.Id, null);
            var linkedResourceToReturn = authorDto.ShapeData(null) as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new { authorID = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
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

        [HttpDelete("{authorID}", Name = "DeleteAuthor")]
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

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(urlHelper.Link("GetAuthor", new { authorID = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(urlHelper.Link("GetAuthor", new { authorID = id, fields = fields }), "self", "GET"));
            }

            links.Add(new LinkDto(urlHelper.Link("DeleteAuthor", new { authorID = id }), "delete_author", "DELETE"));
            links.Add(new LinkDto(urlHelper.Link("CreateBookForAuthor", new { authorID = id }), "create_book_for_author", "POST"));
            links.Add(new LinkDto(urlHelper.Link("GetBooksForAuthor", new { authorID = id }), "books", "GET"));



            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameter, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();
            links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.Current),
                "self",
                "GET"));

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage),
                "self",
                "GET"));
            }

            if (hasNext)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage),
                "self",
                "GET"));
            }
            return links;
        }
    }
}
