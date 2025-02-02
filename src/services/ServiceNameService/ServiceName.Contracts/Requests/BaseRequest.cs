using System.Text.Json.Serialization;

namespace ServiceName.Contracts.Requests;

public abstract record BaseRequest
{
    [JsonIgnore]
    public static string Route => "/service-name";
}