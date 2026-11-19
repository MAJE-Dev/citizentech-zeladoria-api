using CitizenTech.Zeladoria.DTOs;
using CitizenTech.Zeladoria.Services;
using Microsoft.AspNetCore.Mvc;

namespace CitizenTech.Zeladoria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcorrenciasController : ControllerBase
    {
        private readonly IOcorrenciaService _service;

        public OcorrenciasController(IOcorrenciaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OcorrenciaResponseDto>>> Listar(
            [FromQuery] int? categoriaId,
            [FromQuery] int? statusId)
        {
            var resultado = await _service.ListarTodasAsync(categoriaId, statusId);
            return Ok(resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OcorrenciaResponseDto>> ObterPorId(int id)
        {
            var ocorrencia = await _service.ObterPorIdAsync(id);
            if (ocorrencia is null)
            {
                return NotFound(new { Message = $"Ocorrência com ID {id} não encontrada." });
            }

            return Ok(ocorrencia);
        }

        // FromForm para suportar o IFormFile
        [HttpPost]
        public async Task<ActionResult<OcorrenciaResponseDto>> Criar([FromForm] CriarOcorrenciaDto dto)
        {
            var criada = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }

        

        [HttpPost("transacional")]
        public async Task<ActionResult<OcorrenciaResponseDto>> CriarComTransacao(
            [FromForm] CriarOcorrenciaComComentarioDto dto,
            [FromQuery] bool simularErro = false)
        {
            try
            {
                var resultado = await _service.CriarComTransacaoAsync(dto, simularErro);
                return StatusCode(201, resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensagem = "Transação falhou e foi revertida com sucesso (Rollback executado).",
                    Detalhe = ex.Message
                });
            }
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<OcorrenciaResponseDto>> AtualizarStatus(int id, [FromBody] AtualizarStatusDto dto)
        {
            var atualizada = await _service.AtualizarStatusAsync(id, dto.NovoStatusId);
            if (atualizada is null)
            {
                return NotFound(new { Message = $"Ocorrência com ID {id} não encontrada." });
            }

            return Ok(atualizada);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remover(int id)
        {
            var removida = await _service.RemoverAsync(id);
            if (!removida)
            {
                return NotFound(new { Message = $"Ocorrência com ID {id} não encontrada." });
            }

            return NoContent();
        }
    }
}