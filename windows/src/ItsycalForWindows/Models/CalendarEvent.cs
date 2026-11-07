using System;

namespace ItsycalForWindows.Models;

/// <summary>
/// One calendar event read from the local Windows appointment store. Times are kept as
/// <see cref="DateTimeOffset"/> so multi-day and all-day events keep their original
/// offsets; the UI decides how to present them.
/// </summary>
/// <param name="Subject">Event title, never null; may be empty.</param>
/// <param name="Start">Start instant.</param>
/// <param name="End">End instant (exclusive for all-day events).</param>
/// <param name="IsAllDay">True for all-day events.</param>
/// <param name="Location">Optional location text.</param>
public sealed record CalendarEvent(
    string Subject,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay = false,
    string? Location = null)
{
    /// <summary>True when the event spans more than one calendar day.</summary>
    public bool IsMultiDay =>
        DateOnly.FromDateTime(Start.LocalDateTime) != DateOnly.FromDateTime(End.LocalDateTime);

    /// <summary>Short display text used by lists and cards.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Subject) ? "(no title)" : Subject;
}
