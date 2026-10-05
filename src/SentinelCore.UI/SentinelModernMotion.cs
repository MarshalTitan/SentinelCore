namespace SentinelCore.UI;

/// <summary>
/// Lightweight per-window motion state. Consumers should retain one instance with their window.
/// All motion is decorative and becomes deterministic/immediate when reduced motion is enabled.
/// </summary>
public sealed class SentinelModernMotion : IDisposable
{
    private const int PruneIntervalFrames = 300;
    private const int StaleAfterFrames = 900;
    private const float MaximumDeltaTime = 0.1f;

    private readonly Dictionary<int, Channel> channels = new();
    private readonly List<int> staleKeys = new();
    private int frame;
    private float deltaTime = 1f / 60f;
    private float elapsedSeconds;
    private bool reducedMotion;
    private bool disposed;

    public bool ReducedMotion => reducedMotion;

    public float ElapsedSeconds => elapsedSeconds;

    public int ActiveChannelCount => channels.Count;

    public void BeginFrame(float frameDeltaTime, bool useReducedMotion)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!float.IsFinite(frameDeltaTime) || frameDeltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(frameDeltaTime));

        frame++;
        deltaTime = Math.Clamp(frameDeltaTime, 0f, MaximumDeltaTime);
        reducedMotion = useReducedMotion;
        if (!reducedMotion)
            elapsedSeconds += deltaTime;

        if (frame % PruneIntervalFrames == 0)
            PruneStaleChannels();
    }

    public float Approach(string key, float target, float response = 18f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Approach(StringComparer.Ordinal.GetHashCode(key), target, response);
    }

    public float Approach(int key, float target, float response = 18f)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateFinite(target, nameof(target));
        ValidatePositive(response, nameof(response));

        if (!channels.TryGetValue(key, out var channel))
            channel = new Channel(target, frame);

        var next = Smooth(channel.Value, target, deltaTime, response, reducedMotion);
        channels[key] = new Channel(next, frame);
        return next;
    }

    public float Hover(string key, bool hovered, float response = 20f)
        => Approach(key, hovered ? 1f : 0f, response);

    public float Hover(int key, bool hovered, float response = 20f)
        => Approach(key, hovered ? 1f : 0f, response);

    public float Wave(float periodSeconds, float phaseRadians = 0f)
    {
        ValidatePositive(periodSeconds, nameof(periodSeconds));
        ValidateFinite(phaseRadians, nameof(phaseRadians));
        var time = reducedMotion ? 0f : elapsedSeconds;
        return MathF.Sin(((time / periodSeconds) * MathF.Tau) + phaseRadians);
    }

    public float Pulse(float periodSeconds = 1.8f)
        => reducedMotion
            ? 0.5f
            : 0.5f + (0.5f * Wave(periodSeconds, -MathF.PI / 2f));

    public static float Smooth(
        float current,
        float target,
        float deltaTime,
        float response,
        bool reducedMotion)
    {
        ValidateFinite(current, nameof(current));
        ValidateFinite(target, nameof(target));
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        ValidatePositive(response, nameof(response));

        if (reducedMotion)
            return target;
        if (deltaTime <= 0f)
            return current;

        var boundedDelta = MathF.Min(deltaTime, MaximumDeltaTime);
        var next = current + ((target - current) * (1f - MathF.Exp(-response * boundedDelta)));
        return MathF.Abs(next - target) < 0.0005f ? target : next;
    }

    public static float AdvanceProgress(
        float current,
        float deltaTime,
        float durationSeconds,
        bool reducedMotion)
    {
        if (!float.IsFinite(current) || current < 0f || current > 1f)
            throw new ArgumentOutOfRangeException(nameof(current));
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        ValidatePositive(durationSeconds, nameof(durationSeconds));
        return reducedMotion ? 1f : Math.Clamp(current + (deltaTime / durationSeconds), 0f, 1f);
    }

    public static float EaseOutCubic(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        var inverse = 1f - value;
        return 1f - (inverse * inverse * inverse);
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        channels.Clear();
        staleKeys.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        channels.Clear();
        staleKeys.Clear();
        disposed = true;
    }

    private void PruneStaleChannels()
    {
        staleKeys.Clear();
        foreach (var pair in channels)
        {
            if (frame - pair.Value.LastSeenFrame > StaleAfterFrames)
                staleKeys.Add(pair.Key);
        }

        foreach (var key in staleKeys)
            channels.Remove(key);
        staleKeys.Clear();
    }

    private static void ValidateFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private readonly record struct Channel(float Value, int LastSeenFrame);
}

/// <summary>
/// Persistent state for one <see cref="SentinelModernAppShell"/> instance.
/// </summary>
public sealed class SentinelModernAppShellState : IDisposable
{
    private const float PageTransitionDurationSeconds = 0.24f;
    private string? selectedPageId;
    private float pageProgress = 1f;
    private bool disposed;

    public SentinelModernMotion Motion { get; } = new();

    public float PageRevealProgress => SentinelModernMotion.EaseOutCubic(pageProgress);

    internal void BeginFrame(string pageId, float deltaTime, bool reducedMotion)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
        Motion.BeginFrame(deltaTime, reducedMotion);

        if (!string.Equals(selectedPageId, pageId, StringComparison.Ordinal))
        {
            selectedPageId = pageId;
            pageProgress = reducedMotion ? 1f : 0f;
        }

        pageProgress = SentinelModernMotion.AdvanceProgress(
            pageProgress,
            Math.Clamp(deltaTime, 0f, 0.1f),
            PageTransitionDurationSeconds,
            reducedMotion);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        Motion.Dispose();
        disposed = true;
    }
}
