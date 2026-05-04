# 🧠 SmartTask API

> AI-destekli görev yönetim sistemi — **ASP.NET Core 9** + **Clean Architecture** + **MCP (Model Context Protocol)**

Claude gibi bir AI asistanı ile doğal dil üzerinden görevlerinizi yönetin.

---

## 🏗️ Mimari

```
SmartTask/
├── SmartTask.Domain/          → Entity'ler, Domain kuralları, Result pattern
├── SmartTask.Application/     → CQRS (MediatR), FluentValidation, Use case'ler
├── SmartTask.Infrastructure/  → EF Core, Repository, JWT, BCrypt
├── SmartTask.API/             → ASP.NET Core Web API, Controller'lar, Middleware
└── SmartTask.MCP/             → MCP Server — Claude ile entegrasyon
```

**Clean Architecture** prensiplerine göre her katman yalnızca bir alta bağımlıdır. Domain katmanının hiçbir dış bağımlılığı yoktur.

---

## ⚙️ Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Web Framework | ASP.NET Core 9 |
| ORM | Entity Framework Core 9 (SQLite) |
| CQRS | MediatR 12 |
| Validation | FluentValidation |
| Authentication | JWT Bearer |
| Password Hashing | BCrypt |
| Logging | Serilog |
| API Docs | Scalar (OpenAPI) |
| AI Entegrasyon | Model Context Protocol (MCP) |

---

## 🚀 Hızlı Başlangıç

### Docker ile (önerilen)
```bash
git clone https://github.com/kullanici/smarttask-api
cd smarttask-api
docker compose up --build
```
API: http://localhost:5000/scalar

### Manuel
```bash
cd SmartTask.API
dotnet run
```

---

## 📡 API Endpoints

### Auth
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/v1/auth/register` | Kayıt ol |
| POST | `/api/v1/auth/login` | Giriş yap, token al |

### Tasks (JWT gerekli)
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/v1/tasks` | Tüm görevleri listele |
| GET | `/api/v1/tasks?filter=overdue` | Geciken görevler |
| GET | `/api/v1/tasks?filter=today` | Bugünkü görevler |
| GET | `/api/v1/tasks/summary` | Özet istatistik |
| POST | `/api/v1/tasks` | Yeni görev oluştur |
| PATCH | `/api/v1/tasks/{id}/complete` | Görevi tamamla |
| DELETE | `/api/v1/tasks/{id}` | Görevi sil (soft-delete) |

---

## 🤖 MCP Entegrasyonu

Claude Desktop ile doğal dil üzerinden görev yönetimi:

```json
// claude_desktop_config.json
{
  "mcpServers": {
    "smarttask": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/SmartTask.MCP"]
    }
  }
}
```

### Kullanım Örnekleri
```
"Yarın saat 17'ye kadar proje sunumu hazırla, yüksek öncelikli"
"Bugünkü görevlerimi göster"
"Geciken görevlerim var mı?"
"Günlük özet ver"
```

### MCP Tool'ları
| Tool | Açıklama |
|------|----------|
| `create_task` | Yeni görev oluşturur |
| `list_tasks` | Filtreye göre görevleri listeler |
| `complete_task` | Görevi tamamlandı işaretler |
| `get_daily_summary` | Günlük istatistik özeti |

---

## 🔑 Tasarım Kararları

- **Generic Repository + Unit of Work**: Her entity için ayrı repo yerine `IRepository<T>` generic yapısı
- **Result Pattern**: Exception fırlatmak yerine `Result<T>` ile kontrollü hata yönetimi
- **Domain Driven**: Entity state'i yalnızca domain metotları ile değişir (private setter)
- **MediatR Pipeline**: Validation tüm command'lara otomatik uygulanır, controller'da tek satır validation yok
- **Soft Delete + Global Query Filter**: Silinen kayıtlar sorgudan otomatik hariç tutulur
- **Global Exception Handler**: `IExceptionHandler` middleware ile merkezi hata yönetimi

---

## 📁 Proje Yapısı Detayı

```
SmartTask.Domain/
├── Common/
│   ├── BaseEntity.cs       ← Generic base (BaseEntity<TId>)
│   └── Result.cs           ← Result<T> pattern
├── Entities/
│   ├── TaskItem.cs         ← Domain entity, private setter'lar
│   └── User.cs
├── Enums/
│   └── Enums.cs            ← TaskStatus, Priority
└── Exceptions/
    └── DomainExceptions.cs

SmartTask.Application/
├── Common/
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs  ← MediatR pipeline
│   ├── Interfaces/
│   │   ├── IRepository.cs         ← Generic repo + UoW
│   │   └── IAuthInterfaces.cs
│   └── Exceptions/
├── Features/
│   ├── Tasks/
│   │   ├── Commands/TaskCommands.cs
│   │   ├── Queries/TaskQueries.cs
│   │   └── TaskMappingExtensions.cs
│   └── Auth/Commands/AuthCommands.cs
└── DTOs/

SmartTask.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── UnitOfWork.cs
│   ├── Repositories/GenericRepository.cs
│   └── Configurations/
└── Services/
    └── AuthServices.cs
```
