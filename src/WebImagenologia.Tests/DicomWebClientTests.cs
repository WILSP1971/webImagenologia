using System.Text.Json;
using WebImagenologia.Web.Services.Visor;

namespace WebImagenologia.Tests;

public class DicomWebClientTests
{
    [Fact]
    public void MapStudy_LeeTagsDicomJson()
    {
        const string json = """
            {
              "0020000D": { "vr": "UI", "Value": ["1.2.3.4.5"] },
              "00080050": { "vr": "SH", "Value": ["ACC-99"] },
              "00100020": { "vr": "LO", "Value": ["123456"] },
              "00080060": { "vr": "CS", "Value": ["CT"] },
              "00080020": { "vr": "DA", "Value": ["20260115"] },
              "00081030": { "vr": "LO", "Value": ["Torax"] },
              "00201206": { "vr": "IS", "Value": ["3"] },
              "00201208": { "vr": "IS", "Value": ["120"] }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var estudio = DicomWebClient.MapStudy(doc.RootElement);

        Assert.Equal("1.2.3.4.5", estudio.StudyInstanceUID);
        Assert.Equal("ACC-99", estudio.AccessionNumber);
        Assert.Equal("123456", estudio.PatientId);
        Assert.Equal("CT", estudio.Modality);
        Assert.Equal("20260115", estudio.StudyDate);
        Assert.Equal("Torax", estudio.StudyDescription);
        Assert.Equal(3, estudio.NumberOfSeries);
        Assert.Equal(120, estudio.NumberOfInstances);
    }
}
