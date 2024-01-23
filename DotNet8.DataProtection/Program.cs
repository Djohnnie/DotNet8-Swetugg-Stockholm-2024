using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new JsonWriterOptions
{
    Indented = true
});

builder.Logging.EnableRedaction();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRedaction(redaction =>
{
    redaction.SetRedactor<MyCustomErasingRedactor>(MyDataClassificationTaxonomy.PersonalData);
    redaction.SetRedactor<ErasingRedactor>(MyDataClassificationTaxonomy.SensitiveData);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/patients", (Patient patient, ILogger<Program> logger) =>
{
    // TODO: Create patient in the database.

    logger.LogPatientCreated(patient);

    return Results.Created($"/patients/{Guid.NewGuid}", patient);
}).WithOpenApi();

app.Run();


public record Patient([PersonalData] string Name, [PersonalData] string Email, [SensitiveData] string SocialSecurityNumber);

public static class MyDataClassificationTaxonomy
{
    public static string TaxonomyName => typeof(MyDataClassificationTaxonomy).FullName!;

    public static DataClassification SensitiveData => new(TaxonomyName, nameof(SensitiveData));

    public static DataClassification PersonalData => new(TaxonomyName, nameof(PersonalData));
}

public class SensitiveDataAttribute : DataClassificationAttribute
{
    public SensitiveDataAttribute() : base(MyDataClassificationTaxonomy.SensitiveData) { }
}

public class PersonalDataAttribute : DataClassificationAttribute
{
    public PersonalDataAttribute() : base(MyDataClassificationTaxonomy.PersonalData) { }
}


public class MyCustomErasingRedactor : Redactor
{
    private const string ErasedValue = "R*E*D*A*C*T*E*D";

    public override int GetRedactedLength(ReadOnlySpan<char> input)
        => ErasedValue.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        ErasedValue.CopyTo(destination);
        return ErasedValue.Length;
    }
}



internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Patient created")]
    internal static partial void LogPatientCreated(
      this ILogger logger,
      [LogProperties] Patient patient);
}