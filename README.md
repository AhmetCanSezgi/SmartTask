# 🧠 SmartTask API

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=flat)
![MCP](https://img.shields.io/badge/MCP-Enabled-00C853?style=flat)
![Architecture](https://img.shields.io/badge/Architecture-Clean-blue?style=flat)
![License](https://img.shields.io/badge/License-MIT-green?style=flat)

> AI-destekli görev yönetim sistemi — **ASP.NET Core 9** + **Clean Architecture** + **MCP (Model Context Protocol)**

Claude gibi bir AI asistanına bağlanarak doğal dil ile görev yönetimi yapabilen backend sistemi.  
"Yarın saat 17'ye kadar sunum hazırla, yüksek öncelikli" → Claude bunu anlayıp otomatik görev oluşturur.

---

## 🏗️ Mimari

```
SmartTask/
├── SmartTask.Domain/          → Entity'ler, Domain kuralları, Result pattern
├── SmartTask.Application/     → CQRS (MediatR), FluentValidation, Use case'ler
├── SmartTask.Infrastructure/  → EF Core, Generic Repository, JWT, BCrypt
├── SmartTask.API/             → ASP.NET Core Web API, Controllers, Middleware
└── SmartTask.MCP/             → MCP Server — Claude ile AI entegrasyonu
```

Her katman yalnızca bir alt katmana bağımlıdır. **Domain katmanının hiçbir dış bağımlılığı yoktur** — ne EF Core, ne HTTP, ne NuGet paketi.

---

## ⚙️ Teknoloji Yığını

| Katman | Teknoloji | Amaç |
|--------|-----------|-------|
| Web Framework | ASP.NET Core 9 | REST API |
| ORM | Entity Framework Core 9 | Veritabanı erişimi |
| Veritabanı | SQLite | Geliştirme (PostgreSQL'e geçiş kolay) |
| CQRS | MediatR 12 | Command/Query ayrımı |
| Validation | FluentValidation | Pipeline üzerinden otomatik validasyon |
| Authentication | JWT Bearer | Token tabanlı kimlik doğrulama |
| Password Hashing | BCrypt | Güvenli şifre saklama |
| Logging | Serilog | Structured logging |
| API Docs | Scalar (OpenAPI) | Modern API dokümantasyonu |
| AI Entegrasyon | Model Context Protocol | Claude ile doğal dil entegrasyonu |

---

## 🔑 Öne Çıkan Tasarım Kararları

### Generic Repository + Unit of Work
Her entity için ayrı repository sınıfı yazmak yerine `IRepository<T>` ile tek implementasyon:
```csharp
_uow.Repository<TaskItem>().FindAsync(t => t.UserId == userId);
_uow.Repository<User>().ExistsAsync(u => u.Email == email);
```

### Result Pattern
Exception fırlatmak yerine kontrollü hata yönetimi:
```csharp
return Result<TaskDto>.Success(task.ToDto());
return Result<TaskDto>.Failure("Email already in use.");

// Controller'da:
return result.Match<IActionResult>(
    task => CreatedAtAction(...),
    error => BadRequest(new { error }));
```

### Domain Entity — Private Setter
State yalnızca domain metotları ile değişir:
```csharp
public void Complete()
{
    if (Status == TaskStatus.Completed)
        throw new DomainException("Task is already completed.");
    Status = TaskStatus.Completed;
}
```

### MediatR Pipeline — Otomatik Validation
Her command otomatik olarak FluentValidation'dan geçer, controller'da tek satır validation kodu yok:
```
Request → ValidationBehavior → Handler
```

### Soft Delete + Global Query Filter
`HasQueryFilter(t => !t.IsDeleted)` ile silinen kayıtlar tüm sorgulardan otomatik hariç tutulur.

### Global Exception Handler
`IExceptionHandler` middleware ile tüm exception'lar tek yerden yönetilir, controller'larda try/catch yok.

---

## 🤖 MCP Entegrasyonu

Bu proje iki farklı arayüze sahiptir:

| | REST API | MCP Server |
|---|---|---|
| Kim kullanır? | Frontend, Postman, mobil | Claude gibi AI asistanlar |
| Nasıl iletişim? | HTTP + JSON | stdio protokolü |
| Input | JSON body | Doğal dil parametreleri |

**İkisi de aynı Application katmanını kullanır — business logic tekrarı sıfır.**

### Claude Desktop Kurulumu

`claude_desktop_config.json` dosyasına ekle:
```json
{
  "mcpServers": {
    "smarttask": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/SmartTask.MCP"]
    }
  }
}
```

### MCP Tool'ları

| Tool | Açıklama |
|------|----------|
| `create_task` | Yeni görev oluşturur |
| `list_tasks` | Filtreye göre görevleri listeler (all/pending/today/overdue) |
| `complete_task` | Görevi tamamlandı işaretler |
| `get_daily_summary` | Toplam/bekleyen/geciken görev özeti |

---

## 🚀 Hızlı Başlangıç

### Gereksinimler
- .NET 9 SDK
- dotnet-ef tool: `dotnet tool install --global dotnet-ef`

### Kurulum

```bash
git clone https://github.com/AhmetCanSezgi/SmartTask.git
cd SmartTask

dotnet restore SmartTask.API/SmartTask.API.csproj
dotnet restore SmartTask.Infrastructure/SmartTask.Infrastructure.csproj
dotnet restore SmartTask.Application/SmartTask.Application.csproj

cd SmartTask.API
dotnet ef migrations add InitialCreate --project ../SmartTask.Infrastructure
dotnet ef database update --project ../SmartTask.Infrastructure
dotnet run
```

API dokümantasyonu: `http://localhost:5000/scalar`

### Docker ile

```bash
docker compose up --build
```

---

## 📡 API Endpoints

### Auth
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/v1/auth/register` | Kayıt ol |
| POST | `/api/v1/auth/login` | Giriş yap, JWT token al |

### Tasks `[JWT gerekli]`
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/v1/tasks` | Tüm görevleri listele |
| GET | `/api/v1/tasks?filter=overdue` | Geciken görevler |
| GET | `/api/v1/tasks?filter=today` | Bugünkü görevler |
| GET | `/api/v1/tasks/summary` | Özet istatistik |
| POST | `/api/v1/tasks` | Yeni görev oluştur |
| PATCH | `/api/v1/tasks/{id}/complete` | Görevi tamamla |
| DELETE | `/api/v1/tasks/{id}` | Soft-delete |

---

## 📁 Proje Yapısı

```
SmartTask.Domain/
├── Common/
│   ├── BaseEntity.cs          ← Generic base (BaseEntity<TId>)
│   └── Result.cs              ← Result<T> pattern
├── Entities/
│   ├── TaskItem.cs            ← Domain entity, private setter'lar
│   └── User.cs
├── Enums/Enums.cs             ← TaskStatus, Priority
└── Exceptions/DomainExceptions.cs

SmartTask.Application/
├── Common/
│   ├── Behaviors/ValidationBehavior.cs   ← MediatR pipeline
│   ├── Interfaces/IRepository.cs         ← Generic repo + UoW interface
│   └── Exceptions/
├── Features/
│   ├── Tasks/Commands/TaskCommands.cs    ← Create, Complete, Delete
│   ├── Tasks/Queries/TaskQueries.cs      ← GetTasks, GetSummary
│   └── Auth/Commands/AuthCommands.cs     ← Register, Login
└── DTOs/

SmartTask.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── UnitOfWork.cs
│   ├── Repositories/GenericRepository.cs ← Tek implementasyon, tüm entity'ler
│   └── Configurations/
└── Services/AuthServices.cs              ← JWT + BCrypt

SmartTask.API/
├── Controllers/               ← Tasks, Auth
├── Middleware/GlobalExceptionHandler.cs
└── Services/CurrentUserService.cs

SmartTask.MCP/
├── TaskTools.cs               ← Claude'un çağırdığı tool'lar
└── Program.cs
```

---

## 📄 Lisans

MIT
