# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed

- Deleting a redirect from the admin redirects list (`IRedirectsService.DeleteById`) now raises `RedirectsEvents.RedirectsUpdated`, like every other write on `DefaultRedirectsService`. Before, the deleted redirect kept applying until the application restarted or another redirect was saved, and `OptimizelySyncEvents` never told the other instances.
- Redirects and `410` responses for deleted URLs now also work when the application starts writing the response body inside the pipeline. The handler now runs just before that response starts. Before, it only ran after the pipeline finished, when changing headers threw an exception. A response that hasn't started by then is still handled at that point, so `UseStatusCodePages*` registered before the handler only sees requests the handler didn't redirect. A redirect applied to a response the application is already writing still carries that body, because it can't be stopped at that point; browsers follow the `Location` header.
- A request whose status is in `ActiveStatusCodes` but has no matching redirect now keeps its status. Before, a response that hadn't started yet was forced to `404`, which broke sites using `ActiveStatusCodes` with `200`.
- When a status code page is re-executed (`UseStatusCodePagesWithReExecute`), redirect lookup and logging use the URL the client requested, not the error page's URL.
- Wildcard redirects now join the new URL and the appended path with exactly one slash. Before, the slash was chosen from the request URL instead of the new URL, so `/content` -> `/legacy` turned `/content/file/` into `/legacyfile/`, and `/content` -> `/legacy/` turned `/content/file` into `/legacy//file`.
- Wildcard redirects no longer produce a protocol-relative URL from a request path with extra leading separators. Before, `/content` -> `/` turned `/content//evil.com` into a redirect to `//evil.com`, which is an open redirect.
- `CustomRedirectCollection.Find` no longer throws `ArgumentOutOfRangeException` when the request repeats the old URL after extra slashes (`/content/` with `/content//content`) or when `/` is the old URL and the request is `/?param=value`.
- The guard against redirects building up now also applies when the old URL ends in a slash, so `/content/` -> `/legacy` turns `/content/content/file` into `/legacy/file`, the same as `/content` -> `/legacy` already did.
- Redirects to a new URL nested under the old one no longer grow without end. With `/a` -> `/a/b`, `/a/x` still redirects to `/a/b/x`, but a request already under `/a/b` is now left as the 404 it is, instead of redirecting to `/a/b/b/x` and on until the browser gave up. This also applies when the new URL includes the host of the request or has encoded characters.
- Setting `UseInternalScheduler` no longer crashes the application at startup with `No service for type 'Microsoft.Extensions.Logging.ILogger'`.
- URLs under an entry in the Deleted list now return `410`. Before, a request such as `/gone/`, `/gone?a=1` or `/gone/child` lost the deleted state and was redirected, often to the home page.

### Security

- The CSV export now escapes values a spreadsheet would run as a formula (starting with `=`, `@`, `+`, `-`, tab, carriage return or line feed), whichever way they got into the database. An escaped value is prefixed with `'`, and importing that file keeps the prefix as part of the value. Only the start of a value is checked, so a formula after a `;` inside a value isn't escaped, which can matter in spreadsheets that use `;` as the list separator.

## [7.1.0] - 2026-07-10

### Added

- `OptimizelyNotFoundHandlerOptions.MovedContentBatchSize` (default 1000) and `NotFoundHandlerOptions.CommandTimeout` so the "Register content move redirects" job pages through large `NotFoundHandler.ContentUrlHistory` tables instead of loading every row in one unbounded query that exceeded the command timeout.
- `IContentUrlHistoryLoader.GetAllMoved(int skip, int take)` for paged access to moved content. Ships as a default interface implementation (pages in-memory over `GetAllMoved()`), so existing implementers keep compiling.

### Changed

- `SqlDataExecutor.ExecuteQuery` now propagates SQL exceptions (e.g. command timeouts) instead of swallowing them and returning an empty result, which previously surfaced as a misleading "Cannot find table 0" error.

## [7.0.0] - 2026-06-01

### Changed

- Upgraded target framework to .NET 10.
- Upgraded to Optimizely CMS 13 (`EPiServer.CMS.UI.Core` 13) and Optimizely Commerce 15.
- Bumped `Microsoft.Data.SqlClient` to 6.1.5.
- Replaced `Newtonsoft.Json` with `System.Text.Json` for content URL history serialization in `SqlContentUrlHistoryRepository`. `System.Text.Json` is provided transitively by Optimizely, so no explicit package reference is added.

### Fixed

- `Geta.NotFoundHandler.Optimizely` now registers its Optimizely Shell module when the package is referenced transitively (for example when only `Geta.NotFoundHandler.Optimizely.Commerce` is installed). The `module.config` copy target is now shipped under `buildTransitive`, fixing the "Unable to find a module by assembly 'Geta.NotFoundHandler.Optimizely'" error in the admin UI.

## [1.1.0]

- Refctoring

## [1.0.0]

- Initial release
