using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Services;
using Library_API.Helpers;
using Library_API.Models;
using Library_API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace Library_API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                //setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

                var xmlDataContractSerializerInputFormatter = new XmlDataContractSerializerInputFormatter();
                xmlDataContractSerializerInputFormatter.SupportedMediaTypes.Add("application/vnd.danzy.authorwithdateofdeath.full+xml");
                setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);

                var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
                if (jsonOutputFormatter != null)
                {
                    jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.danzy.hateoas+json");
                }


                var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();
                if (jsonInputFormatter != null)
                {
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.danzy.author.full+json");
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.danzy.authorwithdateofdeath.full+json");
                }

            })
            .AddJsonOptions(setupAction =>
            {
                setupAction.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IUrlHelper, UrlHelper>(implementationFactory =>
            {
                var actionContext =
                implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                return new UrlHelper(actionContext);
            });

            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<ITypeHelperService, TypeHelperService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
        ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug(LogLevel.Information);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
               {
                   appBuilder.Run(async context =>
                   {
                       var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                       if (exceptionHandlerFeature != null)
                       {
                           var logger = loggerFactory.CreateLogger("GlobalException");
                           logger.LogError(500, exceptionHandlerFeature.Error, exceptionHandlerFeature.Error.Message);

                       }

                       context.Response.StatusCode = 500;
                       await context.Response.WriteAsync("error- DANZY");
                   });
               });
            }

            ////app.Run(async (context) =>
            ////{
            ////    await context.Response.WriteAsync("Hello World!");
            ////});

            AutoMapper.Mapper.Initialize(con =>
            {
                con.CreateMap<Author, AuthorDto>()
                  .ForMember("Name", dest => dest.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                  .ForMember(dest => dest.Age, option => option.MapFrom(src => src.DateOfBirth.GetCurrentAge(src.DateOfDeath)));

                con.CreateMap<Book, BookDto>();

                con.CreateMap<AuthorCreationDto, Author>();
                con.CreateMap<AuthorCreationWithDateOfDeathDto, Author>();

                con.CreateMap<BookCreationDto, Book>();
                con.CreateMap<BookUpdationDto, Book>();
                con.CreateMap<Book, BookUpdationDto>();
            });


            libraryContext.EnsureSeedDataForContext();

            app.UseMvc();
        }
    }
}
