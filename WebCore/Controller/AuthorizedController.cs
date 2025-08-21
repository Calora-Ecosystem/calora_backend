using BRB.Core.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebCore.Helpers;

namespace WebCore.Controller;

[Authorize]
[ApiController, Route("[controller]/[action]")]
public abstract class AuthorizedController : ControllerBase
{
     private long? _userId;
     // private long? _roleId;
     // private long? _deviceId;

     /// <summary>
     /// User ID
     /// </summary>
     /// <exception cref="NotFoundException"></exception>
     public long UserId
     {
          get
          {
               if (_userId.HasValue)
                    return _userId.Value;

               _userId = this.HttpContext.ParseRequired<long>("user-id");

               return _userId.Value;
          }
     }

     /// <summary>
     /// Has Authorized
     /// </summary>
     protected bool HasAuthorized => this.User.Identity is not null && this.User.Identity!.IsAuthenticated;

     /// <summary>
     /// User Role ID
     /// </summary>
     /// <exception cref="NotFoundException"></exception>
     // public long RoleId
     // {
     //      get
     //      {
     //           if (_roleId.HasValue)
     //                return _roleId.Value;
     //
     //           _roleId = this.HttpContext.ParseRequired();
     //
     //           return _roleId.Value;
     //      }
     // }

     /// <summary>
     /// User Device ID
     /// </summary>
     /// <exception cref="NotFoundException"></exception>
     // public long DeviceId
     // {
     //      get
     //      {
     //           if (_deviceId.HasValue)
     //                return _deviceId.Value;
     //
     //           _deviceId = this.HttpContext.ParseRequired<long>(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.);
     //
     //           return _deviceId.Value;
     //      }
     // }
     
     
}