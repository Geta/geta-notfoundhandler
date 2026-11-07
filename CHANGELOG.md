# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Security

- Wildcard and regex redirects no longer let a request choose the host of the redirect. Before, `/content` -> `/` turned `/content//evil.com` into a redirect to `//evil.com`, and the regex format `/$1` did the same, as did `$1` capturing `https://evil.com`. A format that names a host itself, such as `//cdn.example.com/$1`, is left alone, and a capture naming the host of the request stays as it is. A rule that relied on `$1` capturing another host from the request now redirects to a path on the site.
- The CSV export now escapes values a spreadsheet would run as a formula, whichever way they got into the database. A value starting with `=`, `@`, `+`, `-`, tab, carriage return or line feed is prefixed with `'`. A `;` followed by one of those characters inside a value is written as `%3B`, because spreadsheets using `;` as the list separator would otherwise start a new cell there.

### Changed

Check these when upgrading, as they change how existing rules and integrations behave:

- A redirect that would only append to itself now stops. With `/a` -> `/a/b`, `/a/x` still redirects to `/a/b/x`, but a request already under `/a/b` stays a 404. With `/a` -> `/a?x=1`, `/a?y=2` still redirects to `/a?y=2&x=1`, and that URL stays a 404. Before, these grew on every hop until the browser gave up.
- URLs under an entry in the Deleted list now return `410`. Before, `/gone/`, `/gone?a=1` and `/gone/child` were redirected, often to the home page.
- A request with no matching redirect keeps the status the application gave it. Before, a response that hadn't started yet was forced to `404`. `RequestHandler.SetStatusCodeAndShow404` is now only called for deleted URLs, so a subclass overriding it no longer runs for unmatched requests.
- `HttpContextExtensions.SetStatusCode` no longer clears the response, so a custom page keeps its headers when its status changes to `410`.
- CSV export: an imported file that was exported with escaped values keeps the `'` prefix and the `%3B` as part of the value.
- Each instance now checks the database for redirect changes every 5 minutes, which adds one query per instance per check that hashes every row of the redirects table. Set `RedirectsRefreshInterval` to `TimeSpan.Zero` to turn it off. A loader implementing `IRedirectLoader` can add `GetChecksum` to take part, and without it the check is skipped.
- A handler failing on `RedirectsEvents` no longer stops the others, and the failure now reaches the caller as an `AggregateException`.
- `OptimizelySyncEvents` takes an `ILogger<OptimizelySyncEvents>`, which matters only if you construct it yourself.

### Known issues

- With `UseInternalScheduler`, the cleanup job runs in a plain ASP.NET Core host, but it didn't fire in the Optimizely CMS 13 sample site, for reasons not yet known. On Optimizely sites, use the *[Geta NotFoundHandler] Suggestions cleanup job* scheduled job.
- Two rules pointing into each other (`/a` -> `/b/c` and `/b` -> `/a/d`) can still grow on every hop.

### Fixed

- Instances that miss an update no longer stay out of date until a restart. A check every 5 minutes picks up redirects and regex redirects that never reached an instance.
- Two reloads at the same time could leave an instance without a new redirect, when a slow load that read the table before the save finished last.
- A failed reload on the instance saving a redirect no longer stops the other instances from being told about it.
- Leaving out `UseOptimizelyNotFoundHandler` no longer silently turns off the sync between servers. The sync starts anyway and a warning is logged.
- The CMS navigation shows again on the add-on's page in the CMS on CMS 13. The layout's `<platform-navigation />` wasn't bound to its tag helper, so the browser got an empty element.
- Automatic redirects no longer fail with `Sequence contains no elements` for content with no URL history.
- A content language with no primary URL no longer stops automatic redirects for the remaining languages.
- Whitespace around old and new URLs is now removed when a redirect is saved, so a trailing space or tab in a form or an imported file no longer silently breaks it. Redirects saved earlier aren't changed: save them again to clean them up.
- The module copy on build skips unchanged files, so a running site holding the file no longer fails the build.
- Deleting a redirect from the admin redirects list (`IRedirectsService.DeleteById`) now raises `RedirectsEvents.RedirectsUpdated`, like every other write on `DefaultRedirectsService`. Before, the deleted redirect kept applying until the application restarted or another redirect was saved, and `OptimizelySyncEvents` never told the other instances.
- Redirects and `410` responses for deleted URLs now also work when the application starts writing the response body inside the pipeline. The handler now runs just before that response starts. Before, it only ran after the pipeline finished, when changing headers threw an exception. A response that hasn't started by then is still handled at that point, so `UseStatusCodePages*` registered before the handler only sees requests the handler didn't redirect. A redirect applied to a response the application is already writing still carries that body, because it can't be stopped at that point; browsers follow the `Location` header.
- When a status code page is re-executed (`UseStatusCodePagesWithReExecute`), redirect lookup and logging use the URL the client requested, not the error page's URL.
- Wildcard redirects now join the new URL and the appended path with exactly one slash. Before, the slash was chosen from the request URL instead of the new URL, so `/content` -> `/legacy` turned `/content/file/` into `/legacyfile/`, and `/content` -> `/legacy/` turned `/content/file` into `/legacy//file`.
- A wildcard redirect to `/` no longer sends an empty `Location` header when there is nothing to append (`/content` -> `/` with `/content/content`), which made browsers request the same URL again.
- `CustomRedirectCollection.Find` no longer throws `ArgumentOutOfRangeException` when the request repeats the old URL after extra slashes (`/content/` with `/content//content`) or when `/` is the old URL and the request is `/?param=value`.
- The guard against redirects building up now also applies when the old URL ends in a slash, so `/content/` -> `/legacy` turns `/content/content/file` into `/legacy/file`, the same as `/content` -> `/legacy` already did.
- Setting `UseInternalScheduler` no longer crashes the application at startup with `No service for type 'Microsoft.Extensions.Logging.ILogger'`.

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
