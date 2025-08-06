using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TimeScaleAPI.Data;
using TimeScaleAPI.Models;

namespace TimeScaleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CsvController> _logger;

    public CsvController(AppDbContext context, ILogger<CsvController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // validation
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (Path.GetExtension(file.FileName).ToLower() != ".csv")
            return BadRequest("Only CSV files are allowed");

        // read CSV
        List<CsvRecord> records;

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
            });

            csv.Context.RegisterClassMap<CsvRecordMap>();
            records = csv.GetRecords<CsvRecord>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV parsing error");
            return BadRequest($"Error parsing CSV: {ex.Message}");
        }

        // validation records
        if (records.Count < 1 || records.Count > 10000)
            return BadRequest($"Invalid row count: {records.Count}. Must be between 1-10000");

        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
        var errors = new List<string>();
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = DateTime.UtcNow;

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var utcTimestamp = record.Timestamp.Kind == DateTimeKind.Utc
                ? record.Timestamp
                : new DateTime(record.Timestamp.Ticks, DateTimeKind.Utc);

            if (utcTimestamp < minDate || utcTimestamp > maxDate)
                errors.Add($"Row {i + 1}: Invalid date {record.Timestamp}. Date must be between 2000-01-01 and current date");

            if (record.ExecutionTime < 0)
                errors.Add($"Row {i + 1}: Execution time cannot be negative");

            if (record.IndicatorValue < 0)
                errors.Add($"Row {i + 1}: Indicator value cannot be negative");
        }

        if (errors.Any())
            return BadRequest(new { Errors = errors });

        // serializing data
        var values = records.Select(r => new Value
        {
            Timestamp = r.Timestamp.Kind == DateTimeKind.Utc
                ? r.Timestamp
                : new DateTime(r.Timestamp.Ticks, DateTimeKind.Utc),
            ExecutionTime = r.ExecutionTime,
            IndicatorValue = r.IndicatorValue,
            FileName = fileName
        }).ToList();

        // transaction to db
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // delete existing data
            await DeleteExistingFileData(fileName);

            // save values
            await _context.Values.AddRangeAsync(values);
            await _context.SaveChangesAsync();

            // calc and save res
            var result = CalculateResults(values, fileName);
            await _context.Results.AddAsync(result);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Ok(new { Message = "File processed successfully", FileName = fileName });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing file");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task DeleteExistingFileData(string fileName)
    {
        var existingValues = await _context.Values
            .Where(v => v.FileName == fileName)
            .ToListAsync();

        var existingResults = await _context.Results
            .Where(r => r.FileName == fileName)
            .ToListAsync();

        if (existingValues.Count != 0) _context.Values.RemoveRange(existingValues);
        if (existingResults.Count != 0) _context.Results.RemoveRange(existingResults);
        await _context.SaveChangesAsync();
    }

    private static Result CalculateResults(List<Value> values, string fileName)
    {
        var timestamps = values.Select(v => v.Timestamp).ToList();
        var indicatorValues = values.Select(v => v.IndicatorValue).ToList();
        var orderedValues = indicatorValues.OrderBy(v => v).ToList();

        return new Result
        {
            FileName = fileName,
            FirstOperationTime = timestamps.Min(),
            TotalTimeSpan = (timestamps.Max() - timestamps.Min()).TotalSeconds,
            AvgExecutionTime = values.Average(v => v.ExecutionTime),
            AvgIndicatorValue = indicatorValues.Average(),
            MedianIndicatorValue = CalculateMedian(orderedValues),
            MaxIndicatorValue = indicatorValues.Max(),
            MinIndicatorValue = indicatorValues.Min()
        };
    }

    private static double CalculateMedian(List<double> orderedValues)
    {
        int size = orderedValues.Count;
        int mid = size / 2;

        // if the number of numbers is even, the median will be the arithmetic mean of the two central numbers.
        return size % 2 != 0 ? orderedValues[mid] : (orderedValues[mid - 1] + orderedValues[mid]) / 2.0;
    }
}

public class CsvRecord
{
    public DateTime Timestamp { get; set; }
    public double ExecutionTime { get; set; }
    public double IndicatorValue { get; set; }
}

public sealed class CsvRecordMap : ClassMap<CsvRecord>
{
    public CsvRecordMap()
    {
        Map(m => m.Timestamp)
            .TypeConverterOption.DateTimeStyles(DateTimeStyles.AdjustToUniversal);

        Map(m => m.ExecutionTime);
        Map(m => m.IndicatorValue);
    }
}
