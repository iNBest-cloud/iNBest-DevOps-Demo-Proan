// Program.cs
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ❌ CORS demasiado permisivo
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
));

var app = builder.Build();

app.MapGet("/", () =>
{
    return Results.Content(
        "<h1>Hola, Esta es una prueba de vulnerabilidades<br/>Pruebas DevOps</h1>",
        "text/html"
    );
});


app.UseCors();

// ❌ Secret hardcodeado en código fuente
const string ApiKey = "super-secreto-12345";

// ❌ Deshabilitar HTTPS redirection (mejor dejarlo activado en apps reales)
// app.UseHttpsRedirection();


// ❌ Exposición de información sensible en logs + credenciales hardcodeadas
app.MapGet("/login", (HttpContext ctx) =>
{
    var u = ctx.Request.Query["user"].ToString();
    var p = ctx.Request.Query["pass"].ToString();

    // Log de datos sensibles (¡muy mal!)
    app.Logger.LogInformation("Intento login user={User} pass={Pass}", u, p);

    // Comparación insegura y credenciales débiles
    if (u == "admin" && p == "admin123")
        return Results.Ok("Bienvenido admin (mala práctica).");

    return Results.Unauthorized();
});

// ❌ SQL Injection (concatenación directa)
app.MapGet("/user", async (HttpContext ctx) =>
{
    var id = ctx.Request.Query["id"].ToString(); // p.ej: 1 OR 1=1--
    var cs = "Server=localhost;Database=Demo;User Id=sa;Password=Your_password123;"; // hardcoded

    using var cn = new SqlConnection(cs);
    await cn.OpenAsync();

    var sql = $"SELECT * FROM Users WHERE Id = {id}"; // ¡Inyección!
    using var cmd = new SqlCommand(sql, cn);
    using var rd = await cmd.ExecuteReaderAsync();

    var rows = new List<Dictionary<string, object>>();
    while (await rd.ReadAsync())
    {
        var row = new Dictionary<string, object>();
        for (int i = 0; i < rd.FieldCount; i++)
            row[rd.GetName(i)] = rd.GetValue(i);
        rows.Add(row);
    }

    return Results.Ok(rows);
});


// ❌ Criptografía débil (MD5) + SHA1
app.MapGet("/hash", (HttpContext ctx) =>
{
    var text = ctx.Request.Query["text"].ToString();

    using var md5 = MD5.Create(); // deprecated/weak
    var md5Hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));

    using var sha1 = SHA1.Create(); // weak
    var sha1Hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(text)));

    return Results.Ok(new { md5Hash, sha1Hash });
});

// ❌ Generación insegura de tokens (System.Random)
app.MapGet("/token-inseguro", () =>
{
    var rnd = new Random();
    var bytes = new byte[16];
    rnd.NextBytes(bytes); // predecible
    return Results.Ok(Convert.ToHexString(bytes));
});

// ❌ Validación SSL deshabilitada + SSRF/Ghost requests potencial
app.MapGet("/descargar", async (HttpContext ctx) =>
{
    var url = ctx.Request.Query["url"].ToString(); // p.ej. http://127.0.0.1:2375/...
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true // acepta todo
    };
    using var http = new HttpClient(handler);
    var resp = await http.GetStringAsync(url); // posible SSRF
    return Results.Text(resp, "text/plain");
});

// ❌ Path Traversal (lectura de archivos arbitrarios)
app.MapGet("/archivo", (HttpContext ctx) =>
{
    var path = ctx.Request.Query["path"].ToString(); // p.ej. ../../../../Windows/System32/drivers/etc/hosts
    if (!System.IO.File.Exists(path)) return Results.NotFound("No existe");
    var content = System.IO.File.ReadAllText(path); // sin saneamiento
    return Results.Text(content, "text/plain");
});

// ❌ Open Redirect
app.MapGet("/redirigir", (HttpContext ctx) =>
{
    var target = ctx.Request.Query["url"].ToString(); // p.ej. https://phishing.example
    return Results.Redirect(target); // sin validación de dominio
});

// ❌ Deserialización insegura (BinaryFormatter)
#pragma warning disable SYSLIB0011
app.MapPost("/deserialize", async (HttpContext ctx) =>
{
    using var ms = new MemoryStream();
    await ctx.Request.Body.CopyToAsync(ms);
    ms.Position = 0;
    var bf = new BinaryFormatter(); // inseguro
    var obj = bf.Deserialize(ms);   // RCE en escenarios hostiles
    return Results.Ok(new { deserializedType = obj?.GetType().FullName });
});
#pragma warning restore SYSLIB0011

// ❌ Ejecución de comandos del SO (Command Injection) — DEMO SOLO LOCAL
app.MapGet("/exec", (HttpContext ctx) =>
{
    var cmd = ctx.Request.Query["cmd"].ToString(); // p.ej. "whoami" o "dir & calc"
    var psi = new ProcessStartInfo
    {
        FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
        Arguments = OperatingSystem.IsWindows() ? $"/c {cmd}" : $"-c \"{cmd}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var p = Process.Start(psi);
    var output = p!.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
    p.WaitForExit();
    return Results.Text(output, "text/plain");
});

app.Run();
