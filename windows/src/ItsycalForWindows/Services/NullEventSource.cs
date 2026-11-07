using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ItsycalForWindows.Models;

namespace ItsycalForWindows.Services;

/// <summary>
/// Event source used when the appointment store is unavailable (unpackaged run, missing
/// capability, or a host without calendar access). Always returns no events.
/// </summary>
internal sealed class NullEventSource : IEventSource
{
    public static NullEventSource Instance { get; } = new();

    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateOnly fromInclusive,
        DateOnly toExclusive,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CalendarEvent>>(Array.Empty<CalendarEvent>());
}
