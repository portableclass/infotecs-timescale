using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeScaleAPI.Data;

namespace TimeScaleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResultsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("filter")]
    public async Task<IActionResult> FilterResults(
        [FromQuery] string? fileName,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] double? minAvgValue,
        [FromQuery] double? maxAvgValue,
        [FromQuery] double? minAvgExecutionTime,
        [FromQuery] double? maxAvgExecutionTime)
    {
        var query = _context.Results.AsQueryable();

        if (!string.IsNullOrEmpty(fileName))
        {
            query = query.Where(r => r.FileName.Contains(fileName));
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.FirstOperationTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.FirstOperationTime <= endDate.Value);
        }

        if (minAvgValue.HasValue)
        {
            query = query.Where(r => r.AvgIndicatorValue >= minAvgValue.Value);
        }

        if (maxAvgValue.HasValue)
        {
            query = query.Where(r => r.AvgIndicatorValue <= maxAvgValue.Value);
        }

        if (minAvgExecutionTime.HasValue)
        {
            query = query.Where(r => r.AvgExecutionTime >= minAvgExecutionTime.Value);
        }

        if (maxAvgExecutionTime.HasValue)
        {
            query = query.Where(r => r.AvgExecutionTime <= maxAvgExecutionTime.Value);
        }

        var results = await query.ToListAsync();
        return Ok(results);
    }

    [HttpGet("last/{fileName}")]
    public async Task<IActionResult> GetLastValues(string fileName)
    {
        var values = await _context.Values
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Timestamp)
            .Take(10)
            .ToListAsync();

        return Ok(values);
    }
}
