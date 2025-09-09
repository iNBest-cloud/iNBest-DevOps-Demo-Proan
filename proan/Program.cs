var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Despliegue completo Ramas Main, sonar Actualizado");

app.Run();
