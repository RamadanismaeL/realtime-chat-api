/*
*@author Ramadan Ismael
*/

using Microsoft.AspNetCore.Identity;

namespace server.src.Models;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? ProfileImage { get; set; }
}
