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
