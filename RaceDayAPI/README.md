# RaceDay API (PROG6212 – Part 2)

ASP.NET Core Web API that powers the RaceDay race-event system. It implements the endpoint plan from Part 1
(`/api/auth`, `/api/users/me`, `/api/events`, categories, enrolments and results) using EF Core (code-first)
on SQL Server, JWT authentication with role-based access, Swagger, xUnit tests and a GitHub Actions pipeline.

## Tech stack
- .NET 8, ASP.NET Core Web API (controllers)
- Entity Framework Core 8 (code-first) + SQL Server / LocalDB
- JWT bearer authentication, BCrypt password hashing
- Swagger / Swashbuckle (with XML comments)
- xUnit + `WebApplicationFactory` + EF Core InMemory (tests)
- GitHub Actions (build + test on every push)

## The two roles
| Role | What they can do |
|------|------------------|
| **Organiser** | Create, update and delete **their own** events and categories; view all enrolments for their events; capture and correct results. |
| **Participant** | Browse events and categories; enrol in (and cancel) categories; view their own enrolments. |

Both roles can register, log in, and view/update their own profile. Viewing events, categories and leaderboards is public.
Organiser-only endpoints return **403** for Participants (and vice versa); anonymous callers get **401**.

## Setup and run
**Prerequisites:** .NET 8 SDK, Visual Studio 2022 (or VS Code), SQL Server LocalDB (installed with Visual Studio) or SQL Server.

1. Clone the repo and open `RaceDayAPI.sln`.
2. Check the connection string in `RaceDayAPI/appsettings.json` (`DefaultConnection`). The default uses LocalDB and a database called `RaceDayDB`.
   If you previously ran the Part 1 SQL script, drop that `RaceDayDB` first (or change the database name here) so EF can create its own tables.
3. Create the database from the EF migrations:
   - **Package Manager Console** (Default project = `RaceDayAPI`): `Add-Migration InitialCreate` then `Update-Database`
   - **or CLI:** `dotnet tool install --global dotnet-ef`, then from the repo root:
     `dotnet ef migrations add InitialCreate --project RaceDayAPI` and `dotnet ef database update --project RaceDayAPI`
   
   (The app also applies any pending migrations when it starts.)
4. Run the API (`F5` in Visual Studio, or `dotnet run --project RaceDayAPI`). Swagger opens at `/swagger`.
5. In Swagger: `POST /api/auth/register` → `POST /api/auth/login` → click **Authorize** and paste the token.

> The JWT signing key in `appsettings.json` is for development only. Use user-secrets or an environment variable for anything real.

## Run the tests
```
dotnet test
```
The tests start the real API in memory with an in-memory database, so they exercise the controllers, JWT authentication and role checks.
They cover: registration/login/password hashing, profile access, event management, category management,
role rejection (403/401), enrolments (including full/duplicate cases), and results/leaderboard.

## CI/CD
`.github/workflows/dotnet.yml` restores, builds and runs all unit tests on every push.

![Successful green build](docs/green-build.png)

<!-- TODO: take a screenshot of the green GitHub Actions run, save it as docs/green-build.png -->

## Endpoints
| Area | Method & route | Role |
|------|----------------|------|
| Auth | `POST /api/auth/register` | Public |
| Auth | `POST /api/auth/login` | Public |
| Profile | `GET /api/users/me`, `PUT /api/users/me` | Any logged-in user |
| Events | `GET /api/events`, `GET /api/events/{id}` | Public |
| Events | `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` | Organiser (owner) |
| Categories | `GET /api/events/{id}/categories` | Public |
| Categories | `POST /api/events/{id}/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/categories/{id}/enrol`, `GET /api/users/me/enrolments`, `DELETE /api/enrolments/{id}` | Participant |
| Enrolments | `GET /api/categories/{id}/enrolments` | Organiser (owner) |
| Results | `POST /api/enrolments/{id}/result`, `PUT /api/results/{id}` | Organiser (owner) |
| Results | `GET /api/categories/{id}/result` | Public |

## Project structure
```
RaceDayAPI/            Web API (Controllers, Models, Data/AppDbContext, DTOs, Services, Program.cs)
RaceDayAPI.Tests/      xUnit tests
.github/workflows/     GitHub Actions workflow
```

## Demo video
<!-- TODO: paste your unlisted YouTube link here -->
YouTube (unlisted): https://youtu.be/YOUR-LINK-HERE
