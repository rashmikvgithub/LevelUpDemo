# Level Up My Life

A personal growth and productivity app. You can:

- Grade your **daily activities** (0–5).
- Track **long-term goals** by logging progress checkpoints.
- See **dashboard trends**.
- Press **Help Me!** to get an idea for doing better.

| Layer    | Technology |
|----------|------------|
| API      | ASP.NET Core 9 Web API, Swagger (Swashbuckle), FluentValidation |
| Data     | Entity Framework Core 9, SQL Server (LocalDB by default) |
| Frontend | Angular (standalone, zoneless, signals), Angular Material, Chart.js |
| Tests    | xUnit, Moq, EF Core InMemory |

## Architecture

Clean Architecture. Dependencies point inward:

```
LevelUp.Api ──► LevelUp.Application ──► LevelUp.Domain
     │                  ▲
     └──► LevelUp.Infrastructure (EF Core, repositories, audit interceptor, clock, current user)
```

- **Domain**:
  - Entities: `Activity`, `ActivityScore`, `Goal`, `GoalCheckpoint`, `Recommendation`, `RecommendationHistory`.
  - Enums.
  - An `AuditableEntity` base class with `CreatedBy`, `CreatedDate`, `ModifiedBy` and `ModifiedDate`.
- **Application**:
  - DTOs, service layer, FluentValidation validators, repository interfaces and the Unit of Work.
  - Domain exceptions.
  - `IRecommendationProvider`, with the default `RandomRecommendationProvider`.
- **Infrastructure**:
  - `LevelUpDbContext`, Fluent API configurations, migrations and seed data.
  - Repositories.
  - `AuditableEntityInterceptor`, which fills in the audit fields automatically.
- **Api**:
  - Thin controllers.
  - Global exception handling (`IExceptionHandler` → RFC 7807 ProblemDetails).
  - Swagger, CORS, health check.

All I/O is async and accepts a `CancellationToken`.

## Folder structure

```
LevelUp/
├── backend/
│   ├── LevelUp.sln, global.json
│   ├── database/LevelUp_Schema.sql        # Idempotent SQL Server schema + seed script
│   ├── src/
│   │   ├── LevelUp.Domain/                # Common, Entities, Enums
│   │   ├── LevelUp.Application/           # Activities, Goals, Dashboard, Recommendations, Common
│   │   ├── LevelUp.Infrastructure/        # Persistence (DbContext, Configurations, Migrations, Repositories), Services
│   │   └── LevelUp.Api/                   # Controllers, Middleware, Services, Program.cs
│   └── tests/LevelUp.Application.Tests/   # Service-level unit tests
└── frontend/
    ├── proxy.conf.json                    # /api -> http://localhost:5042
    └── src/app/
        ├── core/        # models, API services, HTTP error interceptor, utils
        ├── shared/      # reusable components + dialogs (grade activity, goal checkpoint, confirm)
        ├── features/    # dashboard, activities, goals, recommendations (lazy-loaded pages)
        └── app.*        # shell (responsive sidenav), routes, config
```

## Prerequisites

- .NET 9 SDK
- Node.js 20+ and npm
- SQL Server or SQL Server LocalDB. The default connection string is `Server=(localdb)\MSSQLLocalDB;Database=LevelUpMyLife;...` in `backend/src/LevelUp.Api/appsettings.json`.
- An OpenAI API key for AI recommendations.

## Running locally

**API.** In the Development environment, the database is created and migrated, and seed data is added automatically.

```powershell
cd backend
dotnet run --project src/LevelUp.Api --launch-profile http
# Swagger UI: http://localhost:5042/swagger
```

**Frontend**

```powershell
cd frontend
npm install
npx ng serve            # http://localhost:4200 (use --port to change); /api is proxied to the API
```

To enable AI recommendations, set the key as a backend environment variable before starting the API. Never put the key in Angular code or commit it to a configuration file:

```powershell
$env:OpenAI__ApiKey = "your-api-key"
dotnet run --project src/LevelUp.Api --launch-profile http
```

The optional model setting is `OpenAI:Model` in `appsettings.json` and defaults to `gpt-4o-mini`.

**Tests / build**

```powershell
cd backend;  dotnet test
cd frontend; npx ng build
```

**Database without EF.** To create the schema directly, run `backend/database/LevelUp_Schema.sql` against SQL Server. To regenerate the schema from migrations instead:
`dotnet ef migrations script --idempotent -p src/LevelUp.Infrastructure -s src/LevelUp.Api`.

## API overview

| Area | Endpoints |
|------|-----------|
| Activities | `GET /api/activities?search=&status=&addedFrom=&addedTo=&retiredFrom=&retiredTo=`, `GET/PUT/DELETE /api/activities/{id}`, `POST /api/activities` |
| Daily grades | `GET /api/activities/daily?date=`, `GET /api/activities/{id}/scores`, `PUT /api/activities/{id}/scores` (upsert per date), `DELETE /api/activities/{id}/scores/{scoreId}` |
| Goals | `GET/POST /api/goals`, `GET/PUT/DELETE /api/goals/{id}`, `POST /api/goals/{id}/complete` |
| Checkpoints | `GET/POST /api/goals/{id}/checkpoints`, `DELETE /api/goals/{id}/checkpoints/{checkpointId}` |
| Dashboard | `GET /api/dashboard/summary?date=`, `GET /api/dashboard/trends?period=Daily\|Weekly\|Monthly\|Annual` |
| Recommendations | CRUD on `/api/recommendations`, `POST /api/recommendations/suggest`, `GET /api/recommendations/history?activityId=&goalId=&take=` |

Errors are returned as ProblemDetails with these status codes:

- 400: validation error, including per-field errors.
- 404: not found.
- 409: business rule conflict, such as grading a date before an activity started or after it was retired.

## Business rules

- **Daily grade**: one score (0–5) per activity per day. Saving a grade for a date that already has one updates it. You can grade past days, but not future days.
- **Activity status**: an activity is either Active or Retired. Retiring an activity records a `RetiredDate`. Filters cover the date added and the date retired.
- **Goal progress**: each checkpoint is its own row. The goal stores the latest checkpoint's percentage. The status is derived from that percentage:
  - 0% = Not Started
  - 1–99% = In Progress
  - 100% = Completed

  "Mark completed" logs a 100% checkpoint.
- **Scores**:
  - Today, weekly and monthly scores are earned points ÷ possible points. Possible points are 5 × each active activity × each day in the period.
  - The trend charts show sums: all activities together or all goals together. You can also pick individual activities or goals to show as their own lines.
  - Because the goals line is a sum, it can go above 100.
- **Struggle detection**: the app may offer help after a grade of 0 or 1. It does this when at least 3 of the activity's last 5 grades (up to and including that date) are 0 or 1. The grade popup then asks "Would you like an idea on how to do better?"
- **Recommendations**: stored in the database by category (Activity, Goal or General) and editable on the Recommendations page. Every suggestion shown is logged in `RecommendationHistory`, along with what triggered it (Help Me button or low-score prompt) and the related activity or goal.

## Future scope

- **AI/LLM suggestions**: implement `IRecommendationProvider` (Application layer) with an LLM-backed provider and register it in DI. The UI and API contract stay the same.
- **Multiple users and authentication**: add authentication (for example Entra ID / JWT), then implement `ICurrentUserService` from the authenticated user's claims. The audit fields already come from that service.
