using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public class RecognizeFoodDto
{
    [Required] public IFormFile File { get; set; } = null!;
}