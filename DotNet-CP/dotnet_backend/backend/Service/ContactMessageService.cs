using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;

namespace ComputerSeekho.Service;

/// <summary>
/// BRD section 5: "the data will be saved in database and appropriate mail
/// will be sent to predefined mail-id".
///
/// Note the direction. An enquiry confirmation emails the *enquirer*; this
/// notifies the *institute*. The two are easy to confuse and do opposite
/// things.
/// </summary>
public class ContactMessageService : IContactMessageService
{
    private readonly IGenericRepository<ContactMessage> _repository;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactMessageService> _logger;
    private readonly string _notificationEmail;

    public ContactMessageService(
        IGenericRepository<ContactMessage> repository,
        IMapper mapper,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ContactMessageService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _notificationEmail = configuration["ContactNotificationEmail"] ?? "training@vita.com";
    }

    public async Task<ContactMessageDto> SubmitAsync(ContactMessageRequest request, CancellationToken ct = default)
    {
        var message = _mapper.Map<ContactMessage>(request);
        message.CreatedAt = DateTime.UtcNow;

        // Stored first, notified second — and that order is the whole point.
        //
        // The database row is the record; the email is only a notification.
        // Reversed, a mail outage would mean an enquiry from a prospective
        // student vanishes with no trace of it ever having arrived.
        var saved = await _repository.AddAsync(message, ct);

        await _emailService.SendAsync(
            _notificationEmail,
            $"New website message from {saved.Name}",
            $"""
             A message was submitted through the Get in Touch form.

             From  : {saved.Name}
             Email : {saved.Email}

             {saved.Message}

             Received: {saved.CreatedAt:dd MMM yyyy, HH:mm} UTC
             View it under Contact Messages in the admin panel.
             """,
            ct);

        return _mapper.Map<ContactMessageDto>(saved);
    }

    public async Task<IEnumerable<ContactMessageDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _repository.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<ContactMessageDto>>(rows.OrderByDescending(m => m.CreatedAt));
    }

    public async Task<bool> MarkReadAsync(int id, CancellationToken ct = default)
    {
        var message = await _repository.GetByIdAsync(id, ct);
        if (message is null) return false;

        message.IsRead = true;
        await _repository.UpdateAsync(message, ct);
        return true;
    }
}
