using System;
using System.Text.Json.Serialization; // Required for [JsonPropertyName]

/// <summary>
/// Data Transfer Object mirroring the server's WorldStateDto.
/// Update: Changed to Properties { get; set; } so System.Text.Json can read them.
/// </summary>
[Serializable]
public class WorldStateDto
{
    // The string inside [JsonPropertyName("...")] must match the JSON key sent by the server.
    // ASP.NET Core usually sends properties in camelCase (e.g., "currentWh").

    [JsonPropertyName("currentWh")]
    public long CurrentWh { get; set; }

    [JsonPropertyName("maxWh")]
    public long MaxWh { get; set; }

    [JsonPropertyName("activeEventName")]
    public string ActiveEventName {  get; set;}

    [JsonPropertyName("activeEventDescription")]
    public string ActiveEventDescription {get; set;}

    [JsonPropertyName("isPaused")]
    public bool IsPaused { get; set; }

    [JsonPropertyName("dasySurvived")]
    public int DaysSurvived { get; set; }

    [JsonPropertyName("serverTime")]
    public string ServerTime { get; set; }
}