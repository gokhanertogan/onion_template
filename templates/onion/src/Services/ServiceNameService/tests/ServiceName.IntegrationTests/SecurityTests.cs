namespace ServiceName.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using ServiceName.API.Endpoints.ServiceNames;
using ServiceName.Contracts.ServiceNames;

[TestClass]
public sealed class SecurityTests
{
    [TestMethod]
    public async Task Api_RequiresAuthentication()
    {
        using var client = TestHost.Factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/service-names", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Api_RequiresPermission()
    {
        using var client = TestHost.Factory.CreateClientWithPermissions(ServiceNamePermissions.Read);

        var response = await client.PostAsJsonAsync("/api/v1/service-names", new CreateServiceNameRequest("NoWrite", null));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Responses_CarrySecurityHeaders_AndCorrelationId()
    {
        using var client = TestHost.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.AreEqual("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.AreEqual("test-correlation-123", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.IsFalse(response.Headers.Contains("Server"));
    }

    [TestMethod]
    public async Task Readiness_ChecksDatabase()
    {
        using var client = TestHost.Factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("database", body);
    }
}
