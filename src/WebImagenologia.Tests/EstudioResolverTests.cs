using WebImagenologia.Web.Models.Visor;
using WebImagenologia.Web.Services.Visor;

namespace WebImagenologia.Tests;

public class EstudioResolverTests
{
    private sealed class FakeDicomWebClient : IDicomWebClient
    {
        public IReadOnlyList<EstudioDicomDto> EstudiosPorAccession { get; set; } = Array.Empty<EstudioDicomDto>();
        public IReadOnlyList<EstudioDicomDto> EstudiosPorPatientId { get; set; } = Array.Empty<EstudioDicomDto>();

        public string? UltimoAccessionConsultado { get; private set; }
        public string? UltimoPatientIdConsultado { get; private set; }

        public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorAccessionNumberAsync(
            string accessionNumber,
            CancellationToken cancellationToken = default)
        {
            UltimoAccessionConsultado = accessionNumber;
            return Task.FromResult(EstudiosPorAccession);
        }

        public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorPatientIdAsync(
            string patientId,
            CancellationToken cancellationToken = default)
        {
            UltimoPatientIdConsultado = patientId;
            return Task.FromResult(EstudiosPorPatientId);
        }

        public Task<byte[]?> ObtenerRenderedInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            int? frame,
            string formato,
            CancellationToken cancellationToken = default) => Task.FromResult<byte[]?>(null);
    }

    private static EstudioDicomDto BuildEstudio(string studyInstanceUid) => new()
    {
        StudyInstanceUID = studyInstanceUid,
        Modality = "CT"
    };

    [Fact]
    public async Task ResolverAsync_ConCaso_UsaRutaDeAccessionNumberCuandoHayMatch()
    {
        var client = new FakeDicomWebClient
        {
            EstudiosPorAccession = [BuildEstudio("1.2.3.accession")]
        };
        var resolver = new EstudioResolver(client);

        var resultado = await resolver.ResolverAsync(caso: "CASO-123", identificacion: null);

        Assert.Equal(EstudioResolver.CriterioCaso, resultado.CriterioBusqueda);
        Assert.Single(resultado.Estudios);
        Assert.Equal("1.2.3.accession", resultado.Estudios[0].StudyInstanceUID);
        Assert.Equal("CASO-123", client.UltimoAccessionConsultado);
        Assert.Null(client.UltimoPatientIdConsultado);
    }

    [Fact]
    public async Task ResolverAsync_ConCaso_HaceFallbackAPatientIdCuandoAccessionNoMatchea()
    {
        var client = new FakeDicomWebClient
        {
            EstudiosPorAccession = Array.Empty<EstudioDicomDto>(),
            EstudiosPorPatientId = [BuildEstudio("1.2.3.fallback")]
        };
        var resolver = new EstudioResolver(client);

        var resultado = await resolver.ResolverAsync(caso: "CASO-999", identificacion: null);

        Assert.Equal(EstudioResolver.CriterioCaso, resultado.CriterioBusqueda);
        Assert.Single(resultado.Estudios);
        Assert.Equal("1.2.3.fallback", resultado.Estudios[0].StudyInstanceUID);
        Assert.Equal("CASO-999", client.UltimoAccessionConsultado);
        Assert.Equal("CASO-999", client.UltimoPatientIdConsultado);
    }

    [Fact]
    public async Task ResolverAsync_ConIdentificacion_VaDirectoAPatientId()
    {
        var client = new FakeDicomWebClient
        {
            EstudiosPorPatientId = [BuildEstudio("1.2.3.identificacion")]
        };
        var resolver = new EstudioResolver(client);

        var resultado = await resolver.ResolverAsync(caso: null, identificacion: "123456789");

        Assert.Equal(EstudioResolver.CriterioIdentificacion, resultado.CriterioBusqueda);
        Assert.Single(resultado.Estudios);
        Assert.Equal("123456789", client.UltimoPatientIdConsultado);
        Assert.Null(client.UltimoAccessionConsultado);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("caso", "identificacion")]
    public async Task ResolverAsync_LanzaExcepcionSiNoHayExactamenteUnCriterio(string? caso, string? identificacion)
    {
        var resolver = new EstudioResolver(new FakeDicomWebClient());

        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolverAsync(caso, identificacion));
    }
}
