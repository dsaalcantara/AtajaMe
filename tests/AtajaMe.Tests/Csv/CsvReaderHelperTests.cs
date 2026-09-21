using System.IO;
using System.Text;
using AtajaMe.Application.Csv.Parser;

namespace AtajaMe.Tests.Csv;

public class CsvReaderHelperTests
{
    [Fact]
    public void Read_SimpleCsv_ParsesCorrectly()
    {
        string csv = "Code,Name,Content,FolderPath,CaseSensitive,IsActive,IsFavorite,SortOrder\r\n" +
                     "!saludo,Saludo,Hola mundo,General,false,true,false,1\r\n" +
                     "!bye,Despedida,Hasta luego,,true,false,true,2";

        using var reader = new StringReader(csv);
        var rows = CsvReaderHelper.Read(reader);

        Assert.Equal(2, rows.Count);

        Assert.Equal("!saludo", rows[0].Code);
        Assert.Equal("Saludo", rows[0].Name);
        Assert.Equal("Hola mundo", rows[0].Content);
        Assert.Equal("General", rows[0].FolderPath);
        Assert.False(rows[0].CaseSensitive);
        Assert.True(rows[0].IsActive);
        Assert.False(rows[0].IsFavorite);
        Assert.Equal(1, rows[0].SortOrder);

        Assert.Equal("!bye", rows[1].Code);
        Assert.Equal("Despedida", rows[1].Name);
        Assert.Equal("Hasta luego", rows[1].Content);
        Assert.Equal("", rows[1].FolderPath);
        Assert.True(rows[1].CaseSensitive);
        Assert.False(rows[1].IsActive);
        Assert.True(rows[1].IsFavorite);
        Assert.Equal(2, rows[1].SortOrder);
    }

    [Fact]
    public void Read_WithEscapedQuotesAndCommas_ParsesCorrectly()
    {
        string csv = "Code,Name,Content,FolderPath\r\n" +
                     "!firma,\"Firma, Oficial\",\"Atentamente,\r\n\"\"El Director\"\"\",Trabajo/Legal";

        using var reader = new StringReader(csv);
        var rows = CsvReaderHelper.Read(reader);

        Assert.Single(rows);
        Assert.Equal("!firma", rows[0].Code);
        Assert.Equal("Firma, Oficial", rows[0].Name);
        Assert.Equal("Atentamente,\r\n\"El Director\"", rows[0].Content);
        Assert.Equal("Trabajo/Legal", rows[0].FolderPath);
    }

    [Fact]
    public void Read_WithUtf8Bom_StripsBomAndParses()
    {
        byte[] bom = Encoding.UTF8.GetPreamble();
        string csv = "Code,Name,Content\r\n!test,Test,Prueba BOM";
        byte[] csvBytes = Encoding.UTF8.GetBytes(csv);
        byte[] fullBytes = bom.Concat(csvBytes).ToArray();

        using var ms = new MemoryStream(fullBytes);
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var rows = CsvReaderHelper.Read(reader);

        Assert.Single(rows);
        Assert.Equal("!test", rows[0].Code);
        Assert.Equal("Prueba BOM", rows[0].Content);
    }

    [Fact]
    public void Read_EmptyCsv_ReturnsEmptyList()
    {
        using var reader = new StringReader("");
        var rows = CsvReaderHelper.Read(reader);

        Assert.Empty(rows);
    }
}
