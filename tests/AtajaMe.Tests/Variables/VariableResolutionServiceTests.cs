using System.Globalization;
using AtajaMe.Application.Variables;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Tests.Variables;

public class VariableResolutionServiceTests
{
    private readonly VariableResolutionService _service = new();

    [Theory]
    [InlineData("Hola mundo", false)]
    [InlineData("Hoy es {date}", true)]
    [InlineData("Son las {time}", true)]
    [InlineData("Registro: {datetime}", true)]
    [InlineData("Estimado {input:nombre}", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void HasVariables_DetectsCorrectly(string? content, bool expected)
    {
        Assert.Equal(expected, _service.HasVariables(content));
    }

    [Theory]
    [InlineData("Hoy es {date}", false)]
    [InlineData("Estimado {input:nombre}", true)]
    [InlineData("Atentamente, {input:firma|Dpto. Legal}", true)]
    [InlineData(null, false)]
    public void HasInteractiveVariables_DetectsCorrectly(string? content, bool expected)
    {
        Assert.Equal(expected, _service.HasInteractiveVariables(content));
    }

    [Fact]
    public void ExtractVariables_ExtractsAllTypes()
    {
        var template = "Fecha: {date}, Hora: {TIME}, Usuario: {input:cliente|Invitado}";

        var vars = _service.ExtractVariables(template);

        Assert.Equal(3, vars.Count);
        Assert.Contains(vars, v => v.Type == VariableType.SystemDate);
        Assert.Contains(vars, v => v.Type == VariableType.SystemTime);
        Assert.Contains(vars, v => v.Type == VariableType.InteractiveInput && v.Name == "cliente" && v.DefaultValue == "Invitado");
    }

    [Fact]
    public void ExtractInteractiveVariables_ExtractsOnlyInputs()
    {
        var template = "Hoy {date} se atendió a {input:cliente} por {input:asunto|Consulta}";

        var inputs = _service.ExtractInteractiveVariables(template);

        Assert.Equal(2, inputs.Count);
        Assert.Equal("cliente", inputs[0].Name);
        Assert.Null(inputs[0].DefaultValue);
        Assert.Equal("asunto", inputs[1].Name);
        Assert.Equal("Consulta", inputs[1].DefaultValue);
    }

    [Fact]
    public void ResolveAutomaticVariables_SubstitutesDateAndTime()
    {
        var fixedDate = new DateTime(2026, 9, 21, 15, 30, 45);
        var culture = CultureInfo.InvariantCulture;
        var template = "Fecha: {date}, Hora: {time}, Combinada: {datetime}";

        var result = _service.ResolveAutomaticVariables(template, fixedDate, culture);

        var expectedDate = fixedDate.ToString("d", culture);
        var expectedTime = fixedDate.ToString("t", culture);
        var expectedDateTime = fixedDate.ToString("g", culture);

        Assert.Equal($"Fecha: {expectedDate}, Hora: {expectedTime}, Combinada: {expectedDateTime}", result);
    }

    [Fact]
    public void ResolveAutomaticVariables_CustomOptions_AppliesCustomFormats()
    {
        var options = new VariableResolutionOptions
        {
            DateFormat = "yyyy-MM-dd",
            TimeFormat = "HH:mm",
            DateTimeFormat = "yyyy-MM-dd HH:mm"
        };
        var service = new VariableResolutionService(options);
        var fixedDate = new DateTime(2026, 9, 21, 8, 5, 0);

        var result = service.ResolveAutomaticVariables("Fecha: {date} {time}", fixedDate, CultureInfo.InvariantCulture);

        Assert.Equal("Fecha: 2026-09-21 08:05", result);
    }

    [Fact]
    public void ResolveAutomaticVariables_CaseInsensitiveTags()
    {
        var fixedDate = new DateTime(2026, 9, 21, 12, 0, 0);
        var result = _service.ResolveAutomaticVariables("{DATE} - {Time} - {DATETIME}", fixedDate, CultureInfo.InvariantCulture);

        Assert.DoesNotContain("{DATE}", result);
        Assert.DoesNotContain("{Time}", result);
        Assert.DoesNotContain("{DATETIME}", result);
    }

    [Theory]
    [InlineData("{fecha:dd/mm/aa}", "26/09/26")]
    [InlineData("{fecha:dd/mm/aaaa}", "26/09/2026")]
    [InlineData("{fecha:aammdd}", "260926")]
    [InlineData("{fecha:aaaammdd}", "20260926")]
    [InlineData("{fecha:aaaa-mm-dd}", "2026-09-26")]
    [InlineData("{date:dd/MM/yy}", "26/09/26")]
    [InlineData("{date:yyMMdd}", "260926")]
    [InlineData("{date:yyyy-MM-dd}", "2026-09-26")]
    [InlineData("{hora:HH:mm}", "16:35")]
    [InlineData("{hora:HH:mm:ss}", "16:35:45")]
    [InlineData("{fechahora:dd/mm/aa HH:mm}", "26/09/26 16:35")]
    public void ResolveAutomaticVariables_CustomInlineFormats_ProducesExpectedOutput(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 26, 16, 35, 45);
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CW {semana}", "CW 39")]
    [InlineData("CW {cw}", "CW 39")]
    [InlineData("CW {semana:+1}", "CW 40")]
    [InlineData("CW {cw:+1}", "CW 40")]
    [InlineData("CW {semana:proxima}", "CW 40")]
    [InlineData("CW {cw:proxima}", "CW 40")]
    [InlineData("CW {semana:-1}", "CW 38")]
    [InlineData("CW {cw:anterior}", "CW 38")]
    [InlineData("Semana {semana:00}", "Semana 39")]
    [InlineData("{week}", "39")]
    [InlineData("{week:+1}", "40")]
    public void ResolveAutomaticVariables_CalendarWeek_CalculatesIsoWeekCorrectly(string template, string expected)
    {
        // 27 de septiembre de 2026 es domingo, perteneciente a la semana 39 ISO
        var fixedDate = new DateTime(2026, 9, 27, 10, 0, 0);
        var result = _service.ResolveAutomaticVariables(template, fixedDate, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractVariables_WithCalendarWeek_IdentifiesCorrectType()
    {
        var template = "Planificación para CW {semana} y CW {semana:+1}.";
        var vars = _service.ExtractVariables(template);

        Assert.Equal(2, vars.Count);
        Assert.Equal(VariableType.CalendarWeek, vars[0].Type);
        Assert.Equal("{semana}", vars[0].RawTag);
        Assert.Equal(VariableType.CalendarWeek, vars[1].Type);
        Assert.Equal("{semana:+1}", vars[1].RawTag);
    }

    [Fact]
    public void ExtractVariables_WithCustomFormats_DoesNotTreatAsInteractiveInput()
    {
        var template = "Hoy es {fecha:dd/mm/aa} y amd es {fecha:aammdd}. Nombre: {input:cliente}";
        var vars = _service.ExtractVariables(template);

        Assert.Equal(3, vars.Count);
        Assert.Equal(VariableType.SystemDate, vars[0].Type);
        Assert.Equal("{fecha:dd/mm/aa}", vars[0].RawTag);
        Assert.Equal(VariableType.SystemDate, vars[1].Type);
        Assert.Equal("{fecha:aammdd}", vars[1].RawTag);
        Assert.Equal(VariableType.InteractiveInput, vars[2].Type);
        Assert.Equal("cliente", vars[2].Name);
    }

    [Fact]
    public void ResolveInteractiveVariables_SubstitutesWithProvidedValues()
    {
        var template = "Estimado/a {input:nombre}, su código es {input:codigo}.";
        var inputs = new Dictionary<string, string>
        {
            ["nombre"] = "Carlos Gómez",
            ["codigo"] = "XYZ-123"
        };

        var result = _service.ResolveInteractiveVariables(template, inputs);

        Assert.Equal("Estimado/a Carlos Gómez, su código es XYZ-123.", result);
    }

    [Fact]
    public void ResolveInteractiveVariables_UsesDefaultValueWhenInputMissing()
    {
        var template = "Servicio: {input:servicio|Básico}, Costo: {input:costo|Gratis}";
        var inputs = new Dictionary<string, string>
        {
            ["servicio"] = "Premium"
        };

        var result = _service.ResolveInteractiveVariables(template, inputs);

        Assert.Equal("Servicio: Premium, Costo: Gratis", result);
    }

    [Fact]
    public void ResolveInteractiveVariables_CaseInsensitiveInputKeys()
    {
        var template = "Hola {input:NombreCliente}";
        var inputs = new Dictionary<string, string>
        {
            ["nombrecliente"] = "Ana"
        };

        var result = _service.ResolveInteractiveVariables(template, inputs);

        Assert.Equal("Hola Ana", result);
    }

    [Fact]
    public void ResolveAll_ResolvesBothAutomaticAndInteractive()
    {
        var fixedDate = new DateTime(2026, 9, 21, 10, 0, 0);
        var template = "[{date}] Ticket creado para {input:usuario|Anonimo}";
        var inputs = new Dictionary<string, string>
        {
            ["usuario"] = "Laura"
        };

        var result = _service.ResolveAll(template, inputs, fixedDate, CultureInfo.InvariantCulture);

        var expectedDate = fixedDate.ToString("d", CultureInfo.InvariantCulture);
        Assert.Equal($"[{expectedDate}] Ticket creado para Laura", result);
    }

    [Fact]
    public void ResolveAll_NullOrEmptyContent_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, _service.ResolveAll(null));
        Assert.Equal(string.Empty, _service.ResolveAll(""));
    }

    [Theory]
    [InlineData("{mes}", "09")]
    [InlineData("{month}", "09")]
    [InlineData("{mes:0}", "9")]
    [InlineData("{mes:nombre}", "septiembre")]
    [InlineData("{mes:Nombre}", "Septiembre")]
    [InlineData("{mes:MAYUS}", "SEPTIEMBRE")]
    [InlineData("{mes:corto}", "sept")]
    [InlineData("{mes:+1}", "10")]
    [InlineData("{mes:-1}", "08")]
    [InlineData("{mes:proximo}", "10")]
    [InlineData("{mes:anterior}", "08")]
    [InlineData("{mes:+1:Nombre}", "Octubre")]
    [InlineData("{mes:nombre,+1}", "octubre")]
    public void ResolveAutomaticVariables_Month_FormatsCorrectly(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 28, 14, 0, 0);
        var culture = new CultureInfo("es-ES");
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, culture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveAutomaticVariables_Month_RollsOverYear()
    {
        var fixedDate = new DateTime(2026, 12, 15, 10, 0, 0);
        var culture = new CultureInfo("es-ES");

        var nextMonth = _service.ResolveAutomaticVariables("{mes:+1}", fixedDate, culture);
        var nextMonthName = _service.ResolveAutomaticVariables("{mes:+1:Nombre}", fixedDate, culture);

        Assert.Equal("01", nextMonth);
        Assert.Equal("Enero", nextMonthName);
    }

    [Theory]
    [InlineData("{dia}", "28")]
    [InlineData("{day}", "28")]
    [InlineData("{dia:0}", "28")]
    [InlineData("{dia:nombre}", "lunes")]
    [InlineData("{dia:Nombre}", "Lunes")]
    [InlineData("{dia:MAYUS}", "LUNES")]
    [InlineData("{dia:corto}", "lun")]
    [InlineData("{dia:+1}", "29")]
    [InlineData("{dia:-1}", "27")]
    [InlineData("{dia:mañana}", "29")]
    [InlineData("{dia:ayer}", "27")]
    [InlineData("{dia:+1:Nombre}", "Martes")]
    [InlineData("{dia:año}", "271")]
    public void ResolveAutomaticVariables_Day_FormatsCorrectly(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 28, 14, 0, 0); // 28 de septiembre de 2026 es lunes, día 271 del año
        var culture = new CultureInfo("es-ES");
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("{año}", "2026")]
    [InlineData("{ano}", "2026")]
    [InlineData("{year}", "2026")]
    [InlineData("{año:corto}", "26")]
    [InlineData("{año:aa}", "26")]
    [InlineData("{año:+1}", "2027")]
    [InlineData("{año:-1}", "2025")]
    [InlineData("{año:+1:corto}", "27")]
    public void ResolveAutomaticVariables_Year_FormatsCorrectly(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 28, 14, 0, 0);
        var culture = new CultureInfo("es-ES");
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("{trimestre}", "T3")]
    [InlineData("{quarter}", "Q3")]
    [InlineData("{q}", "Q3")]
    [InlineData("{trimestre:Q}", "Q3")]
    [InlineData("{trimestre:num}", "3")]
    [InlineData("{trimestre:romano}", "III")]
    [InlineData("{trimestre:+1}", "T4")]
    [InlineData("{trimestre:-1}", "T2")]
    [InlineData("{trimestre:+1:Q}", "Q4")]
    public void ResolveAutomaticVariables_Quarter_FormatsCorrectly(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 28, 14, 0, 0); // Septiembre = mes 9 = T3 / Q3
        var culture = new CultureInfo("es-ES");
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, culture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveAutomaticVariables_Quarter_RollsOverYear()
    {
        var fixedDate = new DateTime(2026, 11, 10, 10, 0, 0); // Noviembre = T4
        var culture = new CultureInfo("es-ES");

        var nextQ = _service.ResolveAutomaticVariables("{trimestre:+1}", fixedDate, culture);
        var prevQ = _service.ResolveAutomaticVariables("{trimestre:-1}", fixedDate, culture);

        Assert.Equal("T1", nextQ);
        Assert.Equal("T3", prevQ);
    }

