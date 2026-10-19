# TrAInr

Träningsapp där du loggar dina pass och får hjälp av AI (Claude) med:

1. **Träningsschema**: ett veckoschema anpassat efter din profil, ditt mål och dina loggade pass, med föreslagna vikter.
2. **Kaloriförbränning**: uppskattning av hur många kalorier du bränt utifrån övning, vikt, set, reps och kroppsvikt.
3. **Återhämtningsmåltid**: den ideala måltiden efter passet, med makron, ingredienser och recept.

## Teknik

| Del | Teknik | Mapp |
|---|---|---|
| Backend | .NET 10, ASP.NET Core Web API, EF Core 10 | `backend/TrAInr.Api` |
| Tester | xUnit | `backend/TrAInr.Api.Tests` |
| Frontend | React 19, TypeScript, Vite | `frontend` |
| Databas | SQL Server (hanteras i SSMS) | migreringar i `backend/TrAInr.Api/Data/Migrations`, SQL-skript i `database/TrAInr.sql` |
| AI | Claude via det officiella Anthropic-paketet för .NET | `backend/TrAInr.Api/Services/TrainerAi.cs` |

## Kom igång

Du behöver .NET 10 SDK, Node.js 20+ och SQL Server (t.ex. SQL Server Express/Developer eller `docker compose up -d`).

### 1. Databasen

Standardanslutningen i `backend/TrAInr.Api/appsettings.json` är `Server=localhost;Database=TrAInr;Trusted_Connection=True;TrustServerCertificate=True` (Windows-inloggning mot lokal SQL Server). Ändra den om du kör en namngiven instans, t.ex. `Server=localhost\SQLEXPRESS;...`, eller Docker: `Server=localhost,1433;Database=TrAInr;User Id=sa;Password=TrAInr_Dev123!;TrustServerCertificate=True`.

Databasen och tabellerna skapas automatiskt när backenden startar i utvecklingsläge. Vill du hellre skapa dem själv kan du antingen:

- öppna `database/TrAInr.sql` i SSMS och köra det, eller
- köra `dotnet ef database update` i `backend/TrAInr.Api` (kräver `dotnet tool install --global dotnet-ef`).

Tabeller: `Profiles`, `Workouts`, `Exercises` och EF:s `__EFMigrationsHistory`.

### 2. Backend

```bash
cd backend/TrAInr.Api
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."   # skapa nyckeln på console.anthropic.com
dotnet run --launch-profile http
```

API:t körs på http://localhost:5028. Nyckeln kan också sättas med miljövariabeln `ANTHROPIC_API_KEY`. Utan nyckel fungerar loggning, historik och den regelbaserade kaloriuppskattningen; AI-knapparna visar då ett meddelande om att AI är avstängt.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

Öppna http://localhost:5173. Vite skickar alla anrop till `/api` vidare till backenden.

### Tester

```bash
dotnet test TrAInr.slnx
```

## Hur det fungerar

- **Kalorier i två steg.** `CalorieEstimator` räknar direkt med MET-metoden (kcal = MET × kroppsvikt × tid, där tiden räknas fram ur set, reps och vila, och tung vikt relativt kroppsvikten höjer intensiteten). Den visas medan du loggar. AI:n kan sedan förfina den med intervall och efterförbränning.
- **Strukturerade AI-svar.** Varje AI-funktion har en C#-typ (`Services/AiModels.cs`) som görs om till ett JSON-schema. Claude måste svara enligt schemat, så svaren går alltid att visa i gränssnittet.
- **AI-svaren sparas** på passet i databasen så att de finns kvar i historiken. Ändras passet rensas de.
- **Modell:** `claude-opus-5-5` som standard, ändras under `Ai:Model` i `appsettings.json` (t.ex. `claude-sonnet-5-5` eller `claude-haiku-5-5` för lägre kostnad).

## API

| Metod | Sökväg | |
|---|---|---|
| GET/PUT | `/api/profile` | Läs/uppdatera profil |
| GET/POST | `/api/workouts` | Lista/skapa pass |
| GET/PUT/DELETE | `/api/workouts/{id}` | Hämta/ändra/ta bort pass |
| POST | `/api/estimate` | Snabb kaloriuppskattning för ett osparat pass |
| POST | `/api/ai/plan` | AI-veckoschema (`{ "wishes": "..." }`) |
| POST | `/api/workouts/{id}/ai/calories` | AI-kaloriuppskattning |
| POST | `/api/workouts/{id}/ai/meal` | AI-återhämtningsmåltid |

OpenAPI-beskrivningen finns på `/openapi/v1.json` i utvecklingsläge.

## Nästa steg

- Inloggning och flera användare (idag finns en profil med Id 1)
- Grafer över progression per övning
- Rådet är allmänt och ersätter inte medicinsk rådgivning
