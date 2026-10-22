namespace ServiceName.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceName.API.Endpoints.ServiceNames;
using ServiceName.Contracts;
using ServiceName.Contracts.ServiceNames;
using ServiceName.Persistence;

[TestClass]
public sealed class ServiceNameEndpointsTests
{
    private const string Route = "/api/v1/service-names";

    private static HttpClient Writer => TestHost.Factory.CreateClientWithPermissions(ServiceNamePermissions.Read, ServiceNamePermissions.Write);

    [TestMethod]
    public async Task FullLifecycle_Create_Get_Update_Delete()
    {
        using var client = Writer;
        var name = UniqueName();

        var create = await client.PostAsJsonAsync(Route, new CreateServiceNameRequest(name, "first version"));
        Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
        Assert.IsNotNull(create.Headers.Location);
        var id = (await create.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var fetched = await client.GetFromJsonAsync<ServiceNameResponse>($"{Route}/{id}");
        Assert.AreEqual(name, fetched!.Name);
        Assert.AreEqual("Active", fetched.Status);

        var update = await client.PutAsJsonAsync($"{Route}/{id}", new UpdateServiceNameRequest($"{name}-v2", null));
        Assert.AreEqual(HttpStatusCode.NoContent, update.StatusCode);

        // The cached entry must have been invalidated by the update.
        var updated = await client.GetFromJsonAsync<ServiceNameResponse>($"{Route}/{id}");
        Assert.AreEqual($"{name}-v2", updated!.Name);
        Assert.IsNotNull(updated.ModifiedAtUtc);

        var delete = await client.DeleteAsync(new Uri($"{Route}/{id}", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await client.GetAsync(new Uri($"{Route}/{id}", UriKind.Relative));
        Assert.AreEqual(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [TestMethod]
    public async Task List_FiltersAndPaginates()
    {
        using var client = Writer;
        var prefix = UniqueName();
        for (var i = 0; i < 3; i++)
        {
            (await client.PostAsJsonAsync(Route, new CreateServiceNameRequest($"{prefix}-{i}", null))).EnsureSuccessStatusCode();
        }

        var page = await client.GetFromJsonAsync<PagedResponse<ServiceNameResponse>>($"{Route}?search={prefix}&page=1&pageSize=2");

        Assert.AreEqual(3, page!.TotalCount);
        Assert.HasCount(2, page.Items);
        Assert.IsTrue(page.HasNextPage);
    }

    [TestMethod]
    public async Task Create_WithInvalidBody_ReturnsValidationProblem()
    {
        using var client = Writer;

        var response = await client.PostAsJsonAsync(Route, new CreateServiceNameRequest("", null));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.IsTrue(problem!.Errors.ContainsKey("Name"));
        Assert.IsTrue(problem.Extensions.ContainsKey("traceId"));
    }

    [TestMethod]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        using var client = Writer;
        var name = UniqueName();
        (await client.PostAsJsonAsync(Route, new CreateServiceNameRequest(name, null))).EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync(Route, new CreateServiceNameRequest(name, null));

        Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.AreEqual("ServiceName.NameAlreadyExists", ((JsonElement)problem!.Extensions["code"]!).GetString());
    }

    [TestMethod]
    public async Task Create_WithSameIdempotencyKey_ExecutesOnce()
    {
        using var client = Writer;
        var key = Guid.NewGuid().ToString();
        var body = new CreateServiceNameRequest(UniqueName(), null);

        async Task<Guid> SendAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Route) { Content = JsonContent.Create(body) };
            request.Headers.Add("Idempotency-Key", key);
            var response = await client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        Assert.AreEqual(await SendAsync(), await SendAsync());
    }

    [TestMethod]
    public async Task Create_PublishesDomainEvent_ThroughOutbox()
    {
        using var client = Writer;
        var create = await client.PostAsJsonAsync(Route, new CreateServiceNameRequest(UniqueName(), null));
        var id = (await create.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var processed = false;
        for (var attempt = 0; attempt < 50 && !processed; attempt++)
        {
            await Task.Delay(100);
            await using var scope = TestHost.Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceNameDbContext>();
            processed = await db.OutboxMessages.AnyAsync(m => m.Content.Contains(id.ToString()) && m.ProcessedOnUtc != null && m.Error == null);
        }

        Assert.IsTrue(processed, "The outbox message was not processed in time.");
    }

    private static string UniqueName() => $"it-{Guid.NewGuid():N}"[..20];
}
