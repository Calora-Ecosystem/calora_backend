using Core.Brokers.DbContext;
using Core.Entities.Refs;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Refs;

[ApiController]
[Route("/ref/purpose")]
public class PurposeController(AppDbContext dbContext) : ReferenceControllerBase<Purpose>(dbContext)
{
}