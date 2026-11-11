using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Tillad at alle computere og dashboards kan snakke med serveren (CORS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// En trådsikker liste i skyen til at gemme computer-stats midlertidigt
var activeClients = new ConcurrentDictionary<string, ClientData>();

// 1. ENDPOINT: Modtag stats fra klienten
app.MapPost("/api/telemetry", (ClientData data) =>
{
    if (data != null && !string.IsNullOrEmpty(data.SessionId))
    {
        data.LastSeen = DateTime.Now;
        data.Status = "🟢 Online";
        activeClients.AddOrUpdate(data.SessionId, data, (key, old) => data);
    }
    return Results.Ok(new { status = "success" });
});

// 2. ENDPOINT: Hent alle stats ned til dit HatLacker Dashboard
app.MapGet("/api/telemetry/all", () =>
{
    // Tjek om nogle computere er gået offline (mere end 15 sekunder)
    foreach (var kvp in activeClients)
    {
        if ((DateTime.Now - kvp.Value.LastSeen).TotalSeconds > 15)
        {
            kvp.Value.Status = "🔴 Offline";
            kvp.Value.Ram = "0 MB";
        }

        // Slet helt hvis de har været offline i over 2 minutter
        if ((DateTime.Now - kvp.Value.LastSeen).TotalMinutes > 2)
        {
            activeClients.TryRemove(kvp.Key, out _);
        }
    }
    return Results.Ok(activeClients.Values);
});

app.Run();

// Datamodellen der skal matche din WinForms-app fuldstændig
public class ClientData
{
    public string SessionId { get; set; } = "";
    public string ComputerName { get; set; } = "";
    public string OS { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string Ram { get; set; } = "";
    public string Status { get; set; } = "🟢 Online";
    public DateTime LastSeen { get; set; } = DateTime.Now;
}
