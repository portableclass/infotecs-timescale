using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using TimeScaleAPI.Controllers;
using TimeScaleAPI.Data;

namespace TimeScaleAPI.UnitTests.Controllers;

public class CsvControllerTests
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<CsvController>> _loggerMock;
    private readonly CsvController _controller;

    public CsvControllerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<CsvController>>();
        _controller = new CsvController(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _connection.Close();
        _context.Values.RemoveRange(_context.Values);
        _context.Results.RemoveRange(_context.Results);
        _context.SaveChanges();
        _context.Dispose();
    }

    private FormFile CreateFormFile(string content, string fileName)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    [Fact]
    public async Task Upload_ValidCsv_ReturnsOk()
    {
        // Arrange
        var content = "Timestamp,ExecutionTime,IndicatorValue\n" +
                      "2023-01-01T12:00:00,1.5,10.2\n" +
                      "2023-01-01T12:01:00,2.3,15.7";
        var file = CreateFormFile(content, "test.csv");

        // Act
        var result = await _controller.Upload(file);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var values = await _context.Values.ToListAsync();
        values.Should().HaveCount(2);
    }

    [Fact]
    public async Task Upload_ValidCsv_CreatesCorrectResult()
    {
        // Arrange
        var content = "Timestamp,ExecutionTime,IndicatorValue\n" +
                       "2023-01-01T12:00:00,1.5,10.2\n" +
                       "2023-01-01T12:01:00,2.3,15.7";
        var file = CreateFormFile(content, "test.csv");

        // Act
        await _controller.Upload(file);

        // Assert
        var result = await _context.Results
            .FirstOrDefaultAsync(r => r.FileName == "test");

        result.Should().NotBeNull();
        result.TotalTimeSpan.Should().Be(60);
        result.FirstOperationTime.Should().Be(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        result.AvgExecutionTime.Should().BeApproximately(1.9, 0.01);
        result.AvgIndicatorValue.Should().BeApproximately(12.95, 0.01);
        result.MedianIndicatorValue.Should().BeApproximately(12.95, 0.01);
        result.MaxIndicatorValue.Should().Be(15.7);
        result.MinIndicatorValue.Should().Be(10.2);
    }

    [Fact]
    public async Task Upload_InvalidDate_ReturnsBadRequest()
    {
        // Arrange
        var content = "Timestamp,ExecutionTime,IndicatorValue\n" +
                      "1999-12-31T23:59:59,1.5,10.2"; // before 2000
        var file = CreateFormFile(content, "invalid.csv");

        // Act
        var result = await _controller.Upload(file);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Upload_InvalidData_RollsBackTransaction()
    {
        // Arrange
        var validContent = "Timestamp,ExecutionTime,IndicatorValue\n" +
                            "2023-01-01T12:00:00,1.5,10.2";
        var validFile = CreateFormFile(validContent, "valid.csv");
        await _controller.Upload(validFile);

        var invalidContent = "Timestamp,ExecutionTime,IndicatorValue\n" +
                             "1999-12-31T23:59:59,-1.5,-10.2";
        var invalidFile = CreateFormFile(invalidContent, "invalid.csv");

        // Act
        var result = await _controller.Upload(invalidFile);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        var values = await _context.Values.ToListAsync();
        values.Should().HaveCount(1);
        values[0].FileName.Should().Be("valid");
    }
}