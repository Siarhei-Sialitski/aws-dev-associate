using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.Util;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddSingleton<IImagesRepository, ImagesRepository>();
builder.Services.AddSingleton<IQueueRepository, QueueRepository>();
builder.Services.AddSingleton<ISnsRepository, SnsRepository>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/version", () => "V2")
    .WithName("version")
    .WithOpenApi();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/metainfo", () =>
{
    var currentRegion = EC2InstanceMetadata.Region;
    var availabilityZone = EC2InstanceMetadata.AvailabilityZone;

    return new { currentRegion, availabilityZone };
})
.WithName("GetMetaInfo")
.WithOpenApi();

app.MapPost("/images", async (UploadImagePayload payload, IImagesRepository repository, IQueueRepository queueRepository) =>
{
    try
    {
        await repository.UploadImage(payload.ImageName, payload.Base64Image);

        var metaInfo = await repository.ImageMetainfo(payload.ImageName);
        await queueRepository.SendMessage(metaInfo);

        return Results.Ok("Image uploaded successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/images/{imageName}", async (string imageName, IImagesRepository repository) =>
{
    try
    {
        var base64Image = await repository.DownloadImage(imageName);
        return Results.Ok(base64Image);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapDelete("/images/{imageName}", async (string imageName, IImagesRepository repository) =>
{
    try
    {
        await repository.DeleteImage(imageName);
        return Results.Ok("Image deleted successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/images/metainfo", async (string? imageName, IImagesRepository repository) =>
{
    try
    {
        if (string.IsNullOrEmpty(imageName))
        {
            var allMetaInfo = await repository.ImageMetainfo();
            return Results.Ok(allMetaInfo);
        }
        var metaInfo = await repository.ImageMetainfo(imageName);
        return Results.Ok(metaInfo);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
app.MapHealthChecks("/healthz")
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapPost("/subscribe", async (SubscriptionPayload body, ISnsRepository snsRepository) => {
    await snsRepository.AddSubscription(body.email);
});

app.MapPost("/unsubscribe", async (SubscriptionPayload body, ISnsRepository snsRepository) => {
    await snsRepository.RemoveSubscription(body.email);
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record UploadImagePayload(string ImageName, string Base64Image);

public record SubscriptionPayload(string email);
