using CodexSwitch.Models;

namespace CodexSwitch.Proxy;

public enum ProviderCircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public sealed record ProviderCircuitBreakerAttempt(
    bool CanAttempt,
    ProviderCircuitBreakerState State,
    DateTimeOffset? NextAttemptAt,
    int RecoveryAttempt,
    bool IsProbe);

public sealed class ProviderCircuitBreakerRegistry
{
    private sealed class Entry
    {
        public ProviderCircuitBreakerState State { get; set; } = ProviderCircuitBreakerState.Closed;

        public int ConsecutiveFailures { get; set; }

        public int RecoveryAttempt { get; set; }

        public DateTimeOffset NextAttemptAt { get; set; }

        public bool ProbeInFlight { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<DateTimeOffset> _now;

    public ProviderCircuitBreakerRegistry(Func<DateTimeOffset>? now = null)
    {
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public ProviderCircuitBreakerAttempt Evaluate(string providerId, ResilienceSettings settings)
    {
        if (!settings.CircuitBreakerEnabled)
            return new ProviderCircuitBreakerAttempt(true, ProviderCircuitBreakerState.Closed, null, 0, false);

        lock (_sync)
        {
            if (!_entries.TryGetValue(providerId, out var entry))
                return new ProviderCircuitBreakerAttempt(true, ProviderCircuitBreakerState.Closed, null, 0, false);

            if (entry.State == ProviderCircuitBreakerState.Closed)
                return new ProviderCircuitBreakerAttempt(true, entry.State, null, 0, false);

            var now = _now();
            if (entry.State == ProviderCircuitBreakerState.Open && now >= entry.NextAttemptAt)
            {
                entry.State = ProviderCircuitBreakerState.HalfOpen;
                entry.ProbeInFlight = true;
                return new ProviderCircuitBreakerAttempt(true, entry.State, entry.NextAttemptAt, entry.RecoveryAttempt + 1, true);
            }

            if (entry.State == ProviderCircuitBreakerState.HalfOpen && !entry.ProbeInFlight)
            {
                entry.ProbeInFlight = true;
                return new ProviderCircuitBreakerAttempt(true, entry.State, entry.NextAttemptAt, entry.RecoveryAttempt + 1, true);
            }

            return new ProviderCircuitBreakerAttempt(false, entry.State, entry.NextAttemptAt, entry.RecoveryAttempt + 1, false);
        }
    }

    public void ReportSuccess(string providerId, ResilienceSettings settings)
    {
        if (!settings.CircuitBreakerEnabled)
            return;

        lock (_sync)
        {
            _entries.Remove(providerId);
        }
    }

    public void ReportFailure(string providerId, ResilienceSettings settings)
    {
        if (!settings.CircuitBreakerEnabled)
            return;

        var failureThreshold = settings.CircuitBreakerFailureThreshold <= 0
            ? 3
            : settings.CircuitBreakerFailureThreshold;
        var delays = NormalizeRecoveryDelays(settings);

        lock (_sync)
        {
            var now = _now();
            if (!_entries.TryGetValue(providerId, out var entry))
            {
                entry = new Entry();
                _entries[providerId] = entry;
            }

            entry.ProbeInFlight = false;
            entry.ConsecutiveFailures++;
            if (entry.State == ProviderCircuitBreakerState.HalfOpen)
            {
                entry.RecoveryAttempt = Math.Min(entry.RecoveryAttempt + 1, delays.Length - 1);
                Open(entry, now, delays);
                return;
            }

            if (entry.ConsecutiveFailures >= failureThreshold)
            {
                if (entry.State == ProviderCircuitBreakerState.Closed)
                    entry.RecoveryAttempt = 0;
                Open(entry, now, delays);
            }
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    private static void Open(Entry entry, DateTimeOffset now, int[] delays)
    {
        entry.State = ProviderCircuitBreakerState.Open;
        entry.NextAttemptAt = now + TimeSpan.FromSeconds(delays[entry.RecoveryAttempt]);
    }

    private static int[] NormalizeRecoveryDelays(ResilienceSettings settings)
    {
        var delays = settings.CircuitBreakerRecoveryDelaySeconds?
            .Where(delay => delay > 0)
            .Take(5)
            .ToArray();
        return delays is { Length: > 0 } ? delays : [5, 15, 30, 60, 120];
    }
}
