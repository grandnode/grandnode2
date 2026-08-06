# Scheduled Task

## Purpose
Create, modify, and review GrandNode scheduled tasks including the task class, DI registration, database seed, distributed-lock behavior, error handling, and task migrations.

## When To Use
Use this skill when adding a new background task, changing an existing task's logic or interval, migrating a task name or seed entry for existing installations, or reviewing scheduled task correctness, idempotency, and multi-instance safety.

## When Not To Use
Do not use this skill for message template content or email sending logic; combine with `message-notification` when a task sends emails.

Do not use this skill as the primary review for MongoDB query safety or security; combine with the relevant skill when those concerns apply.

## Inputs Required
- Repository root.
- Task purpose and trigger description.
- Required time interval (minutes).
- Whether the task should be enabled by default.
- Domain entities the task reads or writes.
- Whether the task must respect per-store or per-language context.

## Instructions

### Mandatory Rules

#### Task Class
1. Implement `IScheduleTask` from `Grand.Business.Core.Interfaces.System.ScheduleTasks` with a single `Task Execute()` method.
2. Place the task class in `src/Modules/Grand.Module.ScheduledTasks/BackgroundServices/`.
3. Resolve all dependencies through constructor injection. `Execute()` takes no parameters and receives no `CancellationToken` from the caller.
4. Keep `Execute()` idempotent: running it twice in a row must not corrupt data or duplicate side effects.
5. For tasks that process a collection of items, wrap each item in its own `try-catch` block. Log the error and continue with the next item; do not let a single-item failure abort the whole run.
6. Never call `Thread.Sleep`, `Task.Delay`, or any other wait inside `Execute()`. The runner controls the interval.

#### DI Registration
7. Register the task in `src/Modules/Grand.Module.ScheduledTasks/Startup/StartupApplication.cs` using exactly:
   ```csharp
   serviceCollection.AddKeyedScoped<IScheduleTask, YourTask>("Task name string");
   ```
8. The service key string **must exactly match** the `ScheduleTaskName` stored in the database. Any mismatch means the runner cannot resolve the task.

#### Database Seed
9. Add a `ScheduleTask` entry in `src/Modules/Grand.Module.Installer/Services/InstallDataScheduleTasks.cs` inside `InstallScheduleTasks()`:
   ```csharp
   new ScheduleTask {
       ScheduleTaskName = "Task name string",   // must equal the DI key
       Enabled = false,                          // prefer disabled by default
       StopOnError = false,
       TimeInterval = N                          // minutes
   }
   ```
10. Prefer `Enabled = false` for new tasks so the administrator explicitly activates them. Only set `Enabled = true` for tasks that must run immediately after installation (e.g., `"Send emails"`).
11. Leave `StopOnError = false` unless the task manages a resource that is unsafe to run after a partial failure.

#### Existing Installations — Migrations
12. Add a migration under `src/Modules/Grand.Module.Migration/Migrations/` when changing the `ScheduleTaskName`, default `TimeInterval`, or `Enabled` value for a task that already exists in production. Follow the `IMigration` interface and existing migration class patterns.
13. In a migration that adds a new task, insert the new `ScheduleTask` only when it does not already exist (check by `ScheduleTaskName` before inserting).
14. In a migration that removes a task, delete by `ScheduleTaskName` and remove the DI registration.

#### Error Handling
15. The runner sets `task.Enabled = false` when `StopOnError = true` and `Execute()` throws an uncaught exception. Tasks with `StopOnError = false` remain enabled and retry on the next interval.
16. Log unexpected exceptions with `ILogger<T>`. Do not swallow them silently — the runner also logs at error level, but task-level context is valuable.
17. For partial-failure loops, update the item's state inside a `finally` block so the next run does not re-process an item that was already attempted.

#### Multi-Instance Safety
18. The runner uses `TryClaimTaskRun()` — an atomic compare-and-set on `LastStartUtc` — to ensure only one instance executes a given task per interval window. Task classes do not need to implement their own locking.
19. Do not cache expensive results across `Execute()` calls using instance fields. The task is registered as `AddKeyedScoped`, so a new instance is created for each execution.

#### Store and Language Context
20. When a task must operate per store, inject `IStoreService` and iterate over `GetAllStores()`. The `ScheduleTask.StoreId` field is available if the task should be restricted to a single store.
21. When locale-sensitive output is required, inject `ILanguageService` and iterate over languages for the relevant store.

### Recommendations
1. Prefer reading a bounded batch of records (e.g., top 500) rather than loading an unbounded collection in one query.
2. Prefer a short `TimeInterval` for operational tasks (email sending = 1 min) and a long interval for maintenance tasks (sitemap generation = 10080 min = 7 days).
3. Prefer injecting `ILogger<T>` for all non-trivial tasks to support observability.
4. Prefer structured log messages that include the task name and record ID when processing per-item.

## Constraints
- Never place task classes outside `src/Modules/Grand.Module.ScheduledTasks/BackgroundServices/`.
- Never register a task with a DI key that differs from its `ScheduleTaskName` in the database.
- Never add a seed entry without also adding a DI registration, and vice versa.
- Never introduce locking, semaphores, or static shared state inside a task class.
- Never call `IEmailSender` directly from a task; use `IMessageProviderService` or queue via `IQueuedEmailService` following the message-notification pattern.

## Key Contracts

### IScheduleTask (`src/Business/Grand.Business.Core/Interfaces/System/ScheduleTasks/IScheduleTask.cs`)
```csharp
public interface IScheduleTask
{
    Task Execute();
}
```

