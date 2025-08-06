using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeScaleAPI.Models;

public class Value
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Range(typeof(DateTime), "2000-01-01", "2100-01-01",
        ErrorMessage = "Date must be between 2000-01-01 and current date")]
    [Column(TypeName = "timestamp with time zone")]
    public DateTime Timestamp { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Execution time must be >= 0")]
    public double ExecutionTime { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Indicator value must be >= 0")]
    public double IndicatorValue { get; set; }
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;
}
