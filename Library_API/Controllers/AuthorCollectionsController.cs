using AutoMapper;
using Library.API.Entities;
using Library.API.Services;
using Library_API.Helpers;
using Library_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Controllers
{
    [Route("api/authorCollections")]
    public class AuthorCollectionsController : Controller
    {
        private ILibraryRepository libraryRepository;

        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
        }

        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {


            // , Name ="ArrayModelBinder"  
            if (ids == null)
            {
                return BadRequest(); //400
            }

            var authors = libraryRepository.GetAuthors(ids);
            if (ids.Count() != authors.Count())
            {
                return NotFound(); //404
            }

            var authorsDto = Mapper.Map<IEnumerable<AuthorDto>>(authors);
            return Ok(authorsDto);
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorCreationDto> authorCollection)
        {
            if (authorCollection == null)
            {
                return BadRequest();
            }

            var authorEntities = Mapper.Map<IEnumerable<Author>>(authorCollection);
            foreach (var author in authorEntities)
            {
                libraryRepository.AddAuthor(author);
            }

            if (!libraryRepository.Save())
            {
                throw new Exception("Error !!!");
            }

            //return Ok(); this will return statuscode 200, without location header, and its adviced to return 201 for created resources eith location header

            var authorsDto = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            string authorsIds = string.Join(',', authorsDto.Select(x => x.Id));
            return CreatedAtRoute("GetAuthorCollection", new { ids = authorsIds }, authorsDto);
        }


    }
}
