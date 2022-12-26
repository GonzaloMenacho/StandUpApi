using System.Net;
using API;
using Microsoft.AspNetCore.HttpOverrides;
using Nest;


var builder = WebApplication.CreateBuilder(args);

// Elasticsearch services

var settings = new ConnectionSettings(); // takes http://localhost:9200 as default uri
// create elasticsearch connection
builder.Services.AddSingleton<IElasticClient>(new ElasticClient(settings));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
