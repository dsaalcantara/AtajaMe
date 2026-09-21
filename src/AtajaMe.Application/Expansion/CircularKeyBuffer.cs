namespace AtajaMe.Application.Expansion;

/// <summary>
/// Búfer efímero circular en memoria para capturar pulsaciones recientes de teclado.
/// Cumple con las directrices de privacidad estricta (RF-08 y RF-46):
/// - Capacidad fija en memoria (por defecto 50 caracteres).
/// - Acceso seguro para subprocesos (thread-safe).
/// - Purga automática por inactividad (por defecto 5 segundos).
/// - Ningún almacenamiento ni persistencia en disco de teclas tecleadas.
/// </summary>
public sealed class CircularKeyBuffer : ICircularKeyBuffer
{
    public const int DefaultCapacity = 50;
    public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(5);

    private readonly object _syncLock = new();
    private readonly char[] _buffer;
    private readonly int _maxCapacity;
    private readonly TimeSpan _idleTimeout;
    private readonly Timer _purgeTimer;

    private int _start;
    private int _count;
    private bool _isDisposed;

    public int MaxCapacity => _maxCapacity;
    public TimeSpan IdleTimeout => _idleTimeout;

    public int Length
    {
        get
        {
            lock (_syncLock)
            {
                return _count;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_syncLock)
            {
                return _count == 0;
            }
        }
    }

    public event EventHandler? BufferPurged;

    public CircularKeyBuffer(int maxCapacity = DefaultCapacity, TimeSpan? idleTimeout = null)
    {
        if (maxCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "La capacidad del búfer debe ser mayor que cero.");
        }

        _maxCapacity = maxCapacity;
        _idleTimeout = idleTimeout ?? DefaultIdleTimeout;
        _buffer = new char[_maxCapacity];

        _purgeTimer = new Timer(OnTimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Append(char c)
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();

            if (_count < _maxCapacity)
            {
                var index = (_start + _count) % _maxCapacity;
                _buffer[index] = c;
                _count++;
            }
            else
            {
                _buffer[_start] = c;
                _start = (_start + 1) % _maxCapacity;
            }

            ResetTimer();
        }
    }

    public bool HandleBackspace()
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();

            if (_count == 0)
            {
                return false;
            }

            _count--;
            ResetTimer();
            return true;
        }
    }

    public string GetContent()
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();

            if (_count == 0)
            {
                return string.Empty;
            }

            var result = new char[_count];
            for (var i = 0; i < _count; i++)
            {
                result[i] = _buffer[(_start + i) % _maxCapacity];
            }

            return new string(result);
        }
    }

    public void Clear()
    {
        bool hadContent;
        lock (_syncLock)
        {
            ThrowIfDisposed();

            hadContent = _count > 0;
            _start = 0;
            _count = 0;
            StopTimer();
        }

        if (hadContent)
        {
            NotifyPurged();
        }
    }

    private void ResetTimer()
    {
        if (_idleTimeout > TimeSpan.Zero)
        {
            _purgeTimer.Change(_idleTimeout, Timeout.InfiniteTimeSpan);
        }
    }

    private void StopTimer()
    {
        _purgeTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed(object? state)
    {
        bool hadContent;
        lock (_syncLock)
        {
            if (_isDisposed || _count == 0)
            {
                return;
            }

            hadContent = _count > 0;
            _start = 0;
            _count = 0;
            StopTimer();
        }

        if (hadContent)
        {
            NotifyPurged();
        }
    }

    private void NotifyPurged()
    {
        try
        {
            BufferPurged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Evitar que excepciones de observadores corrompan el ciclo de vida del búfer
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CircularKeyBuffer));
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _count = 0;
            _start = 0;
            _purgeTimer.Dispose();
        }
    }
}
