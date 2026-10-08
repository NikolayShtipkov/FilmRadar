# FilmRadar

Интелигентен филмов навигатор за курсова работа: **ASP.NET Core MVC, .NET 10, C#, Razor Views, EF Core 10 и SQLite**. Търси в TMDB, съхранява личен списък и предлага филми чрез обясним mock AI.

## Стартиране

Необходими са .NET 10 SDK, достъп до NuGet при първия restore и writable папка на проекта. `global.json` допуска стабилен .NET 10 SDK от инсталираните feature bands. Не се изискват SQL Server, Node.js, Python или Docker за приложението.

Изпълнете от папката, съдържаща `FilmRadar.sln`:

```powershell
dotnet restore FilmRadar.sln
dotnet tool restore
dotnet build FilmRadar.sln --no-restore
dotnet tool run dotnet-ef database update --project src/FilmRadar.Web
dotnet test FilmRadar.sln --no-restore
dotnet run --project src/FilmRadar.Web --launch-profile http
```

Отворете **http://localhost:5133**. При първото стартиране в Development миграциите се прилагат автоматично, след което се създават един локален профил и 19 базови жанра. Посочената отделна EF команда е полезна за проверка и ръчно управление на схемата.

### TMDB token

Приложението стартира и без token. Профилът и локалният списък работят, а каталогът показва обяснение за липсващата конфигурация. За реално търсене и препоръки вземете **API Read Access Token** от собствения си TMDB акаунт. Това е Bearer token, не краткият v3 API key.

В проекта има празен шаблон `src/FilmRadar.Web/appsettings.Local.example.json`. След изтегляне от GitHub го копирайте:

```powershell
Copy-Item src/FilmRadar.Web/appsettings.Local.example.json src/FilmRadar.Web/appsettings.Local.json
```

Отворете `appsettings.Local.json` и попълнете токена, без префикса `Bearer`:

```json
{
  "Tmdb": {
    "ReadAccessToken": "YOUR_TMDB_READ_ACCESS_TOKEN"
  }
}
```

Рестартирайте приложението след промяна. Празният `.example.json` се предава в Git, а попълненият `appsettings.Local.json` се игнорира и не се копира в build/publish output. Липсващ или празен локален файл позволява работа без token.

User Secrets също остават поддържани, ако вече сте настроили токена там:

```powershell
dotnet user-secrets set "Tmdb:ReadAccessToken" "YOUR_TMDB_READ_ACCESS_TOKEN" --project src/FilmRadar.Web
```

Алтернатива за текущия PowerShell процес:

```powershell
$env:Tmdb__ReadAccessToken = "YOUR_TMDB_READ_ACCESS_TOKEN"
dotnet run --project src/FilmRadar.Web --launch-profile http
```

Непразните стойности от локалния файл имат предимство пред базовите JSON настройки и User Secrets; environment variables и command-line аргументите имат най-висок приоритет. Празните стойности от шаблона не изчистват вече настроен User Secrets token.

Заменете примерната стойност локално. Не добавяйте реален token в Git, `appsettings.json`, `appsettings.Local.example.json`, `launchSettings.json`, screenshots или логове. User Secrets се зареждат само в Development. Приложението не изписва Authorization headers, токена или търсеното заглавие в HTTP логовете.

TMDB настройки без тайни се намират в `src/FilmRadar.Web/appsettings.json`. Заявките използват `bg-BG`, регион `BG`, `include_adult=false` и timeout 10 секунди. Няма автоматичен English fallback или retries.

## Visual Studio 2026

1. Инсталирайте workload **ASP.NET and web development** и .NET 10 SDK. Използвайте актуализирана Visual Studio 2026, съвместима с инсталирания SDK.
2. Отворете `FilmRadar.sln`.
3. Задайте `FilmRadar.Web` като Startup Project.
4. Изберете профил `http` и F5. За примерните данни изберете `demo`.
5. Тестовете се откриват от Test Explorer. За token копирайте локалния шаблон и попълнете `appsettings.Local.json`. Manage User Secrets на web проекта остава алтернатива.

Профилът `https` е наличен след доверяване на development сертификата с `dotnet dev-certs https --trust`. Не заобикаляйте предупрежденията за сертификат в браузъра.

## VS Code

Отворете цялата папка `FilmRadar`, не само `src`. Инсталирайте препоръчаните C# и C# Dev Kit разширения. `.vscode/launch.json` осигурява F5 debugging, а `tasks.json` съдържа build, test и database update. Може да използвате и същите CLI команди в integrated terminal. EF task изисква предварителен `dotnet tool restore`.

## Функционалности