    [Theory]
    [InlineData("{cuatrimestre}", "C3")]
    [InlineData("{c}", "C3")]
    [InlineData("{fourmonth}", "C3")]
    [InlineData("{cuatrimestre:num}", "3")]
    [InlineData("{cuatrimestre:romano}", "III")]
    [InlineData("{cuatrimestre:+1}", "C1")]
    [InlineData("{cuatrimestre:-1}", "C2")]
    public void ResolveAutomaticVariables_FourMonth_FormatsCorrectly(string tag, string expected)
    {
        var fixedDate = new DateTime(2026, 9, 28, 14, 0, 0); // Septiembre = C3 (meses 9-12)
        var culture = new CultureInfo("es-ES");
        var result = _service.ResolveAutomaticVariables(tag, fixedDate, culture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, "C1")]  // Enero -> C1
    [InlineData(4, "C1")]  // Abril -> C1
    [InlineData(5, "C2")]  // Mayo -> C2
    [InlineData(8, "C2")]  // Agosto -> C2
    [InlineData(9, "C3")]  // Septiembre -> C3
    [InlineData(12, "C3")] // Diciembre -> C3
    public void ResolveAutomaticVariables_FourMonth_Boundaries(int month, string expected)
    {
        var date = new DateTime(2026, month, 15);
        var result = _service.ResolveAutomaticVariables("{cuatrimestre}", date, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractVariables_DetectsAllNewPeriodTypes()
    {
        var template = "{mes:nombre} {dia:Nombre} de {año} - Trimestre: {trimestre} y Cuatrimestre: {cuatrimestre}";
        var vars = _service.ExtractVariables(template);

        Assert.Equal(5, vars.Count);
        Assert.Equal(VariableType.Month, vars[0].Type);
        Assert.Equal(VariableType.Day, vars[1].Type);
        Assert.Equal(VariableType.Year, vars[2].Type);
        Assert.Equal(VariableType.Quarter, vars[3].Type);
        Assert.Equal(VariableType.FourMonth, vars[4].Type);
    }

    [Fact]
    public void ExtractInteractiveVariables_GreetingWithConditionals_ReturnsZeroVariables()
    {
        var template = "{si hora < 12}Buenos días{sino si hora < 20}Buenas tardes{sino}Buenas noches{/si}";
        
        var inputs = _service.ExtractInteractiveVariables(template);
        var hasInputs = _service.HasInteractiveVariables(template);

        Assert.Empty(inputs);
        Assert.False(hasInputs);
    }

    [Fact]
    public void ExtractVariables_ConditionalsAreNotTreatedAsInteractive()
    {
        var template = "{si hora < 12}Buenos días{sino si hora < 20}Buenas tardes{sino}Buenas noches{/si}";
        
        var vars = _service.ExtractVariables(template);

        Assert.DoesNotContain(vars, v => v.Type == VariableType.InteractiveInput);
    }
}

