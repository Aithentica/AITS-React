using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class I18nController : ControllerBase
{
    private readonly AppDbContext _db;
    public I18nController(AppDbContext db) => _db = db;

    [HttpGet("{culture}")]
    public async Task<IActionResult> Get(string culture = "pl")
    {
        var items = await _db.Translations
            .Where(t => t.Culture == culture)
            .ToDictionaryAsync(t => t.Key, t => t.Value);
        return Ok(items);
    }
}




