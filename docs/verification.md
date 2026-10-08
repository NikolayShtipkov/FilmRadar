# Implementation and verification

## Scope delivered

The implementation follows the revised `implementation-plan-filmradar.md` and `review-consensus.md` in the parent workspace. Historical review suggestions superseded by that plan were not treated as additional requirements.

| Coursework requirement | Implementation |
|---|---|
| C#, ASP.NET Core MVC, Razor, .NET 10 | `src/FilmRadar.Web`, controllers and Razor views |
| At least three pages and input forms | Home, Search/Discover, Details, My List, Edit, Profile, Recommendations, About |
| User data processing and persistence | EF Core SQLite, profile/preferences, snapshots, ratings, notes, genre joins |
| Async external API via HttpClient | Typed `TmdbClient`, cancellation, timeout and controlled errors |
| Mock AI | Deterministic scoring engine with explicit weights and explanations |
| Rating 0–4 and genres | Nullable personal enum, genre preferences, learned genre affinity |
| Visual Studio 2026 and VS Code | Classic `.sln`, `net10.0`, launch profiles, `.vscode` configs |
| Defense material | 13-slide PDF, reproducible demo data and demonstration script |

The MVP deliberately does not include Identity, real AI, extra architecture projects, generic repositories, SPA, retry policy, English fallback or caching. Search pagination and controller tests beyond the minimum are included. UI minimum-rating fields use whole numbers 0–10 to avoid ambiguous decimal binding across cultures. SQLite stores the corresponding numeric criterion.

## Automated verification

Environment: Windows, .NET SDK **10.0.401**, runtime **10.0.12**. Latest full test execution: **98 passed, 0 failed, 0 skipped**, Release configuration.

- Pure scoring fixtures cover cold start, active normalization, missing metadata, neutral history, zero history, genre ratios, mood changes, vote confidence, tie-breaks and reason limits.
- SQLite tests apply the actual initial migration and verify idempotent initialization, schema/model consistency, unique/check constraints, nullable ratings, state reset, genre/profile edits and cascading deletion.
- TMDB tests inspect endpoint, query encoding, OR genres, dates, invariant decimal formatting and header authentication. They simulate 401/403/429/500, timeout, cancellation, invalid JSON and invalid response structure.
- Workflow tests verify profile defaults per field, conflicting effective bounds, deduplication, watched exclusions, watchlist badges, top 10, maximum 3 pages/60 candidates and no per-candidate details calls.
- MVC tests render pages, reject missing antiforgery tokens, exercise add/edit/zero/reset/delete, verify Razor encoding and invalid form redisplay, reject invalid search filters before HTTP calls, and check degraded/404 behavior.

These tests use deterministic fakes for TMDB, not the live API. No genuine TMDB token was added to source or used for testing.

The project-local configuration update adds a tracked empty `appsettings.Local.example.json` and an ignored `appsettings.Local.json`. Three configuration cases verify missing/blank files, a populated local token and command-line precedence. Blank local values preserve an existing User Secrets token. Release publish was checked and does not include the local token file. Environment variables and command-line arguments retain priority. The local file is loaded at startup; restart after editing it.

Movie details now support adding directly as watched and opening the editor. The initial watched state and date are saved together. An existing watchlist record is promoted without duplicating the movie; existing ratings, dates, favorites and notes on watched records are preserved. MVC tests cover new/watchlist/watched cases and antiforgery rejection for the new POST action.

The restricted environment cannot use the normal Windows NuGet TLS path. Official NuGet packages were downloaded over verified HTTPS into an external workspace-local package source, then restored with that source. `NuGetAudit=false` was passed only to these local restore commands, not committed as project configuration. Normal users should run the README commands against NuGet.org and perform the normal vulnerability audit. MSBuild used `-m:1 -p:UseSharedCompilation=false` because worker-process pipes are restricted here.

`dotnet format whitespace --folder` and Prettier formatting ran successfully. The full semantic `dotnet format` command could not connect to its build-host pipe in this sandbox.

## Delivery checks

- Release compilation and test execution succeeded.
- Development demo startup applied migrations and listened on localhost without a token.
- PDF rendered to images and all 13 slides were visually inspected, including Bulgarian glyphs.
- Local TMDB logo has source and license attribution. Tokens, databases, keys, bin/obj, test outputs and QA tooling are excluded from source delivery.
- A clean-source export with 133 source/documentation/asset files excluded `.git`, `bin`, `obj`, `App_Data`, temporary QA output and test results. Restore, tool restore, Release build, all 91 tests, explicit EF migration and publish succeeded in that export. Build reported **0 warnings and 0 errors**. This was an isolated export of the uncommitted work, not a claim that a new Git commit or checkout was created.
- The clean export then started on `http://localhost:5134` without a token. All **14 HTTP route/asset checks** passed: core pages, CSS/JS, placeholder and TMDB logo, controlled details 503, missing-page 404, and database-path 404. Parsed HTML had one H1 and no duplicate element IDs on the checked pages. These are HTTP/markup checks, not browser-rendering checks. The verification server was stopped afterward.

## Remaining manual acceptance checks

The source implementation is delivered, but **Gate D is not fully verified** until the following environment-dependent checks are completed:

- [ ] Configure a genuine TMDB Read Access Token and exercise live Search, Details, Save, genre refresh and Recommendations.
- [ ] Open `FilmRadar.sln` in Visual Studio 2026 and run/debug from the IDE.
- [ ] Open the folder in VS Code and run the supplied F5 configuration.
- [ ] Run `scripts/browser-smoke.py`, visually inspect desktop/mobile screenshots and check keyboard navigation and contrast.
- [ ] Save screenshots of live results and rehearse the complete 5–8 minute defense.
- [ ] Run normal NuGet restore/audit on an unrestricted development machine.

Browser validation was attempted through the available browser connector, Playwright and Edge's headless runner. No connected browser was available and Windows denied the required process pipes. No browser sandbox was disabled to bypass this restriction. There are no fabricated screenshots or claims of successful visual browser testing.

The code is configured for both requested IDEs, but neither IDE's UI was launched during this run. The PDF uses the PDF workflow because the presentation-specific artifact runtime was unavailable. The plan permits PDF instead of PPTX.
