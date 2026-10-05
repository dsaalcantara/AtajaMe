using System.Globalization;
using AtajaMe.Application.Conditionals;

namespace AtajaMe.Tests.Conditionals;

public class ExpressionEvaluatorTests
{
    [Theory]
    [InlineData("hora < 12", 9, true)]
    [InlineData("hora < 12", 12, false)]
    [InlineData("hora < 12", 15, false)]
    [InlineData("hora >= 12 && hora < 20", 12, true)]
    [InlineData("hora >= 12 && hora < 20", 19, true)]
    [InlineData("hora >= 12 && hora < 20", 20, false)]
    [InlineData("hora >= 20", 21, true)]
    [InlineData("hora >= 20", 8, false)]
    public void Evaluate_TimeComparisons(string expression, int hour, bool expected)
    {
        var fixedTime = new DateTime(2026, 9, 28, hour, 30, 0);
        var context = new ConditionContext(fixedTime);

        var result = ExpressionEvaluator.Evaluate(expression, context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_DayOfWeek_IsoConvention()
    {
        // 2026-09-28 es lunes (día 1)
        var monday = new DateTime(2026, 9, 28, 10, 0, 0);
        var contextMonday = new ConditionContext(monday);

        Assert.True(ExpressionEvaluator.Evaluate("dia_semana == 1", contextMonday));
        Assert.True(ExpressionEvaluator.Evaluate("dia_semana <= 5", contextMonday));
        Assert.False(ExpressionEvaluator.Evaluate("dia_semana > 5", contextMonday));

        // 2026-10-04 es domingo (día 7)
        var sunday = new DateTime(2026, 10, 4, 10, 0, 0);
        var contextSunday = new ConditionContext(sunday);

        Assert.True(ExpressionEvaluator.Evaluate("dia_semana == 7", contextSunday));
        Assert.True(ExpressionEvaluator.Evaluate("dia_semana > 5", contextSunday));
        Assert.False(ExpressionEvaluator.Evaluate("dia_semana <= 5", contextSunday));
    }

    [Theory]
    [InlineData("mes == 9", true)]
    [InlineData("mes == 12", false)]
    [InlineData("año == 2026", true)]
    [InlineData("trimestre == 3", true)]
    [InlineData("cuatrimestre == 3", true)]
    public void Evaluate_DateParts(string expression, bool expected)
    {
        var time = new DateTime(2026, 9, 28, 12, 0, 0);
        var context = new ConditionContext(time);

        var result = ExpressionEvaluator.Evaluate(expression, context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_UserInputs_StringsAndNumbers()
    {
        var inputs = new Dictionary<string, string>
        {
            ["idioma"] = "es",
            ["departamento"] = "Ventas",
            ["nivel"] = "2"
        };
        var context = new ConditionContext(userInputs: inputs);

        Assert.True(ExpressionEvaluator.Evaluate("input:idioma == 'es'", context));
        Assert.True(ExpressionEvaluator.Evaluate("idioma == 'ES'", context)); // Insensible a mayúsculas
        Assert.False(ExpressionEvaluator.Evaluate("idioma == 'en'", context));

        Assert.True(ExpressionEvaluator.Evaluate("departamento != 'Soporte'", context));
        Assert.True(ExpressionEvaluator.Evaluate("nivel >= 2", context));
        Assert.False(ExpressionEvaluator.Evaluate("nivel > 2", context));
    }

    [Theory]
    [InlineData("hora < 12 || hora >= 20", 8, true)]
    [InlineData("hora < 12 || hora >= 20", 15, false)]
    [InlineData("hora < 12 || hora >= 20", 22, true)]
    [InlineData("hora < 12 o hora >= 20", 8, true)]
    [InlineData("hora >= 12 y hora < 20", 15, true)]
    public void Evaluate_LogicalOrAndAndKeywords(string expression, int hour, bool expected)
    {
        var time = new DateTime(2026, 9, 28, hour, 0, 0);
        var context = new ConditionContext(time);

        var result = ExpressionEvaluator.Evaluate(expression, context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("sin_sentido_invalido ?? %%", false)]
    public void Evaluate_InvalidOrEmptyExpressions_ReturnsFalseGracefully(string expression, bool expected)
    {
        var context = new ConditionContext();
        var result = ExpressionEvaluator.Evaluate(expression, context);

        Assert.Equal(expected, result);
    }
}