### ScheduleTask entity (`src/Core/Grand.Domain/Tasks/ScheduleTask.cs`)
```csharp
public class ScheduleTask : BaseEntity
{
    public string ScheduleTaskName { get; set; }   // DI key and display name
    public bool   Enabled          { get; set; }
    public bool   StopOnError      { get; set; }
    public int    TimeInterval     { get; set; }   // minutes
    public string StoreId          { get; set; }   // empty = all stores
    public DateTime? LastStartUtc       { get; set; }
    public DateTime? LastSuccessUtc     { get; set; }
    public DateTime? LastNonSuccessEndUtc { get; set; }
    public string LeasedByInstance { get; set; }   // multi-instance lock holder
}
```

## File Locations

| Concern | Path |
|---|---|
| IScheduleTask interface | `src/Business/Grand.Business.Core/Interfaces/System/ScheduleTasks/IScheduleTask.cs` |
| Task classes | `src/Modules/Grand.Module.ScheduledTasks/BackgroundServices/` |
| DI registration | `src/Modules/Grand.Module.ScheduledTasks/Startup/StartupApplication.cs` |
| ScheduleTask entity | `src/Core/Grand.Domain/Tasks/ScheduleTask.cs` |
| Installer seed | `src/Modules/Grand.Module.Installer/Services/InstallDataScheduleTasks.cs` |
| Task runner (BackgroundService) | `src/Web/Grand.Web.Common/Infrastructure/BackgroundServiceTask.cs` |
| Task handler startup | `src/Web/Grand.Web.Common/Startup/TaskHandler.cs` |
| IScheduleTaskService | `src/Business/Grand.Business.Core/Interfaces/System/ScheduleTasks/IScheduleTaskService.cs` |
| Migration example | `src/Modules/Grand.Module.Migration/Migrations/2.1/MigrationScheduleTasks.cs` |
| Tests — ScheduleTaskService | `src/Tests/Grand.Modules.Tests/Services/BackgroundService/ScheduleTaskServiceTests.cs` |
| Tests — BackgroundServiceTask | `src/Tests/Grand.Web.Common.Tests/Infrastructure/BackgroundServiceTaskTests.cs` |

## Existing Task Inventory

| Class | DI key / ScheduleTaskName | Default interval | Default enabled |
|---|---|---|---|
| `QueuedMessagesSendScheduleTask` | `"Send emails"` | 1 min | true |
| `DeleteGuestsScheduleTask` | `"Delete guests"` | 1440 min | true |
| `UpdateExchangeRateScheduleTask` | `"Update currency exchange rates"` | 1440 min | true |
| `ClearCacheScheduleTask` | `"Clear cache"` | 120 min | false |
| `GenerateSitemapXmlTask` | `"Generate sitemap XML file"` | 10080 min | false |
| `EndAuctionsTask` | `"End of the auctions"` | 60 min | false |
| `CancelOrderScheduledTask` | `"Cancel unpaid and pending orders"` | 1440 min | false |

## Expected Output
Produce one of these results:
- A new task class, DI registration, installer seed, and (when needed) migration.
- An updated task class with matching DI key, seed, and migration for existing installations.
- A review report listing task correctness, idempotency, or registration issues.

Include changed files, DI key, seed values, migration status, and remaining risks.

## Validation Checklist
- [ ] Task class implements `IScheduleTask` and lives in `BackgroundServices/`.
- [ ] `Execute()` is idempotent and does not depend on execution order.
- [ ] Per-item loops use individual `try-catch` blocks.
- [ ] DI key in `AddKeyedScoped` exactly matches the `ScheduleTaskName` seed value.
- [ ] Installer seed entry added with appropriate `Enabled` and `TimeInterval`.
- [ ] Migration added for tasks that affect existing installations.
- [ ] No static shared state or cross-call caching inside the task class.
- [ ] `ILogger<T>` used for error and diagnostic output.
- [ ] Tests cover `Execute()` happy path and error-per-item behavior where applicable.

## Examples

### Example 1: Simple Maintenance Task
Input: Add a task that deletes expired product reservations every 60 minutes.

Output:
1. Create `DeleteExpiredReservationsTask : IScheduleTask` in `BackgroundServices/`.
2. Inject `IProductReservationService` and `ILogger<DeleteExpiredReservationsTask>`.
3. In `Execute()`: query expired reservations, delete each inside a `try-catch`, log errors and continue.
4. Register: `AddKeyedScoped<IScheduleTask, DeleteExpiredReservationsTask>("Delete expired reservations")`.
5. Seed: `new ScheduleTask { ScheduleTaskName = "Delete expired reservations", Enabled = false, StopOnError = false, TimeInterval = 60 }`.

### Example 2: Per-Store Task
Input: Add a task that recalculates price tiers per store daily.

Output:
1. Create `RecalculatePriceTiersTask : IScheduleTask`.
2. Inject `IStoreService` and `IPricingService`.
3. In `Execute()`: call `_storeService.GetAllStores()`, loop each store, call `_pricingService.RecalculateTiers(store.Id)` wrapped in per-store `try-catch`.
4. Register and seed with `TimeInterval = 1440`.

### Example 3: Renaming a Task in Migration
Input: The task `"Recalculate price tiers"` needs to be renamed to `"Recalculate product pricing"`.

Output:
1. Update the DI key in `StartupApplication.cs` to `"Recalculate product pricing"`.
2. Add a migration class implementing `IMigration` that finds the `ScheduleTask` by the old `ScheduleTaskName` and updates it to the new name.
3. If no existing task is found, insert a new seed entry so the task exists on installations that never had the old name.
