# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- `OptimizelyNotFoundHandlerOptions.MovedContentBatchSize` (default 1000) and `NotFoundHandlerOptions.CommandTimeout` so the "Register content move redirects" job pages through large `NotFoundHandler.ContentUrlHistory` tables instead of loading every row in one unbounded query that exceeded the command timeout.
- `IContentUrlHistoryLoader.GetAllMoved(int skip, int take)` for paged access to moved content. Ships as a default interface implementation (pages in-memory over `GetAllMoved()`), so existing implementers keep compiling.

### Changed

- `SqlDataExecutor.ExecuteQuery` now propagates SQL exceptions (e.g. command timeouts) instead of swallowing them and returning an empty result, which previously surfaced as a misleading "Cannot find table 0" error.

## [1.1.0]

- Refctoring

## [1.0.0]

- Initial release
