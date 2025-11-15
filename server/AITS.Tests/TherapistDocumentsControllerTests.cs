using AITS.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;

namespace AITS.Tests;

public class TherapistDocumentsControllerTests
{
    private static (AppDbContext Context, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole> RoleManager) BuildManagers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        var identityOptions = new IdentityOptions();
        identityOptions.Password.RequireDigit = true;
        identityOptions.Password.RequireLowercase = true;
        identityOptions.Password.RequireUppercase = true;
        identityOptions.Password.RequireNonAlphanumeric = false;
        identityOptions.Password.RequiredLength = 6;

        var userStore = new UserStore<ApplicationUser>(context);
        var roleStore = new RoleStore<IdentityRole>(context);

        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(identityOptions),
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
            new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var roleManager = new RoleManager<IdentityRole>(
            roleStore,
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);

        _ = roleManager.CreateAsync(new IdentityRole(Roles.Terapeuta)).Result;
        _ = roleManager.CreateAsync(new IdentityRole(Roles.TerapeutaFreeAccess)).Result;
        _ = roleManager.CreateAsync(new IdentityRole(Roles.Administrator)).Result;

        return (context, userManager, roleManager);
    }

    private static TherapistDocumentsController CreateControllerWithUser(AppDbContext context, ApplicationUser user, string role)
    {
        var controller = new TherapistDocumentsController(context);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    private static IFormFile CreateFormFile(string fileName, string content, string contentType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task CreateDocument_ShouldCreateDocument()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var file = CreateFormFile("test.pdf", "Test content", "application/pdf");
        var request = new TherapistDocumentsController.CreateTherapistDocumentRequest("Opis dokumentu", file);

        var result = await controller.Create(request, CancellationToken.None) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, (result!.StatusCode ?? 0));

        var document = await context.TherapistDocuments.FirstOrDefaultAsync(d => d.TherapistId == user.Id);
        Assert.NotNull(document);
        Assert.Equal("Opis dokumentu", document!.Description);
        Assert.Equal("test.pdf", document.FileName);
        Assert.Equal("application/pdf", document.ContentType);
    }

    [Fact]
    public async Task GetAllDocuments_ShouldReturnDocuments()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);

        var document = new TherapistDocument
        {
            TherapistId = user.Id,
            Description = "Test document",
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            FileContent = Encoding.UTF8.GetBytes("Test content"),
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistDocuments.Add(document);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var result = await controller.GetAll(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ShouldReturnDocument()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);

        var document = new TherapistDocument
        {
            TherapistId = user.Id,
            Description = "Test document",
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            FileContent = Encoding.UTF8.GetBytes("Test content"),
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistDocuments.Add(document);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var result = await controller.Get(document.Id, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateDocument_ShouldUpdateDescription()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);

        var document = new TherapistDocument
        {
            TherapistId = user.Id,
            Description = "Old description",
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            FileContent = Encoding.UTF8.GetBytes("Test content"),
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistDocuments.Add(document);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var request = new TherapistDocumentsController.UpdateTherapistDocumentRequest("New description");
        var result = await controller.Update(document.Id, request, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);

        var updatedDocument = await context.TherapistDocuments.FirstOrDefaultAsync(d => d.Id == document.Id);
        Assert.NotNull(updatedDocument);
        Assert.Equal("New description", updatedDocument!.Description);
        Assert.NotNull(updatedDocument.UpdatedAt);
    }

    [Fact]
    public async Task DeleteDocument_ShouldDeleteDocument()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);

        var document = new TherapistDocument
        {
            TherapistId = user.Id,
            Description = "Test document",
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            FileContent = Encoding.UTF8.GetBytes("Test content"),
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistDocuments.Add(document);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var result = await controller.Delete(document.Id, CancellationToken.None) as NoContentResult;
        Assert.NotNull(result);
        Assert.Equal(204, result!.StatusCode);

        var deletedDocument = await context.TherapistDocuments.FirstOrDefaultAsync(d => d.Id == document.Id);
        Assert.Null(deletedDocument);
    }
}


