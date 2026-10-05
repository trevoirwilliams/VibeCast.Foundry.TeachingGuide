---
applyTo: "src/VibeCast.Web/**/*.razor,src/VibeCast.Web/**/*.razor.cs"
---

# Blazor UI Instructions

- Components represent user interaction and presentation state.
- Do not construct provider SDK clients in Razor components.
- Do not place prompt templates, raw model calls, retrieval, retries, JSON repair,
  tool dispatch, or persistence orchestration in components.
- Inject application-owned services.
- Preserve `[Authorize]` on authenticated routes.
- Revalidate resource ownership server-side; hidden or disabled controls are not
  authorization.
- Handle cancellation and disconnected clients for streamed operations.
- Show explicit loading, empty, refused, failed, cancelled, and insufficient-evidence
  states.
- Never render model-produced markup as trusted HTML.
- Preserve the current visual system unless the task is explicitly a UI redesign.
- Do not make placeholder surfaces appear operational before their implementation
  lesson.