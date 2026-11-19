using CitizenTech.Zeladoria.DTOs;

namespace CitizenTech.Zeladoria.Services
{
    public interface IOcorrenciaService
    {
        Task<IEnumerable<OcorrenciaResponseDto>> ListarTodasAsync(int? categoriaId, int? statusId);
        Task<OcorrenciaResponseDto?> ObterPorIdAsync(int id);
        Task<OcorrenciaResponseDto> CriarAsync(CriarOcorrenciaDto dto);

        // Suporte para transação atômica
        Task<OcorrenciaResponseDto> CriarComTransacaoAsync(CriarOcorrenciaComComentarioDto dto, bool simularErro = false);

        Task<OcorrenciaResponseDto?> AtualizarStatusAsync(int id, int novoStatusId);
        Task<bool> RemoverAsync(int id);
    }
}