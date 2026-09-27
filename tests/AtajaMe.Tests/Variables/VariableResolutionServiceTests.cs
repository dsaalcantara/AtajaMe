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
}
