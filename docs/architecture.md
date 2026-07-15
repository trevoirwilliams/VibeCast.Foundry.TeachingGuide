# Starter Architecture

```text
Browser
  |
  v
VibeCast.Web (Blazor Interactive Server + Identity)
  |-- forms, navigation, authorization, upload UI
  |-- OpenTelemetry registration
  |
  +--> VibeCast.Application
  |      |-- storage and background-job contracts
  |      |-- request models and validators
  |      `-- future AI integration seams
  |
  +--> VibeCast.Infrastructure
         |-- EF Core + SQLite + Identity stores
         |-- local blob storage
         |-- channel job queue + hosted worker
         `-- seed data and migrations

VibeCast.Domain
  `-- Episode, MediaAsset, ProcessingJob, UserProfile
```

The UI and persistence plumbing are intentionally complete. AI implementations must enter through application-owned interfaces instead of being embedded directly in Razor components.
