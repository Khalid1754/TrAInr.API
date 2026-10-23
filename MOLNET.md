# TrAInr i molnet

Så här lägger du TrAInr på internet så att du når den från iPhonen även när datorn är avstängd. Allt är gratis och appen skyddas med inloggning, där du har ett eget konto.

**Vad du får:** en adress som `https://trainr-khalid.azurewebsites.net` som fungerar var du än är. Varje gång något nytt slås ihop i `main` på GitHub uppdateras appen i molnet av sig själv.

**Hur det är byggt:**

| Del | Var | Kostnad |
|---|---|---|
| Appen (API + React) | Azure App Service, nivå **Free F1** | Gratis |
| Databasen | Azure SQL Database, **gratiserbjudandet** (SQL Server, öppnas i SSMS precis som förut) | Gratis, 32 GB |
| Uppdateringar | GitHub Actions (`.github/workflows/azure.yml`) | Gratis |

Det tar ungefär 30 minuter första gången. Du gör allt i webbläsaren på datorn.

> **Lösenord och nycklar** skriver du bara in i Azure och GitHub, aldrig i chatten.

---

## 1. Skapa ett Azure-konto

1. Gå till https://azure.microsoft.com/free och klicka **Start free** (eller **Kom igång kostnadsfritt**).
2. Logga in med ett Microsoft-konto (t.ex. Hotmail/Outlook) eller skapa ett.
3. Azure frågar efter ett betalkort. Det används bara för att kontrollera att du är en riktig person. Allt nedan är gratisnivåer, så inget dras.
   *Studerar du?* Då kan du i stället använda https://azure.microsoft.com/free/students som inte kräver kort.

## 2. Skapa databasen (gratis SQL Server)

1. Gå till https://portal.azure.com och sök efter **Azure SQL** högst upp. Välj **SQL databases** → **+ Create** (Skapa).
2. Längst upp på sidan står det *"Want to try Azure SQL Database for free?"*. Klicka **Apply offer** (Tillämpa erbjudande). Nu står det att databasen är gratis.
3. Fyll i:
   - **Resource group:** klicka *Create new* och skriv `trainr`.
   - **Database name:** `TrAInr`
   - **Server:** klicka *Create new*:
     - **Server name:** något unikt, t.ex. `trainr-khalid` (adressen blir `trainr-khalid.database.windows.net`).
     - **Location:** `Sweden Central` (eller `North Europe` om Sweden inte går att välja).
     - **Authentication method:** *Use SQL authentication*.
     - **Server admin login:** t.ex. `trainradmin`. **Password:** hitta på ett starkt lösenord och spara det i t.ex. iPhones lösenordsapp. Det här är databasens lösenord, inte appens.
   - **Behavior when free limit reached:** *Auto-pause the database until next month*. Då kan det aldrig kosta något.
4. Klicka **Next: Networking**:
   - **Connectivity method:** *Public endpoint*.
   - **Allow Azure services and resources to access this server:** **Yes** (så att appen når databasen).
   - **Add current client IP address:** **Yes** (så att du kan öppna den i SSMS från datorn).
5. Klicka **Review + create** → **Create**. Vänta tills det står *Your deployment is complete*.
6. Klicka **Go to resource**. I menyn till vänster: **Settings → Connection strings**. Kopiera texten under **ADO.NET (SQL authentication)** och klistra in den i Anteckningar. Byt ut:
   - `{your_password}` mot databaslösenordet från steg 3,
   - `Connection Timeout=30` mot `Connection Timeout=60` (gratisdatabasen somnar när den inte används och behöver lite tid att vakna).

   Låt Anteckningar vara öppet, du behöver texten i steg 4.

## 3. Skapa appen (gratis webbserver)

1. Sök efter **App Services** högst upp i portalen → **+ Create** → **Web App**.
2. Fyll i fliken **Basics**:
   - **Resource group:** `trainr`
   - **Name:** t.ex. `trainr-khalid`. Det blir en del av adressen.
   - **Publish:** *Code*
   - **Runtime stack:** *.NET 10 (LTS)*
   - **Operating System:** *Linux*
   - **Region:** samma som databasen.
   - **Pricing plan:** klicka *Create new*, döp den till `trainr-plan`, och välj **Free F1** under *Pricing plan* (klicka *Explore pricing plans* om du inte ser den).
3. Gå till fliken **Deployment** och sätt **Basic authentication** till **Enable**. Det behövs för att GitHub ska få lägga upp appen.
4. Klicka **Review + create** → **Create** → **Go to resource**.
5. På översiktssidan (**Overview**) står adressen under **Default domain**. Det är din app. Spara den.

