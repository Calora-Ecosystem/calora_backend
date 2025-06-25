using Core.Services;
using Microsoft.AspNetCore.Mvc;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserService userService) : AuthorizedController
{
}