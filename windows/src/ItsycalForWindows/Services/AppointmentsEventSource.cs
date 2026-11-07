using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ItsycalForWindows.Models;

namespace ItsycalForWindows.Services;

/// <summary>
/// Reads events from the local Windows appointment store through the read-only
/// <c>Windows.ApplicationModel.Appointments</c> API.
/// </summary>
/// <remarks>
/// Implementation notes for the owner of the events feature:
/// <list type="bullet">
/// <item>Request a read-only store: <c>AppointmentManager.RequestStoreAsync(
/// AppointmentStoreAccessType.AllCalendarsReadOnly)</c>.</item>
/// <item>Query with <c>FindAppointmentsAsync(start, duration, options)</c> using the
/// visible range; request recurrence expansion so recurring events appear per day.</item>
/// <item>Map <c>Appointment</c> to <see cref="CalendarEvent"/>; all-day events report
/// <c>AllDay</c> and a midnight start.</item>
/// <item>Return an empty list for any access failure; never throw. The UI must keep
/// working when the calendar is unavailable or not configured.</item>
/// <item>The packaged manifest declares the <c>appointments</c> capability; an unpackaged
/// debug run therefore shows no events by design.</item>
/// </list>
/// </remarks>
internal sealed class AppointmentsEventSource : IEventSource
{
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateOnly fromInclusive,
        DateOnly toExclusive,
        CancellationToken cancellationToken = default)
    {
        // Placeholder until the events feature lands (branch feat/calendar-events).
        // Contract: never throw; return an empty list when the store is unavailable.
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(Array.Empty<CalendarEvent>());
    }
}
