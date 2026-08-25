using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>Registered students.</summary>
[ApiController]
[Route("students")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _students;
    private readonly IPaymentService _payments;

    public StudentController(IStudentService students, IPaymentService payments)
    {
        _students = students;
        _payments = payments;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll(CancellationToken ct) =>
        Ok(await _students.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _students.GetByIdAsync(id, ct);
        return dto is null ? NotFound(new ApiError(404, $"Student {id} was not found")) : Ok(dto);
    }

    /// <summary>The batch roster.</summary>
    [HttpGet("by-batch/{batchId:int}")]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetByBatch(int batchId, CancellationToken ct) =>
        Ok(await _students.GetByBatchAsync(batchId, ct));

    [HttpGet("{id:int}/payments")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(int id, CancellationToken ct) =>
        Ok(await _payments.GetForStudentAsync(id, ct));
}