- Начало с популярни филми и разбираем offline/configuration state.
- Търсене по заглавие и точна година през `/search/movie`.
- Отделно откриване по жанр, период и минимален TMDB рейтинг през `/discover/movie`, с pagination до 500.
- Детайли с описание, жанрове, дата, продължителност и TMDB рейтинг. Може да добавите филма за гледане или директно като гледан с отваряне на редакцията. Филм, който вече е за гледане, може да се отбележи като гледан от същата страница.
- Моят списък: „За гледане“, „Гледан“, любим, бележка, дата и лична оценка.
- Профил с любими жанрове и начални критерии за препоръките.
- Препоръки по настроение с резултат 0–100 и до четири подкрепени от данните причини.
- Страница „За проекта“ с технологии, алгоритъм и TMDB attribution.

| Лична оценка | Значение |
|---|---|
| Не е оценен (`null`) | Няма лична оценка |
| 0 | За нищо не става |
| 1 | Зле |
| 2 | Става |
| 3 | Важи |
| 4 | Топ |

Личната скала 0–4 е отделна от TMDB рейтинга 0–10. „За гледане“ винаги има празни оценка и дата. „Гледан“ изисква дата, но може да няма оценка. При празна дата се използва днешната дата на сървъра. Връщане към „За гледане“ изисква изрично потвърждение и изчиства оценката и датата. Любимият статус и бележката са независими.

## Архитектура

```text
Razor Views / Input ViewModels
             |
        MVC Controllers
             |
     Application Services
       /              \
EF Core + SQLite    ITmdbClient + HttpClient
       \              /
     RecommendationService (async workflow)
             |
     MockRecommendationEngine (pure C#)
```

`Domain/` съдържа entities и enums. `Data/` съдържа DbContext, миграции и seed. `Integrations/Tmdb/` изолира DTO, конфигурацията и HTTP обработката. `Services/` съдържа бизнес правилата. Контролерите приемат ограничени Input ViewModels, а не EF entities. Всички I/O операции използват `async/await` и предават `CancellationToken`. Не се изпълняват паралелни EF операции върху един DbContext.

Данните са в `src/FilmRadar.Web/App_Data/filmradar.db`. `App_Data/keys` съхранява локалните Data Protection ключове за antiforgery/TempData. Тези файлове не се предават в Git. SQLite пази профила, жанровете, предпочитанията, movie snapshots и join таблиците. Unique index защитава от повторен запис на един филм. Check constraints защитават диапазоните и зависимостта статус/оценка/дата. Read заявките използват `AsNoTracking`.

## Mock AI

1. Празните граници и минимален рейтинг наследяват профила поотделно. Ефективните стойности се показват над резултатите.
2. Текущите избрани жанрове са OR филтър към TMDB. Любимите профилни жанрове участват в scoring, а не като скрит допълнителен филтър.
3. Зареждат се до 3 страници и максимум 60 уникални кандидата. Гледаните се изключват с една batch справка. Няма details заявка за всеки кандидат.
4. Pure engine изчислява резултат и връща top 10. Watchlist има badge, но няма бонус.

| Компонент | Тегло | Стойност 0–1 |
|---|---:|---|
| Любими жанрове | 35 | Съвпадащи / всички жанрове на кандидата |
| История на оценки | 25 | Средно genre affinity, изчислено от личните оценки / 4 |
| Настроение | 15 | Съвпадащи mood жанрове / всички жанрове на кандидата |
| TMDB | 15 | `(rating / 10) * min(votes / 200, 1)` |
| Период | 10 | 1 при дата в границите, иначе 0 |

`Score = 100 * sum(activeWeight * value) / sum(activeWeight)`. Жанровият компонент е активен само с предпочитания, историята само с лични оценки, периодът само с поне една граница. Настроение и TMDB винаги са активни. Един и същ знаменател се използва за всички кандидати в заявката. Неизвестна history affinity е 0.5 без положителна причина. Липсващи метаданни дават нулев принос за съответния активен компонент.

Подреждането е score, TMDB рейтинг, брой гласове низходящо, след това TMDB ID възходящо. Score се закръгля само за показване. Това е детерминистична симулация на интелигентна логика, без LLM, обучение на модел или платен AI API.

### Mock AI и възможно надграждане

**Какво прави сега?** Mock AI подрежда филмите чрез зададени правила и формули. Използва жанровете, настроението и личните оценки, за да изчисли съвпадение 0–100 и да обясни избора. Еднакви входни данни дават еднакъв резултат.

