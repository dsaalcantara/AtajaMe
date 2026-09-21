using AtajaMe.Windows.Security;

namespace AtajaMe.Tests.Windows;

public sealed class SensitiveControlDetectorTests
{
    [Fact]
    public void IsSensitiveControlFocused_WithCustomEvaluatorReturningTrue_ReturnsTrue()
    {
        var detector = new SensitiveControlDetector(customEvaluator: () => true);

        var result = detector.IsSensitiveControlFocused();

        Assert.True(result);
    }

    [Fact]
    public void IsSensitiveControlFocused_WithCustomEvaluatorReturningFalse_ReturnsFalse()
    {
        var detector = new SensitiveControlDetector(customEvaluator: () => false);

        var result = detector.IsSensitiveControlFocused();

        Assert.False(result);
    }

    [Fact]
    public void IsSensitiveControlFocused_WithoutCustomEvaluator_DoesNotThrow()
    {
        var detector = new SensitiveControlDetector();

        // No debe lanzar excepciones no controladas ni fallar
        var exception = Record.Exception(() => detector.IsSensitiveControlFocused());

        Assert.Null(exception);
    }
}