## 4. Ställ in appen

1. I appens meny till vänster: **Settings → Environment variables**. Under fliken **App settings**, klicka **+ Add** för varje rad:

   | Name | Value |
   |---|---|
   | `ConnectionStrings__Trainr` | hela texten från Anteckningar (steg 2.6) |
   | `Auth__InviteCode` | en kod som krävs för att skapa konto, så att främlingar inte kan registrera sig. Hitta på en egen. |
   | `Database__MigrateOnStartup` | `true` (skapar tabellerna i databasen automatiskt) |
   | `GROQ_API_KEY` | din Groq-nyckel (samma som i din `.env`-fil på datorn) |

   Använder du Gemini i stället lägger du till `GEMINI_API_KEY`. Observera de **två** understrecken i namnen.
2. Klicka **Apply** längst ner och sedan **Confirm**.
3. I menyn: **Settings → Configuration** → fliken **General settings**:
   - **Startup Command:** `dotnet TrAInr.Api.dll`
   - **HTTPS Only:** *On*
   - Klicka **Save** → **Continue**.

## 5. Koppla GitHub så att appen läggs upp

1. Gå tillbaka till **Overview** och klicka **Download publish profile** i raden högst upp. En fil som slutar på `.PublishSettings` laddas ner.
2. Öppna filen i Anteckningar (högerklicka → *Öppna med* → *Anteckningar*) och kopiera **allt** (Ctrl+A, Ctrl+C).
3. Gå till https://github.com/Khalid1754/TrAInr.API → **Settings** → **Secrets and variables** → **Actions**.
4. Fliken **Secrets** → **New repository secret**:
   - **Name:** `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Secret:** klistra in (Ctrl+V) → **Add secret**.
5. Fliken **Variables** → **New repository variable**:
   - **Name:** `AZURE_WEBAPP_NAME`
   - **Value:** appens namn från steg 3.2, t.ex. `trainr-khalid` → **Add variable**.
6. Radera den nedladdade `.PublishSettings`-filen, den innehåller en nyckel till appen.

## 6. Lägg upp appen

1. På GitHub: fliken **Actions** → **Publicera till Azure** i listan till vänster → **Run workflow** → **Run workflow**.
2. Vänta tills raden får en grön bock (ca 5 minuter). Blir den röd, klicka på den och skicka en skärmdump till Claude.
3. Öppna adressen från steg 3.5 i webbläsaren. Första gången tar det upp till en minut, för databasen skapas. Klicka **Skapa konto**, fyll i e-post, ett lösenord och koden från `Auth__InviteCode`.

Därefter sker det av sig själv: varje gång en ändring slås ihop i `main` läggs den nya versionen ut.

## 7. På iPhonen

1. Öppna adressen i **Safari** och logga in. Välj **Spara lösenord** när iPhonen frågar.
2. Tryck på dela-knappen → **Lägg till på hemskärmen**.
3. Ta bort den gamla TrAInr-ikonen som pekade på datorn, så att du inte blandar ihop dem.

Du förblir inloggad i ett år.

> **Har du redan `Auth__Password` i Azure?** Det fungerar nu som inbjudningskod, så du behöver inte ändra något. Efter uppdateringen skapar du ett konto med koden. Den som skapar konto först får alla pass som redan finns i databasen.

---

## Bra att veta

- **Långsam första gång.** Gratisnivåerna somnar när de inte används (appen efter ca 20 minuter, databasen efter en timme). Första gången du öppnar appen efter en paus tar det 30–60 sekunder, sedan går det snabbt.
- **Gränser.** Appen får använda 60 minuter processortid per dag och databasen 100 000 vCore-sekunder per månad. För en person räcker det gott. Tar databasens kvot slut pausas den till nästa månad i stället för att kosta pengar.
- **Datan på datorn flyttas inte.** Molnet börjar med en tom databas. Vill du ha med dina gamla pass, säg till så hjälper Claude dig flytta dem.
- **SSMS.** Anslut till `trainr-khalid.database.windows.net` med *SQL Server Authentication*, inloggningen `trainradmin` och databaslösenordet. Byter du nätverk (t.ex. annat wifi) kan du behöva lägga till din nya IP-adress under databasserverns **Networking** igen.
- **Docker hemma.** Även där skapar du ett konto första gången, och dina gamla pass följer med till det. Vill du kräva en kod sätter du `INVITE_CODE` i `.env`.