**Как би работил реален AI?** Езиков модел би могъл да анализира и описанията на филмите и желания като „нещо леко, но без романтика“, след което да предложи заглавия с обяснения според контекста. Предпочитанията и оценките се подават към заявката; това само по себе си не означава обучение на модела.

**Най-лесният подход:** запазваме сегашното подреждане и добавяме AI API, например Gemini, който пише кратки персонализирани обяснения за вече избраните филми. Отделен async service, например `IAiExplanationService`, изпраща една заявка чрез `HttpClient` с предпочитанията и до 10 избрани филма. Моделът връща JSON с TMDB ID и обяснение; сървърът проверява структурата, допустимите ID и дължината, преди да покаже текста. При timeout или невалиден отговор остават сегашните обяснения. API ключът се пази в игнорирания локален конфигурационен файл и се използва само от сървъра. Това добавя реална AI функция без собствено обучение на модел. Интеграцията е идея за бъдещо развитие и още не е реализирана.

Вижте [официалната документация за JSON отговори от Gemini API](https://ai.google.dev/gemini-api/docs/structured-output). JSON форматът не гарантира верността на текста, затова заявката трябва да ограничава обясненията до подадените факти.

## Demo и защита

```powershell
dotnet run --project src/FilmRadar.Web --launch-profile demo
```

Този Development профил използва отделна `App_Data/filmradar-demo.db` и idempotent seed с шест **измислени**, ясно обозначени записа: две оценки 4, една 0, няколко жанра и един филм за гледане. Те нямат TMDB detail страници и използват локален placeholder. Картите на запазените филми отварят локалния запис и работят offline. Seed не заменя live каталога и не подправя API резултати. При ново стартиране в demo режим липсващите demo записи се възстановяват.

- [Сценарий за демонстрация](docs/demo-script.md)
- [Резултати и оставащи ръчни проверки](docs/verification.md)
- [Презентация за защита](output/pdf/FilmRadar-Presentation.pdf)
- [Сценарий за браузър и screenshots](scripts/browser-smoke.py)

Реални screenshots още не са включени: текущата ограничена среда не позволява старт на браузъра. Скриптът ги създава в `docs/screenshots/` при изпълнение на ваша машина, без да представя mock изображения като проверен интерфейс.

## Тестове

```powershell
dotnet test FilmRadar.sln
dotnet test FilmRadar.sln --collect:"XPlat Code Coverage"
```

xUnit тестовете покриват scoring, confidence, null/0/4, ties, bounded workflow, SQLite миграции и constraints, CRUD, profile/genre sync, HTTP serialization/error/cancellation и MVC форми с antiforgery. Използва се реалният SQLite provider in-memory, не EF InMemory. TMDB е заменен с fake или тестов HttpMessageHandler, така че тестовете са повторяеми без token и интернет.

## Данни, reset и deployment

За безопасен development reset спрете приложението. Преместете **само конкретната база** `filmradar.db` (или `filmradar-demo.db`) и съответните `-wal`/`-shm`, ако съществуват, в backup папка. Не изтривайте цялата `App_Data`, `.git` или workspace. При следващ Development старт схемата и началният профил се създават отново. За връщане на backup спрете приложението и възстановете същия комплект файлове. Не копирайте активна SQLite база без подходящ backup механизъм.

За друга база задайте `ConnectionStrings__FilmRadar` с **абсолютен** SQLite път и `Foreign Keys=True`. Същата променлива се използва и от EF design-time factory.

```powershell
dotnet publish src/FilmRadar.Web -c Release -o artifacts/publish
```

В Production няма automatic migrations: приложете миграциите предварително към правилната база. Нужни са writable data/key directories, HTTPS и конфигурация на secrets. **Този MVP няма автентикация и има един споделен локален профил. Не го излагайте публично.** `AllowedHosts` е ограничен до localhost, но не е заместител на автентикация. Multi-user deployment изисква Identity, собственост върху данните, защита на ключовете и отделен security review.

## Ограничения и attribution

Интернет и валиден TMDB token са нужни за каталог, детайли, poster изображения и нови препоръки. При 401/403, 429, timeout или невалиден payload се показват безопасни съобщения. Локалните snapshots не се обновяват автоматично. Няма real AI, регистрация, trailers, streaming providers, retry pipeline или offline каталог. Филтрите за минимален рейтинг в UI са целочислени 0–10.

This product uses the TMDB API but is not endorsed or certified by TMDB.

Вижте [TMDB attribution и лицензи на зависимостите](docs/third-party-notices.md). Реализацията следва `../implementation-plan-filmradar.md` и консенсусния преглед. [Официалната TMDB документация](https://developer.themoviedb.org/docs/getting-started) описва получаването на API достъп.
