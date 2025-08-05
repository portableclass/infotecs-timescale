using System.ComponentModel.DataAnnotations;

namespace TimeScaleAPI.Models;

public class Result
{
    public int Id { get; set; }
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    public DateTime FirstOperationTime { get; set; }
    public double TotalTimeSpan { get; set; }
    public double AvgExecutionTime { get; set; }
    public double AvgIndicatorValue { get; set; }
    public double MedianIndicatorValue { get; set; }
    public double MaxIndicatorValue { get; set; }
    public double MinIndicatorValue { get; set; }
}