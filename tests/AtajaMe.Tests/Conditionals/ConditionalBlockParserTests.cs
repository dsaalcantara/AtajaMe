using System.Globalization;
using AtajaMe.Application.Conditionals;
using AtajaMe.Application.Variables;

namespace AtajaMe.Tests.Conditionals;

public class ConditionalBlockParserTests
{
    private readonly VariableResolutionService _variableService = new();

    [Fact]
    public void ProcessConditionals_MorningGreeting_SelectsMorning()
    {
        var template = @"{si hora < 12}
Buenos días
{sino si hora < 20}
Buenas tardes
{sino}
Buenas noches
{/si}";

        var morningTime = new DateTime(2026, 9, 28, 9, 15, 0);
        var context = new ConditionContext(morningTime);

        var result = ConditionalBlockParser.ProcessConditionals(template, context);

        Assert.Equal("Buenos días", result);
    }

    [Fact]
    public void ProcessConditionals_AfternoonGreeting_SelectsAfternoon()
    {
        var template = @"{si hora < 12}
Buenos días
{sino si hora < 20}
Buenas tardes
{sino}
Buenas noches
{/si}";

        var afternoonTime = new DateTime(2026, 9, 28, 16, 45, 0);
        var context = new ConditionContext(afternoonTime);

        var result = ConditionalBlockParser.ProcessConditionals(template, context);

        Assert.Equal("Buenas tardes", result);
    }

    [Fact]
    public void ProcessConditionals_NightGreeting_SelectsNight()
    {
        var template = @"{si hora < 12}
Buenos días
{sino si hora < 20}
Buenas tardes
{sino}
Buenas noches
{/si}";

        var nightTime = new DateTime(2026, 9, 28, 22, 10, 0);
        var context = new ConditionContext(nightTime);

        var result = ConditionalBlockParser.ProcessConditionals(template, context);

        Assert.Equal("Buenas noches", result);
    }

    [Fact]
    public void ProcessConditionals_EnglishSyntax_SupportsIfElifElseEndif()
    {
        var template = "{if hour < 12}Good morning{elif hour < 18}Good afternoon{else}Good evening{/if}";

        var afternoonTime = new DateTime(2026, 9, 28, 14, 0, 0);
        var context = new ConditionContext(afternoonTime);

        var result = ConditionalBlockParser.ProcessConditionals(template, context);

        Assert.Equal("Good afternoon", result);
    }

    [Fact]
    public void ProcessConditionals_NestedConditionals_EvaluatesBothLevelsCorrectly()
    {
        var template = @"{si dia_semana <= 5}
{si hora < 12}Buenos días laborales{sino}Buenas tardes laborales{/si}
{sino}
¡Feliz fin de semana!
{/si}";

        // Lunes 10:00 -> Laborable, mañana
        var mondayMorning = new DateTime(2026, 9, 28, 10, 0, 0);
        var res1 = ConditionalBlockParser.ProcessConditionals(template, new ConditionContext(mondayMorning));
        Assert.Equal("Buenos días laborales", res1);

        // Lunes 16:00 -> Laborable, tarde
        var mondayAfternoon = new DateTime(2026, 9, 28, 16, 0, 0);
        var res2 = ConditionalBlockParser.ProcessConditionals(template, new ConditionContext(mondayAfternoon));
        Assert.Equal("Buenas tardes laborales", res2);

        // Domingo 11:00 -> Fin de semana
        var sundayMorning = new DateTime(2026, 10, 4, 11, 0, 0);
        var res3 = ConditionalBlockParser.ProcessConditionals(template, new ConditionContext(sundayMorning));
        Assert.Equal("¡Feliz fin de semana!", res3);
    }

    [Fact]
    public void VariableResolutionService_ResolveAll_CombinesConditionalsAndDynamicVariables()
    {
        var template = @"{si hora < 12}
Buenos días, hoy es {fecha} y son las {hora}.
{sino}
Buenas tardes, hoy es {fecha}.
{/si}";

        var morningTime = new DateTime(2026, 9, 28, 8, 30, 0);
        var culture = new CultureInfo("es-ES");

        var result = _variableService.ResolveAll(template, null, morningTime, culture);

        Assert.Contains("Buenos días", result);
        Assert.Contains("28/09/2026", result);
        Assert.DoesNotContain("Buenas tardes", result);
    }

    [Fact]
    public void VariableResolutionService_ResolveAll_WithInteractiveInputsInConditionals()
    {
        var template = @"{si input:idioma == 'en'}
Hello {input:nombre}, welcome!
{sino}
Hola {input:nombre}, ¡bienvenido!
{/si}";

        var inputsEn = new Dictionary<string, string>
        {
            ["idioma"] = "en",
            ["nombre"] = "John"
        };

        var resultEn = _variableService.ResolveAll(template, inputsEn);
        Assert.Equal("Hello John, welcome!", resultEn.Trim());

        var inputsEs = new Dictionary<string, string>
        {
            ["idioma"] = "es",
            ["nombre"] = "Carlos"
        };

        var resultEs = _variableService.ResolveAll(template, inputsEs);
        Assert.Equal("Hola Carlos, ¡bienvenido!", resultEs.Trim());
    }
}
