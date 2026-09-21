using AtajaMe.Application.Expansion;

namespace AtajaMe.Tests.Expansion;

public class CircularKeyBufferTests
{
    [Fact]
    public void InitialState_IsEmpty()
    {
        using var buffer = new CircularKeyBuffer(50);

        Assert.True(buffer.IsEmpty);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(string.Empty, buffer.GetContent());
        Assert.Equal(50, buffer.MaxCapacity);
    }

    [Fact]
    public void Append_AddsCharactersInOrder()
    {
        using var buffer = new CircularKeyBuffer(50);

        buffer.Append('h');
        buffer.Append('o');
        buffer.Append('l');
        buffer.Append('a');

        Assert.False(buffer.IsEmpty);
        Assert.Equal(4, buffer.Length);
        Assert.Equal("hola", buffer.GetContent());
    }

    [Fact]
    public void Append_ExceedingCapacity_DiscardsOldestCharacters()
    {
        using var buffer = new CircularKeyBuffer(5);

        foreach (var c in "abcdefgh")
        {
            buffer.Append(c);
        }

        Assert.Equal(5, buffer.Length);
        Assert.Equal("defgh", buffer.GetContent());
    }

    [Fact]
    public void HandleBackspace_RemovesLastCharacter()
    {
        using var buffer = new CircularKeyBuffer(50);

        buffer.Append('a');
        buffer.Append('b');
        buffer.Append('c');

        var result = buffer.HandleBackspace();

        Assert.True(result);
        Assert.Equal(2, buffer.Length);
        Assert.Equal("ab", buffer.GetContent());
    }

    [Fact]
    public void HandleBackspace_OnEmptyBuffer_ReturnsFalse()
    {
        using var buffer = new CircularKeyBuffer(50);

        var result = buffer.HandleBackspace();

        Assert.False(result);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(string.Empty, buffer.GetContent());
    }

    [Fact]
    public void Clear_EmptiesBufferAndFiresPurgedEvent()
    {
        using var buffer = new CircularKeyBuffer(50);
        buffer.Append('x');
        buffer.Append('y');

        var purgedFired = false;
        buffer.BufferPurged += (_, _) => purgedFired = true;

        buffer.Clear();

        Assert.True(buffer.IsEmpty);
        Assert.Equal(0, buffer.Length);
        Assert.Equal(string.Empty, buffer.GetContent());
        Assert.True(purgedFired);
    }

    [Fact]
    public void AutoPurge_PurgesBufferAfterIdleTimeout()
    {
        using var buffer = new CircularKeyBuffer(50, TimeSpan.FromMilliseconds(50));
        var purgedFired = new ManualResetEventSlim(false);

        buffer.BufferPurged += (_, _) => purgedFired.Set();

        buffer.Append('t');
        buffer.Append('e');
        buffer.Append('s');
        buffer.Append('t');

        Assert.Equal("test", buffer.GetContent());

        var signaled = purgedFired.Wait(TimeSpan.FromMilliseconds(500));

        Assert.True(signaled, "El temporizador de inactividad debería haber purgado el búfer.");
        Assert.True(buffer.IsEmpty);
        Assert.Equal(string.Empty, buffer.GetContent());
    }

    [Fact]
    public void AutoPurge_ResetOnNewAppend()
    {
        using var buffer = new CircularKeyBuffer(50, TimeSpan.FromMilliseconds(80));
        var purgedFired = new ManualResetEventSlim(false);

        buffer.BufferPurged += (_, _) => purgedFired.Set();

        buffer.Append('a');
        Thread.Sleep(40);
        buffer.Append('b'); // Reinicia el temporizador de 80ms
        Thread.Sleep(40);

        Assert.False(buffer.IsEmpty, "El búfer no debería haberse purgado porque se escribió antes de vencer.");
        Assert.Equal("ab", buffer.GetContent());

        var signaled = purgedFired.Wait(TimeSpan.FromMilliseconds(300));
        Assert.True(signaled);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAppendsAndReads_DoNotThrowOrCorrupt()
    {
        using var buffer = new CircularKeyBuffer(20);
        const int threadCount = 10;
        const int iterations = 100;

        var tasks = new Task[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var charToAppend = (char)('a' + t);
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    buffer.Append(charToAppend);
                    var _ = buffer.GetContent();
                    if (i % 10 == 0)
                    {
                        buffer.HandleBackspace();
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.True(buffer.Length <= 20);
        Assert.NotNull(buffer.GetContent());
    }

    [Fact]
    public void Throws_WhenUsedAfterDisposed()
    {
        var buffer = new CircularKeyBuffer(50);
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.Append('a'));
        Assert.Throws<ObjectDisposedException>(() => buffer.HandleBackspace());
        Assert.Throws<ObjectDisposedException>(() => buffer.GetContent());
        Assert.Throws<ObjectDisposedException>(() => buffer.Clear());
    }

    [Fact]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularKeyBuffer(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularKeyBuffer(-5));
    }
}
