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
| AI | Groq eller Google Gemini (gratis) eller Claude via det officiella Anthropic-paketet | `backend/TrAInr.Api/Services/TrainerAi.cs`, `AiProviders.cs` |

## Kör allt i Docker (enklast)

Du behöver bara [Docker Desktop](https://www.docker.com/products/docker-desktop/).

**Enklast i Windows:** dubbelklicka på `start.bat`. Den startar Docker Desktop om det behövs, bygger och startar appen och öppnar http://localhost:8080. Dubbelklicka på `stopp.bat` för att stänga av. Containrarna har `restart: unless-stopped`, så har du startat appen en gång kommer den upp av sig själv när Docker Desktop startar, tills du stoppar den.

Från terminalen står du i mappen där `docker-compose.yml` ligger och kör:

```bash
docker compose up -d --build
```

Det startar tre containrar:

| Container | Adress | Innehåll |
|---|---|---|
| `frontend` | http://localhost:8080 | React-appen (öppna den här i webbläsaren) |
| `api` | http://localhost:5028 | .NET-API:t, skapar databastabellerna automatiskt vid start |
| `sqlserver` | `localhost,1433` | SQL Server 2022, användare `sa`, lösenord `TrAInr_Dev123!` |

Datan sparas i Docker-volymen `sqldata`, så den finns kvar mellan omstarter. Anslut med SSMS till `localhost,1433` med SQL Server Authentication och kryssa i "Trust server certificate".

För AI-funktionerna: kopiera `.env.example` till `.env`, fyll i en nyckel (se [AI-nyckel](#ai-nyckel)) och kör `docker compose up -d` igen. `.env` checkas aldrig in i git.

Stoppa med `docker compose down` (lägg till `-v` om du även vill radera databasen).

## Appen på iPhone (samma wifi)

1. Dubbelklicka på `mobil-atkomst.bat` en gång och godkänn frågan om administratörsbehörighet. Den öppnar port 8080 i Windows-brandväggen för privata nätverk. Wifi-nätverket måste vara satt som **Privat** i Windows.
2. Starta appen med `start.bat`. Fönstret visar adressen för mobilen, till exempel `http://192.168.1.23:8080`.
3. Öppna adressen i Safari på iPhonen, tryck på dela-knappen och välj **Lägg till på hemskärmen**. Då får du en TrAInr-ikon som öppnar appen i helskärm.

Datorn måste vara igång och appen startad för att mobilen ska nå den.

## Appen i molnet (utan datorn)

Vill du nå appen var du än är, även när datorn är avstängd, kan den ligga gratis i Azure med SQL Server-databas och lösenordsskydd. Följ steg-för-steg-guiden i [MOLNET.md](MOLNET.md).

## Inloggning

Sätt `Auth:Password` (`Auth__Password` som miljövariabel, `APP_PASSWORD` i `.env` för Docker) så måste man logga in med det lösenordet. Är den tom behövs ingen inloggning, vilket passar när appen bara körs hemma. Inloggningen gäller ett år, och byter du lösenord loggas alla gamla inloggningar ut.

## AI-nyckel

Appen fungerar med tre AI-leverantörer:

| Leverantör | Kostnad | Nyckel | Variabel |
|---|---|---|---|
| Groq (`openai/gpt-oss-120b`) | Gratisnivå, mycket snabba svar | https://console.groq.com/keys | `GROQ_API_KEY` |
| Google Gemini (`gemini-3.6-flash`) | Gratisnivå med begränsat antal anrop per minut och dag | https://aistudio.google.com/apikey | `GEMINI_API_KEY` |
| Anthropic Claude (`claude-opus-5-5`) | Betalt, kräver krediter | https://console.anthropic.com | `ANTHROPIC_API_KEY` |

Finns flera nycklar används den första i ordningen Claude, Groq, Gemini. Sätt `AI_PROVIDER=groq` eller `AI_PROVIDER=gemini` (eller `Ai:Provider` i `appsettings.json`) för att välja själv. Modellen ändras med `Groq:Model` (`GROQ_MODEL` i Docker) respektive `Gemini:Model` (`GEMINI_MODEL`).

**Snabbare svar med Gemini:** appen ber Gemini tänka så lite som möjligt (`minimal` för kalorier och måltid, `low` för veckoschemat), vilket ger kortare svarstider än modellens standard. Vill du ha ännu snabbare svar, sätt `GEMINI_MODEL=gemini-3.5-flash-lite` i `.env`. Vill du ha mer genomtänkta svar, sätt `GEMINI_THINKING_LEVEL=medium` eller `high`.

Utan Docker sätter du nyckeln med `dotnet user-secrets set "Gemini:ApiKey" "..."` i `backend/TrAInr.Api`.

## Kom igång utan Docker

Du behöver .NET 10 SDK, Node.js 20+ och SQL Server (t.ex. SQL Server Express/Developer, eller bara databasen i Docker med `docker compose up -d sqlserver`).

### 1. Databasen

Standardanslutningen i `backend/TrAInr.Api/appsettings.json` är `Server=localhost;Database=TrAInr;Trusted_Connection=True;TrustServerCertificate=True` (Windows-inloggning mot lokal SQL Server). Ändra den om du kör en namngiven instans, t.ex. `Server=localhost\SQLEXPRESS;...`, eller Docker: `Server=localhost,1433;Database=TrAInr;User Id=sa;Password=TrAInr_Dev123!;TrustServerCertificate=True`.

Databasen och tabellerna skapas automatiskt när backenden startar i utvecklingsläge. Vill du hellre skapa dem själv kan du antingen:

- öppna `database/TrAInr.sql` i SSMS och köra det, eller
- köra `dotnet ef database update --project backend/TrAInr.Api` från roten (kräver `dotnet tool install --global dotnet-ef`).

Tabeller: `Profiles`, `Workouts`, `Exercises` och EF:s `__EFMigrationsHistory`.

### 2. Backend

```bash
cd backend/TrAInr.Api
dotnet user-secrets set "Gemini:ApiKey" "..."   # gratis nyckel, se AI-nyckel ovan
dotnet run --launch-profile http
```

API:t körs på http://localhost:5028. Nyckeln kan också sättas med miljövariabeln `GEMINI_API_KEY` (eller `ANTHROPIC_API_KEY` för Claude). Utan nyckel fungerar loggning, historik och den regelbaserade kaloriuppskattningen; AI-knapparna visar då ett meddelande om att AI är avstängt.

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
- **Strukturerade AI-svar.** Varje AI-funktion har en C#-typ (`Services/AiModels.cs`) som görs om till ett JSON-schema. AI:n måste svara enligt schemat, så svaren går alltid att visa i gränssnittet.
- **AI-svaren sparas** på passet i databasen så att de finns kvar i historiken. Ändras passet rensas de.
- **Modell:** `claude-opus-5-5` som standard, ändras under `Ai:Model` i `appsettings.json` (t.ex. `claude-sonnet-5-5` eller `claude-haiku-5-5` för lägre kostnad).

## API

| Metod | Sökväg | |
|---|---|---|
| GET | `/api/auth/me` | Om inloggning krävs och om du är inloggad |
| POST | `/api/auth/login` / `/api/auth/logout` | Logga in (`{ "password": "..." }`) / ut |
| GET/PUT | `/api/profile` | Läs/uppdatera profil |
| GET/POST | `/api/workouts` | Lista/skapa pass |
| GET/PUT/DELETE | `/api/workouts/{id}` | Hämta/ändra/ta bort pass |
| POST | `/api/estimate` | Snabb kaloriuppskattning för ett osparat pass |
| POST | `/api/ai/plan` | AI-veckoschema (`{ "wishes": "..." }`) |
| POST | `/api/workouts/{id}/ai/calories` | AI-kaloriuppskattning |
| POST | `/api/workouts/{id}/ai/meal` | AI-återhämtningsmåltid |

OpenAPI-beskrivningen finns på `/openapi/v1.json` i utvecklingsläge.

## Nästa steg

- Flera användare (idag finns ett lösenord och en profil med Id 1)
- Grafer över progression per övning
- Rådet är allmänt och ersätter inte medicinsk rådgivning
