using TrayMedia.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TrayMedia.Services;

public sealed class SmtcService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private CancellationTokenSource? _pollCts;
    private string? _lastArtKey;
    private byte[]? _lastArtBytes;
    private MediaPlaybackState _state = MediaPlaybackState.Empty;
    private bool _disposed;

    public event EventHandler<MediaPlaybackState>? StateChanged;

    public MediaPlaybackState Current => _state;

    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnManagerChanged;
        _manager.SessionsChanged += OnManagerChanged;
        await AttachSessionAsync(_manager.GetCurrentSession());
        await PushStateAsync();
        StartPolling();
    }

    public Task PlayAsync() => TryCommandAsync(s => s.TryPlayAsync().AsTask());
    public Task PauseAsync() => TryCommandAsync(s => s.TryPauseAsync().AsTask());
    public Task ToggleAsync() => TryCommandAsync(s => s.TryTogglePlayPauseAsync().AsTask());
    public Task NextAsync() => TryCommandAsync(s => s.TrySkipNextAsync().AsTask());
    public Task PreviousAsync() => TryCommandAsync(s => s.TrySkipPreviousAsync().AsTask());

    public async Task SeekAsync(double positionSec)
    {
        var session = _manager?.GetCurrentSession();
        if (session is null)
        {
            await PushStateAsync();
            return;
        }

        try
        {
            var timeline = session.GetTimelineProperties();
            var target = TimeSpan.FromSeconds(Math.Max(0, positionSec));
            if (timeline.EndTime > TimeSpan.Zero && target > timeline.EndTime)
            {
                target = timeline.EndTime;
            }

            await session.TryChangePlaybackPositionAsync(target.Ticks);
        }
        catch
        {
            // ignore transient SMTC failures
        }

        await Task.Delay(80);
        await PushStateAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        if (_manager is not null)
        {
            _manager.CurrentSessionChanged -= OnManagerChanged;
            _manager.SessionsChanged -= OnManagerChanged;
        }

        DetachSession();
    }

    private void StartPolling()
    {
        _pollCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!_pollCts.IsCancellationRequested)
            {
                try
                {
                    await PushStateAsync();
                }
                catch
                {
                    // ignore transient SMTC failures
                }

                try
                {
                    await Task.Delay(500, _pollCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
    }

    private void OnManagerChanged(GlobalSystemMediaTransportControlsSessionManager sender, object args) =>
        _ = PushStateAsync();

    private async Task TryCommandAsync(Func<GlobalSystemMediaTransportControlsSession, Task> action)
    {
        var session = _manager?.GetCurrentSession();
        if (session is null)
        {
            await PushStateAsync();
            return;
        }

        try
        {
            await action(session);
        }
        catch
        {
            // ignore transient SMTC failures
        }

        await Task.Delay(80);
        await PushStateAsync();
    }

    private async Task AttachSessionAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        DetachSession();
        _session = session;
        _lastArtKey = null;
        _lastArtBytes = null;
        if (_session is null) return;

        _session.MediaPropertiesChanged += OnSessionChanged;
        _session.PlaybackInfoChanged += OnSessionChanged;
        _session.TimelinePropertiesChanged += OnSessionChanged;
        await Task.CompletedTask;
    }

    private void DetachSession()
    {
        if (_session is null) return;
        _session.MediaPropertiesChanged -= OnSessionChanged;
        _session.PlaybackInfoChanged -= OnSessionChanged;
        _session.TimelinePropertiesChanged -= OnSessionChanged;
        _session = null;
    }

    private void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args) =>
        _ = PushStateAsync();

    private async Task PushStateAsync()
    {
        if (_manager is null || _disposed) return;

        var session = _manager.GetCurrentSession();
        if (!ReferenceEquals(session, _session))
        {
            await AttachSessionAsync(session);
        }

        if (session is null)
        {
            ApplyState(MediaPlaybackState.Empty);
            return;
        }

        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();
        var props = await session.TryGetMediaPropertiesAsync();

        var title = props?.Title?.Trim() ?? "";
        var artist = props?.Artist?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(artist))
        {
            artist = props?.AlbumArtist?.Trim() ?? "";
        }

        var appId = session.SourceAppUserModelId ?? "";
        var artKey = $"{appId}|{title}|{artist}|{props?.TrackNumber}|{props?.AlbumTitle}";
        byte[]? albumArt = _lastArtBytes;
        if (artKey != _lastArtKey)
        {
            albumArt = await ReadThumbnailBytesAsync(props?.Thumbnail);
            _lastArtKey = artKey;
            _lastArtBytes = albumArt;
        }

        var start = timeline.StartTime.TotalSeconds;
        var end = timeline.EndTime.TotalSeconds;
        var position = Math.Max(0, timeline.Position.TotalSeconds - start);
        var duration = Math.Max(0, end - start);
        if (duration <= 0 && timeline.MaxSeekTime > timeline.MinSeekTime)
        {
            duration = (timeline.MaxSeekTime - timeline.MinSeekTime).TotalSeconds;
        }

        var isPlaying = playback.PlaybackStatus ==
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        var controls = playback.Controls;
        var empty = string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist);

        ApplyState(new MediaPlaybackState
        {
            IsEmpty = empty,
            Title = empty
                ? MediaPlaybackState.Empty.Title
                : (string.IsNullOrWhiteSpace(title) ? "Unknown track" : title),
            Artist = empty
                ? MediaPlaybackState.Empty.Artist
                : (string.IsNullOrWhiteSpace(artist) ? PrettyAppName(appId) : artist),
            AlbumArtBytes = empty ? null : albumArt,
            AlbumArtKey = empty ? "" : artKey,
            IsPlaying = isPlaying,
            PositionSec = position,
            DurationSec = duration,
            AppId = appId,
            CanPlay = controls.IsPlayEnabled,
            CanPause = controls.IsPauseEnabled,
            CanNext = controls.IsNextEnabled,
            CanPrevious = controls.IsPreviousEnabled,
            CanSeek = controls.IsPlaybackPositionEnabled,
        });
    }

    private void ApplyState(MediaPlaybackState next)
    {
        if (IsSameUiState(_state, next))
        {
            _state = next;
            return;
        }

        _state = next;
        StateChanged?.Invoke(this, next);
    }

    private static bool IsSameUiState(MediaPlaybackState a, MediaPlaybackState b)
    {
        return a.IsEmpty == b.IsEmpty
               && a.IsPlaying == b.IsPlaying
               && a.CanPlay == b.CanPlay
               && a.CanPause == b.CanPause
               && a.CanNext == b.CanNext
               && a.CanPrevious == b.CanPrevious
               && a.CanSeek == b.CanSeek
               && a.AlbumArtKey == b.AlbumArtKey
               && a.Title == b.Title
               && a.Artist == b.Artist
               && a.AppId == b.AppId
               && (int)a.PositionSec == (int)b.PositionSec
               && (int)a.DurationSec == (int)b.DurationSec;
    }

    private static async Task<byte[]?> ReadThumbnailBytesAsync(IRandomAccessStreamReference? thumb)
    {
        if (thumb is null) return null;
        try
        {
            using var stream = await thumb.OpenReadAsync();
            var size = (uint)stream.Size;
            if (size == 0 || size > 5_000_000) return null;

            var reader = new DataReader(stream);
            await reader.LoadAsync(size);
            var bytes = new byte[size];
            reader.ReadBytes(bytes);
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    private static string PrettyAppName(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId)) return "Media";
        var leaf = appId.Split('!').FirstOrDefault() ?? appId;
        leaf = leaf.Split('.').LastOrDefault() ?? leaf;
        return leaf;
    }
}
