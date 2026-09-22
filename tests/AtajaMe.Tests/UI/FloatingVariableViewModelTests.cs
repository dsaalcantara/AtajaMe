using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class FloatingVariableViewModelTests
{
    [Fact]
    public void Constructor_ParsesVariableNames_CreatesInputFields()
    {
        var varNames = new[] { "input:cliente", "input:importe", "ciudad" };
        var vm = new FloatingVariableViewModel("fact", "Factura", varNames);

        Assert.Equal("fact", vm.SnippetCode);
        Assert.Equal("Factura", vm.SnippetName);
        Assert.Equal(3, vm.Fields.Count);
        Assert.Equal("cliente", vm.Fields[0].DisplayLabel);
        Assert.Equal("importe", vm.Fields[1].DisplayLabel);
        Assert.Equal("ciudad", vm.Fields[2].DisplayLabel);
    }

    [Fact]
    public void ConfirmCommand_SetsConfirmedTrue_AndRaisesRequestClose()
    {
        var vm = new FloatingVariableViewModel("saludo", "Saludo", new[] { "input:nombre" });
        bool closeRaised = false;
        vm.RequestClose += (_, _) => closeRaised = true;

        vm.Fields[0].Value = "Carlos";
        vm.ConfirmCommand.Execute(null);

        Assert.True(vm.IsConfirmed);
        Assert.True(closeRaised);
        var values = vm.GetValues();
        Assert.Equal("Carlos", values["nombre"]);
    }

    [Fact]
    public void CancelCommand_SetsConfirmedFalse_AndRaisesRequestClose()
    {
        var vm = new FloatingVariableViewModel("saludo", "Saludo", new[] { "input:nombre" });
        bool closeRaised = false;
        vm.RequestClose += (_, _) => closeRaised = true;

        vm.CancelCommand.Execute(null);

        Assert.False(vm.IsConfirmed);
        Assert.True(closeRaised);
    }

    [Fact]
    public void GetValues_ProvidesBothRawAndCleanKeys()
    {
        var vm = new FloatingVariableViewModel("demo", "Demo", new[] { "input:correo" });
        vm.Fields[0].Value = "test@example.com";

        var dict = vm.GetValues();

        Assert.Equal("test@example.com", dict["input:correo"]);
        Assert.Equal("test@example.com", dict["correo"]);
    }
}
