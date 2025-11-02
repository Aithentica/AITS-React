using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Xunit;

namespace AITS.Tests;

public class PatientsControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkResult_WhenPatientsExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var user = new ApplicationUser { Id = "test-user", UserName = "test@test.com", Email = "test@test.com" };
        db.Users.Add(user);
        db.Patients.Add(new Patient { Id = 1, FirstName = "Jan", LastName = "Kowalski", Email = "jan@test.com", CreatedByUserId = "test-user" });
        await db.SaveChangesAsync();

        var controller = new AITS.Api.Controllers.PatientsController(db);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, "Terapeuta") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var patients = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Single(patients);
    }
}




