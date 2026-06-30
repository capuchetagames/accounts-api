using System.Reflection;
using Microsoft.OpenApi.Models;

namespace UsersApi.Configs;

public static class SwaggerConfiguration
{
    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UsersApi",
                Version = "v1",
                Description = "Api para login de usuários e cadastro de Usuários.",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                {
                    Name = "Diogo Carlomagno / Alan Silva",
                    Email = "diogocarlomagno@gmail.com;alanfs1010@gmail.com",
                    //Url = new Uri("https://google.com/")

                },
            });
            
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });
    }
}