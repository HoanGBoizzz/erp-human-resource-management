using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Admin.ThamSo;
using QLNS_BE.Services;

namespace QLNS_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/tham-so-he-thong")]
    [Authorize(Roles = "HR_ACC,GIAM_DOC")]
    public class ThamSoHeThongController : ControllerBase
    {
        private readonly ThamSoHeThongService _service;

        public ThamSoHeThongController(ThamSoHeThongService service)
        {
            _service = service;
        }

        // GET /api/tham-so-he-thong
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        // GET /api/tham-so-he-thong/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // POST /api/tham-so-he-thong  -- HR only
        [HttpPost]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Create([FromBody] ThamSoHeThongCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT /api/tham-so-he-thong/{id}  -- HR only
        [HttpPut("{id:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Update(int id, [FromBody] ThamSoHeThongUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _service.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE /api/tham-so-he-thong/{id}  -- HR only
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
