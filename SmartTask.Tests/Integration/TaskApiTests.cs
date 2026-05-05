using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTask.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SmartTask.Tests.Integration;

/// <summary>
/// Gerçek HTTP istekleri gönderir, in-memory veritabanı kullanır.
/// Register → Login → Task oluştur → Tamamla akışını uçtan uca test eder.
/// </summary>
public class TaskApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TaskApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Gerçek SQLite yerine in-memory DB kullan
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        }).CreateClient();
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private async Task<string> RegisterAndLoginAsync(
        string username = "testuser",
        string email = "test@example.com",
        string password = "password123")
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username,
            email,
            password
        });

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        var body = await loginResp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private void SetAuth(string token)
        => _client.DefaultRequestHeaders.Authorization =
           new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // ── TESTS ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "newuser",
            email = "new@example.com",
            password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturn400()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "user1",
            email = "duplicate@example.com",
            password = "password123"
        });

        // Aynı email ile tekrar register
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "user2",
            email = "duplicate@example.com",
            password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTasks_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/tasks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTask_WithValidData_ShouldReturn201()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("creator", "creator@example.com");
        SetAuth(token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Integration test görevi",
            priority = "High",
            dueDate = DateTime.UtcNow.AddDays(5)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Integration test görevi");
        doc.RootElement.GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task CreateTask_WithInvalidPriority_ShouldReturn400()
    {
        var token = await RegisterAndLoginAsync("validator", "validator@example.com");
        SetAuth(token);

        var response = await _client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Görev",
            priority = "GECERSIZ_ONCELIK"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteTask_EndToEnd_ShouldSucceed()
    {
        // Arrange — register, login, task oluştur
        var token = await RegisterAndLoginAsync("completer", "completer@example.com");
        SetAuth(token);

        var createResp = await _client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Tamamlanacak görev",
            priority = "Medium"
        });

        var createBody = await createResp.Content.ReadAsStringAsync();
        var taskId = JsonDocument.Parse(createBody).RootElement
                                 .GetProperty("id").GetString();

        // Act — görevi tamamla
        var completeResp = await _client.PatchAsync(
            $"/api/v1/tasks/{taskId}/complete", null);

        // Assert
        completeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Listeye bak — status Completed olmalı
        var listResp = await _client.GetAsync("/api/v1/tasks");
        var listBody = await listResp.Content.ReadAsStringAsync();
        var tasks = JsonDocument.Parse(listBody).RootElement;

        var completedTask = tasks.EnumerateArray()
            .FirstOrDefault(t => t.GetProperty("id").GetString() == taskId);

        completedTask.GetProperty("status").GetString().Should().Be("Completed");
    }

    [Fact]
    public async Task GetSummary_ShouldReturnCorrectCounts()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("summary", "summary@example.com");
        SetAuth(token);

        // 2 task oluştur
        await _client.PostAsJsonAsync("/api/v1/tasks", new { title = "Görev 1", priority = "Low" });
        await _client.PostAsJsonAsync("/api/v1/tasks", new { title = "Görev 2", priority = "High" });

        // Act
        var response = await _client.GetAsync("/api/v1/tasks/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("pending").GetInt32().Should().Be(2);
    }
}