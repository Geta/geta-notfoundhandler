# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed

- Deleting a redirect from the admin redirects list (`IRedirectsService.DeleteById`) now raises `RedirectsEvents.RedirectsUpdated`, like every other write on `DefaultRedirectsService`. Before, the deleted redirect kept applying until the application restarted or another redirect was saved, and `OptimizelySyncEvents` never told the other instances.
- Redirects and `410` responses for deleted URLs now also work when the application has already started writing the response body. The handler runs just before the response starts instead of after the pipeline finishes, when changing headers threw an exception.
- A request whose status is in `ActiveStatusCodes` but has no matching redirect now keeps its status. Before, a response that hadn't started yet was forced to `404`, which broke sites using `ActiveStatusCodes` with `200`.
- When a status code page is re-executed (`UseStatusCodePagesWithReExecute`), redirect lookup and logging use the URL the client requested, not the error page's URL.

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
