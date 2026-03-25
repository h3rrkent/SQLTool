using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace SQLTool.Pages;

[AllowAnonymous]
public class LoginModel(IConfiguration configuration) : PageModel
{
    public string? Error { get; private set; }
    public string ReturnUrl { get; private set; } = "/";

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, string? username, string? password)
    {
        ReturnUrl = returnUrl ?? "/";

        var users = configuration.GetSection("Users").Get<List<UserConfig>>() ?? [];
        var hasher = new PasswordHasher<string>();

        // Find user by name first — prevents timing-based username enumeration
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        // Always run hash verification (constant-time) even when user not found
        var hashToVerify = user?.PasswordHash ?? hasher.HashPassword("", "dummy");
        var verifyResult = hasher.VerifyHashedPassword(username ?? "", hashToVerify, password ?? "");

        if (user is null || verifyResult == PasswordVerificationResult.Failed)
        {
            Error = "Invalid username or password.";
            return Page();
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Url.IsLocalUrl(ReturnUrl) ? Redirect(ReturnUrl) : Redirect("/");
    }
}

public record UserConfig(string Username, string PasswordHash, List<string> Roles);
