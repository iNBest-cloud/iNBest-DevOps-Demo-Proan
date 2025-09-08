var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Despliegue comppleto Ramas Main Escaneo cs");

app.Run();
