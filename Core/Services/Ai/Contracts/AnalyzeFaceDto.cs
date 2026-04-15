using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Ai.Contracts;

public record AnalyzeFaceDto
{
    /// <summary>
    /// Sog'lomlik foizi
    /// </summary>
    [SwaggerSchema("Sog'lomlik foizi: <percent>/100")]
    public int HealthPercent { get; set; }
    /// <summary>
    /// Yuzda toshmalar bor
    /// </summary>
    [SwaggerSchema("Yuzda toshmalar bor")]
    public double Rashes { get; set; }
    /// <summary>
    /// Ko’z osti qoraygan
    /// </summary>
    [SwaggerSchema("Ko’z osti qoraygan")]
    public double DarkEyes { get; set; }
    /// <summary>
    /// Energiya darajasi
    /// </summary>
    [SwaggerSchema("Energiya darajasi")]
    public double Energy { get; set; }
    /// <summary>
    /// Stress darajasi
    /// </summary>
    [SwaggerSchema("Stress darajasi")]
    public double Stress { get; set; }
    /// <summary>
    /// Uyqu darajasi
    /// </summary>
    [SwaggerSchema("Uyqu darajasi")]
    public double Sleep { get; set; }
}