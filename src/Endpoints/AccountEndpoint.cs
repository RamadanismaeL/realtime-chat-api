/*
*@author Ramadan Ismael
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.src.Common;
using server.src.DTOs;
using server.src.Extensions;
using server.src.Models;
using server.src.Services;

namespace server.src.Endpoints
{
    public static class AccountEndpoint
    {
        public static RouteGroupBuilder MapAccountEndpoint(this WebApplication app)
        {
            var group = app.MapGroup("/api/account").WithTags("account");

            group.MapPost("/register", async (HttpContext context, UserManager<AppUser> userManager, [FromForm] string fullName, [FromForm] string email, [FromForm] string password, [FromForm] string username, [FromForm] IFormFile? profileImage) =>
            {
                var userFromDb = await userManager.FindByEmailAsync(email);

                if(userFromDb is not null)
                {
                    return Results.BadRequest(Response<string>.Failure("User is already exist."));
                }

                // IMAGE
                // START
                if(profileImage is null)
                {
                    return Results.BadRequest(Response<string>.Failure("Profile image is required."));
                }

                var picture = await FileUpload.Upload(profileImage);

                picture = $"{context.Request.Scheme}://{context.Request.Host}/uploads/{picture}";

                // END

                var user = new AppUser{
                    Email = email,
                    FullName = fullName,
                    UserName = username,
                    //image
                    ProfileImage = picture
                };

                var result = await userManager.CreateAsync(user, password);

                if(!result.Succeeded)
                {
                    return Results.BadRequest(Response<string>.Failure(result.Errors.Select(x => x.Description).FirstOrDefault()!));
                }

                return Results.Ok(Response<string>.Success("", "User created successfully!"));
            }).DisableAntiforgery();


            // Config Token Service
            // START
            group.MapPost("/login", async (UserManager<AppUser> userManager, TokenService tokenService, LoginDto loginDto) =>
            {
                if(loginDto is null)
                {
                    return Results.BadRequest(Response<string>.Failure("Invalid login details!"));
                }

                var user = await userManager.FindByEmailAsync(loginDto.Email);

                if(user is null)
                {
                    return Results.BadRequest(Response<string>.Failure("User not found"));
                }

                var result = await userManager.CheckPasswordAsync(user!, loginDto.Password);

                if(!result)
                {
                    return Results.BadRequest(Response<string>.Failure("Invalid password!"));
                }

                var token = tokenService.GenerateToken(user.Id, user.UserName!);

                return Results.Ok(Response<string>.Success(token, "Login successfully!"));
            });

            //END

            group.MapGet("/me", async (HttpContext context, UserManager<AppUser> userManager) => {
                var currentLoggedInUserId = context.User.GetUserId();
                var currentLoggedInUser = await userManager.Users.SingleOrDefaultAsync(x => x.Id == currentLoggedInUserId.ToString());

                return Results.Ok(Response<AppUser>.Success(currentLoggedInUser!, "User fetched successfully."));
            }).RequireAuthorization();

            return group;
        }
    }
}