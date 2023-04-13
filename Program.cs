using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Resistor Values", Version = "v1" });
    // path to swagger-introduction in ./bin/Debug/net7.0/swagger-introduction.xml

    // xml is produced with dotnet build => patches documentation together
    var locationOfExecutable = Assembly.GetExecutingAssembly().Location;
    var execFilenamewithoutExtension = Path.GetFileNameWithoutExtension(locationOfExecutable);
    var execFilePath = Path.GetDirectoryName(locationOfExecutable);
    var xmlFilePath = Path.Combine(execFilePath!, $"{execFilenamewithoutExtension}.xml");
    options.IncludeXmlComments(xmlFilePath);
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();


var colorDetailDict = new ConcurrentDictionary<int, ColorDetails>
{
    [0] = new("Black", 0, 1),
    [1] = new("Brown", 1, 10, 1),
    [2] = new("Red", 2, 100, 2),
    [3] = new("Orange", 3, 1000),
    [4] = new("Yellow", 4, 10_000),
    [5] = new("Green", 5, 100_000, 0.5),
    [6] = new("Blue", 6, 1_000_000, 0.25),
    [7] = new("Violet", 7, 10_000_000, 0.10),
    [8] = new("Grey", 8, 100_000_000, 0.05),
    [9] = new("White", 9, 1_000_000_000),
    [10] = new("Gold", 0, 0.1, 5),
    [11] = new("Silver", 0, 0.01, 10)
};

app.MapGet("/colors", () =>
{
    string[] colors = new string[colorDetailDict.Count];
    for (int i = 0; i < colorDetailDict.Count; i++)
    {
        colors[i] = colorDetailDict[i].Name;
    }
    return Results.Ok(colors);
});

app.MapGet("/colors/{color}", (string color) =>
{
    ColorDetails resultColor = new ColorDetails(color, 0, 0, 0);

    foreach (ColorDetails details in colorDetailDict.Values)
    {
        if (details.Name.ToLower() == color.ToLower())
        {
            resultColor = details;
        }
    }
    return Results.Ok(resultColor);
});

app.MapPost("/resistors/value-from-bands", (resistorBands bands) =>
{
    return Results.Ok(FindResistorValue(bands));
});

app.MapGet("/resistors/value-from-bands", ([FromQuery]string FirstBand, [FromQuery]string SecondBand, [FromQuery]string? ThirdBand, [FromQuery]string Multiplier, [FromQuery]string Tolerance) =>
{
    resistorBands bands;
    if (ThirdBand == null || ThirdBand == "")
    {
        bands = new resistorBands(FirstBand, SecondBand, Multiplier, Tolerance);
    }
    else
    {
        bands = new resistorBands(FirstBand, SecondBand, Multiplier, Tolerance, ThirdBand);
    }
    return Results.Ok(FindResistorValue(bands));
});

app.Run();

resistorValue FindResistorValue(resistorBands bands)
{
    resistorValue values;
    string bandColorConcat = "";
    int bandColorAsInt = 0;
    double bandValue = 0;
    double toleranceValue = 0;

    if (bands.ThirdBand == "" || bands.ThirdBand == null)
    {
        for (int j = 0; j < colorDetailDict.Count; j++)
        {
            if (bands.FirstBand.ToLower() == colorDetailDict[j].Name.ToLower() || bands.SecondBand.ToLower() == colorDetailDict[j].Name.ToLower())
            {
                bandColorConcat += colorDetailDict[j].Value;
            }
        }
        bandColorConcat += "0";

    }
    else
    {
        for (int i = 0; i < colorDetailDict.Count; i++)
        {
            if (bands.FirstBand.ToLower() == colorDetailDict[i].Name.ToLower() || bands.SecondBand.ToLower() == colorDetailDict[i].Name.ToLower() || bands.ThirdBand.ToLower() == colorDetailDict[i].Name.ToLower())
            {
                bandColorConcat += colorDetailDict[i].Value;
            }
        }
    }

    if (bandColorConcat != null || bandColorConcat != "")
    {
        bandColorAsInt = Convert.ToInt32(bandColorConcat);
    }

    for (int i = 0; i < colorDetailDict.Count; i++)
    {
        if (bands.Multiplier == colorDetailDict[i].Name)
        {
            bandValue = bandColorAsInt * colorDetailDict[i].Multiplier;
        }

        if (bands.Tolerance == colorDetailDict[i].Name)
        {
            toleranceValue = colorDetailDict[i].Tolerance;
        }
    }
    values = new resistorValue(bandValue, toleranceValue);
    return values;
}

/// <summary>
/// color details containing Name, value, Multiplier and a nullable tolerance
/// </summary>
record ColorDetails(string Name, int Value, double Multiplier, double Tolerance = 0);

/// <summary>
/// information regarding the resistor, color of bands
/// </summary>
record resistorBands(string FirstBand, string SecondBand, string Multiplier, string Tolerance, string ThirdBand = "");

/// <summary>
/// calculated value of given resistor
/// </summary>
record resistorValue(double ResistorValue, double Tolerance);