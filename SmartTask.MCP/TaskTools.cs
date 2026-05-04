using MediatR;
using ModelContextProtocol.Server;
using SmartTask.Application.Features.Tasks.Commands;
using SmartTask.Application.Features.Tasks.Queries;
using System.ComponentModel;

namespace SmartTask.MCP;

/// <summary>
/// Claude'un çağırabileceği MCP tool'ları.
/// Her metot bir tool olarak register edilir.
/// </summary>
[McpServerToolType]
public static class TaskTools
{
    [McpServerTool]
    [Description("Yeni bir görev oluşturur. Öncelik: Low, Medium, High, Critical")]
    public static async Task<string> CreateTask(
        IMediator mediator,
        [Description("Görev başlığı")] string title,
        [Description("Öncelik seviyesi: Low|Medium|High|Critical")] string priority = "Medium",
        [Description("Açıklama (opsiyonel)")] string? description = null,
        [Description("Son tarih ISO 8601 formatında (opsiyonel)")] string? dueDate = null)
    {
        DateTime? due = dueDate is not null ? DateTime.Parse(dueDate).ToUniversalTime() : null;

        var result = await mediator.Send(new CreateTaskCommand(title, description, priority, due));
        return result.Match(
            task => $"✅ Görev oluşturuldu: '{task.Title}' (ID: {task.Id}, Öncelik: {task.Priority})",
            error => $"❌ Hata: {error}");
    }

    [McpServerTool]
    [Description("Görevleri listeler. Filtre: all (varsayılan), pending, today, overdue")]
    public static async Task<string> ListTasks(
        IMediator mediator,
        [Description("Filtre: all | pending | today | overdue")] string filter = "all")
    {
        var result = await mediator.Send(new GetTasksQuery(filter));
        return result.Match(
            tasks =>
            {
                if (!tasks.Any()) return "📭 Bu filtrede görev bulunamadı.";
                var lines = tasks.Select(t =>
                    $"• [{t.Status}] {t.Title}" +
                    (t.DueDate.HasValue ? $" — Son tarih: {t.DueDate:dd/MM/yyyy}" : "") +
                    (t.IsOverdue ? " ⚠️ GECİKMİŞ" : ""));
                return string.Join("\n", lines);
            },
            error => $"❌ Hata: {error}");
    }

    [McpServerTool]
    [Description("Belirtilen görevi tamamlandı olarak işaretler")]
    public static async Task<string> CompleteTask(
        IMediator mediator,
        [Description("Tamamlanacak görevin ID'si (GUID)")] string taskId)
    {
        if (!Guid.TryParse(taskId, out var id))
            return "❌ Geçersiz görev ID formatı.";

        var result = await mediator.Send(new CompleteTaskCommand(id));
        return result.IsSuccess ? "✅ Görev tamamlandı!" : $"❌ Hata: {result.Error}";
    }

    [McpServerTool]
    [Description("Günlük görev özeti — toplam, bekleyen, geciken görev sayıları")]
    public static async Task<string> GetDailySummary(IMediator mediator)
    {
        var result = await mediator.Send(new GetTaskSummaryQuery());
        return result.Match(
            s =>
            {
                var todayList = s.UpcomingToday.Any()
                    ? "\n📅 Bugün:\n" + string.Join("\n", s.UpcomingToday.Select(t => $"  • {t.Title}"))
                    : "\n📅 Bugün için görev yok.";

                return $"""
                📊 Görev Özeti
                ─────────────
                Toplam    : {s.Total}
                Bekleyen  : {s.Pending}
                Devam Eden: {s.InProgress}
                Tamamlanan: {s.Completed}
                Geciken   : {s.Overdue} {(s.Overdue > 0 ? "⚠️" : "✅")}
                {todayList}
                """;
            },
            error => $"❌ Hata: {error}");
    }
}
