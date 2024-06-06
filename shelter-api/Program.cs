using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Shelter.Services;
using Shelter.Services.KdfService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true)
                .AllowCredentials();
        });
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
}
else
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.json");
}

builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var azureSection = configuration.GetSection("Azure");
    var cosmosDbSection = azureSection.GetSection("CosmosDB");
    var endpointUri = cosmosDbSection["EndpointUri"];
    var primaryKey = cosmosDbSection["PrimaryKey"];
    return new CosmosClient(endpointUri, primaryKey);
});

builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddSingleton<IKdfService, KdfService>();
builder.Services.AddSingleton<IHashService, HashService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Apply CORS policy to all routes
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.Run();