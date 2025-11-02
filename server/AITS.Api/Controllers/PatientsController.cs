using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Terapeuta + "," + Roles.TerapeutaFreeAccess + "," + Roles.Administrator)]
public sealed class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patients = await _db.Patients
            .Where(p => p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Phone,
                p.DateOfBirth,
                p.Gender,
                p.City,
                p.CreatedAt
            })
            .ToListAsync();
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _db.Patients
            .Where(p => p.Id == id && (p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator)))
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Phone,
                p.DateOfBirth,
                p.Gender,
                p.Pesel,
                p.Street,
                p.StreetNumber,
                p.ApartmentNumber,
                p.City,
                p.PostalCode,
                p.Country,
                p.Notes,
                p.CreatedAt,
                SessionsCount = p.Sessions.Count
            })
            .FirstOrDefaultAsync();
        
        if (patient is null) return NotFound();
        return Ok(patient);
    }

    public sealed record CreatePatientRequest(
        string FirstName, 
        string LastName, 
        string Email, 
        string? Phone, 
        DateTime? DateOfBirth,
        string? Gender,
        string? Pesel,
        string? Street,
        string? StreetNumber,
        string? ApartmentNumber,
        string? City,
        string? PostalCode,
        string? Country,
        string? Notes);
        
    public sealed record UpdatePatientRequest(
        string FirstName, 
        string LastName, 
        string Email, 
        string? Phone,
        DateTime? DateOfBirth,
        string? Gender,
        string? Pesel,
        string? Street,
        string? StreetNumber,
        string? ApartmentNumber,
        string? City,
        string? PostalCode,
        string? Country,
        string? Notes);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Pesel = request.Pesel,
            Street = request.Street,
            StreetNumber = request.StreetNumber,
            ApartmentNumber = request.ApartmentNumber,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country ?? "Polska",
            Notes = request.Notes,
            CreatedByUserId = userId
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = patient.Id }, new { patient.Id, patient.FirstName, patient.LastName, patient.Email });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == id && (p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator)));
        
        if (patient is null) return NotFound();
        
        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.Email = request.Email;
        patient.Phone = request.Phone;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Pesel = request.Pesel;
        patient.Street = request.Street;
        patient.StreetNumber = request.StreetNumber;
        patient.ApartmentNumber = request.ApartmentNumber;
        patient.City = request.City;
        patient.PostalCode = request.PostalCode;
        patient.Country = request.Country ?? "Polska";
        patient.Notes = request.Notes;
        
        await _db.SaveChangesAsync();
        return Ok(new { patient.Id, patient.FirstName, patient.LastName, patient.Email });
    }
}

