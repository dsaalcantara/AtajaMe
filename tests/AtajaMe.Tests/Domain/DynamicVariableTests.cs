using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using Xunit;

namespace AtajaMe.Tests.Domain;

public class DynamicVariableTests
{
    [Fact]
    public void ExtractVariables_WithDateAndTimeTags_ShouldExtractSystemVariables()
    {
        // Arrange
        var content = "Hoy es {date} y la hora actual es {time}. Registro completo: {datetime}.";

        // Act
        var vars = DynamicVariable.ExtractVariables(content);

        // Assert
        Assert.Equal(3, vars.Count);
        Assert.Equal(VariableType.SystemDate, vars[0].Type);
        Assert.Equal("{date}", vars[0].RawTag);
        Assert.Equal(VariableType.SystemTime, vars[1].Type);
        Assert.Equal("{time}", vars[1].RawTag);
        Assert.Equal(VariableType.SystemDateTime, vars[2].Type);
        Assert.Equal("{datetime}", vars[2].RawTag);
    }

    [Fact]
    public void ExtractVariables_WithUpperCaseTags_ShouldExtractSystemVariablesCaseInsensitive()
    {
        // Arrange
        var content = "Hoy es {DATE} y {TIME} en {DATETIME}.";

        // Act
        var vars = DynamicVariable.ExtractVariables(content);

        // Assert
        Assert.Equal(3, vars.Count);
        Assert.Equal(VariableType.SystemDate, vars[0].Type);
        Assert.Equal(VariableType.SystemTime, vars[1].Type);
        Assert.Equal(VariableType.SystemDateTime, vars[2].Type);
    }

    [Fact]
    public void ExtractVariables_WithInteractiveInput_ShouldParseNameAndDefaultValue()
    {
        // Arrange
        var content = "Estimado/a {input:NombreCliente|Cliente}: le adjunto la factura {input:NumeroFactura}.";

        // Act
        var vars = DynamicVariable.ExtractVariables(content);

        // Assert
        Assert.Equal(2, vars.Count);

        Assert.Equal(VariableType.InteractiveInput, vars[0].Type);
        Assert.Equal("{input:NombreCliente|Cliente}", vars[0].RawTag);
        Assert.Equal("NombreCliente", vars[0].Name);
        Assert.Equal("Cliente", vars[0].DefaultValue);

        Assert.Equal(VariableType.InteractiveInput, vars[1].Type);
        Assert.Equal("{input:NumeroFactura}", vars[1].RawTag);
        Assert.Equal("NumeroFactura", vars[1].Name);
        Assert.Null(vars[1].DefaultValue);
    }

    [Fact]
    public void ExtractVariables_WithNoVariables_ShouldReturnEmptyList()
    {
        // Arrange
        var content = "Texto plano ordinario sin ninguna variable.";

        // Act
        var vars = DynamicVariable.ExtractVariables(content);

        // Assert
        Assert.Empty(vars);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractVariables_WithNullOrWhitespace_ShouldReturnEmptyList(string? content)
    {
        var vars = DynamicVariable.ExtractVariables(content);
        Assert.Empty(vars);
    }
}
