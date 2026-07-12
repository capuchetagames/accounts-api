using System.Text.Json.Serialization;
using AccountsApi.Configs;
using AccountsApi.Middlewares;
using AccountsApi.Service;
using AccountsApi.Service.DynamoLogging;
using AccountsApi.Service.Extensions;
using AccountsApi.Service.Validator;
using Amazon.DynamoDBv2;
using Core.Entity;
using Core.Models;
using Core.Repository;
using FluentValidation;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDynamoDb(builder.Configuration);

var serviceProvider = builder.Services.BuildServiceProvider();
var dynamoClient    = serviceProvider.GetRequiredService<IAmazonDynamoDB>();
var logTableName    = builder.Configuration["DynamoDb:LogTableName"];

// Com DynamoDB local, cria a tabela de logs se ainda não existir
if (builder.Configuration.GetValue<bool>("DynamoDb:UseLocal"))
{
    await DynamoDbExtensions.EnsureLogTableExistsAsync(dynamoClient, logTableName);
}


builder.Logging
    .ClearProviders()                      
    .AddConsole()                          
    .AddDynamoDbLogger(dynamoClient, logTableName, LogLevel.Information);


builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();


builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));


builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IAccountRepository, AccountRepository>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICacheService, MemCacheService>();

builder.Services.AddTransient<ICorrelationIdService, CorrelationIdService>();


builder.Services.AddScoped(typeof(IBaseLogger<>), typeof(BaseLogger<>));

builder.AddJwtAuthentication();
builder.Services.AddPolicyAuthorization();

builder.Services.AddHealthChecks();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<IRabbitMqService>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
    
    return RabbitMqService.CreateAsync(settings, logger).GetAwaiter().GetResult();
});




var app = builder.Build();

//app.UseMiddleware<CorrelationMiddleware>();
app.UseLogMiddleware();
app.UseDynamoLogging();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
    
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.UseReDoc(c =>
    {
        c.DocumentTitle = "REDOC API Documentation";
        c.SpecUrl = "/swagger/v1/swagger.json";
    });
}

//app.UseHttpsRedirection();


app.UseHttpMetrics();

app.MapMetrics();

app.MapHealthChecks("/health");

app.UseAuthorization();

app.MapControllers();



Console.WriteLine("Account API Up and Running!");

app.Run();