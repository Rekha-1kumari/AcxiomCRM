using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomersApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: /api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Customer> query = _context.Customers;
        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(c => c.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.Phone.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var list = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        var dtos = list.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            AssignedTo = c.AssignedTo
        });

        return Ok(dtos);
    }

    // GET: /api/customers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var c = await _context.Customers.FindAsync(id);
        if (c == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        if (isSalesExec && c.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var dto = new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            AssignedTo = c.AssignedTo
        };

        return Ok(dto);
    }

    // POST: /api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CustomerCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Server-side duplicate validation
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new { message = "A customer with this email address already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
        {
            return Conflict(new { message = "A customer with this phone number already exists." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var customer = new Customer
        {
            CustomerCode = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1),
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : dto.AssignedTo
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null,
            $"API Created Customer: {customer.CustomerName}");

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            AssignedTo = customer.AssignedTo
        };

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
    }

    // PUT: /api/customers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("SalesExecutive") && customer.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new { message = "Another customer with this email address already exists." });
        }

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = dto.Email.Trim().ToLower();
        customer.Phone = dto.Phone.Trim();
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;

        if (!User.IsInRole("SalesExecutive") && dto.AssignedTo != null)
        {
            customer.AssignedTo = dto.AssignedTo;
        }

        _context.Update(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Customer", id.ToString(), null, $"API Updated Customer: {customer.CustomerName}");

        return Ok(new { message = "Customer updated successfully." });
    }

    // DELETE: /api/customers/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        customer.Status = "Inactive";
        _context.Update(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Customer", id.ToString(), null, $"API Deactivated Customer: {customer.CustomerName}");

        return Ok(new { message = $"Customer '{customer.CustomerName}' deactivated." });
    }
}
