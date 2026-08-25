using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;

namespace ComputerSeekho.AutoMapperProfiles;

/// <summary>
/// Requirement 10 — entity to DTO mapping in one place.
///
/// Without this every service would hand-write a toDto() method, which is
/// where the Java backend spends a surprising amount of its code. The value
/// isn't saving keystrokes so much as having one file to look at when the
/// API shape and the table shape diverge.
///
/// Only the genuinely ambiguous members are configured. AutoMapper matches
/// same-named properties by convention, so listing the obvious ones would
/// just be noise that goes stale.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // --- Staff --------------------------------------------------------
        CreateMap<Staff, StaffDto>()
            // The enum has to reach the client as the string React compares
            // against in ProtectedRoute ("Admin", "Counselor", ...). Left
            // alone AutoMapper would map the enum to its integer ordinal and
            // every role check on the frontend would silently fail.
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<StaffCreateRequest, Staff>()
            // Role arrives as a string and must be parsed back.
            .ForMember(d => d.Role, o => o.MapFrom(s => ParseRole(s.Role)))
            // Never mapped from the request. The hash is set by the service
            // after BCrypt runs; letting AutoMapper near it would allow a
            // client-supplied value to land in the column.
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.StaffId, o => o.Ignore())
            .ForMember(d => d.CourseStaff, o => o.Ignore())
            .ForMember(d => d.Inquiries, o => o.Ignore());

        // --- Course -------------------------------------------------------
        CreateMap<Course, CourseDto>()
            .ForMember(d => d.Level, o => o.MapFrom(s => s.Level.ToString()))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            // Resolved from course_staff by the service, not by a mapping.
            .ForMember(d => d.PrimaryFacultyId, o => o.Ignore())
            .ForMember(d => d.PrimaryFacultyName, o => o.Ignore());

        CreateMap<CourseCreateRequest, Course>()
            .ForMember(d => d.Level, o => o.MapFrom(s => ParseLevel(s.Level)))
            .ForMember(d => d.CourseId, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.CourseStaff, o => o.Ignore());

        // --- Content and master tables ------------------------------------
        //
        // Every one of these is a straight name-for-name match, so AutoMapper
        // needs no configuration beyond being told the pair exists. Listing
        // members here would only be noise that goes stale.
        //
        // ReverseMap is deliberately not used: the request types have fewer
        // members than the DTOs on purpose, and a reversed map would happily
        // let a client set an id or a created-at timestamp.

        CreateMap<CourseCategory, CourseCategoryDto>();
        CreateMap<CourseCategoryRequest, CourseCategory>()
            .ForMember(d => d.CategoryId, o => o.Ignore())
            .ForMember(d => d.Courses, o => o.Ignore());

        CreateMap<Recruiter, RecruiterDto>();
        CreateMap<RecruiterRequest, Recruiter>()
            .ForMember(d => d.RecruiterId, o => o.Ignore());

        CreateMap<ClosureReason, ClosureReasonDto>();
        CreateMap<ClosureReasonRequest, ClosureReason>()
            .ForMember(d => d.ReasonId, o => o.Ignore());

        CreateMap<Banner, BannerDto>();
        CreateMap<BannerRequest, Banner>()
            .ForMember(d => d.BannerId, o => o.Ignore());

        CreateMap<Announcement, AnnouncementDto>();
        CreateMap<AnnouncementRequest, Announcement>()
            .ForMember(d => d.AnnouncementId, o => o.Ignore())
            // Set by the database default on insert, and must survive an
            // update untouched — mapping it would stamp "now" onto every edit.
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        CreateMap<Testimonial, TestimonialDto>();
        CreateMap<TestimonialRequest, Testimonial>()
            .ForMember(d => d.TestimonialId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        CreateMap<NewsEvent, NewsEventDto>();
        CreateMap<NewsEventRequest, NewsEvent>()
            .ForMember(d => d.NewsId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        // --- Enquiries and follow-ups -------------------------------------
        //
        // Both DTOs carry members resolved from other tables, so the ones
        // that cannot be mapped are ignored here and populated by the
        // service. Leaving them unconfigured would leave AutoMapper unable
        // to satisfy them.

        CreateMap<Inquiry, InquiryDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => InquiryStatusText(s.Status)))
            .ForMember(d => d.CourseName, o => o.MapFrom(s => s.Course != null ? s.Course.Name : null))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.Name : null))
            .ForMember(d => d.ClosureReasonText,
                o => o.MapFrom(s => s.ClosureReason != null ? s.ClosureReason.ReasonText : null))
            // Resolved from followups by the service.
            .ForMember(d => d.NextFollowupDate, o => o.Ignore());

        CreateMap<Followup, FollowupDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => FollowupStatusText(s.Status)))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.Name : null))
            .ForMember(d => d.EnquirerName, o => o.MapFrom(s => s.Inquiry != null ? s.Inquiry.EnquirerName : null))
            .ForMember(d => d.Phone, o => o.MapFrom(s => s.Inquiry != null ? s.Inquiry.Phone : null))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Inquiry != null ? s.Inquiry.Email : null))
            .ForMember(d => d.CourseName,
                o => o.MapFrom(s => s.Inquiry != null && s.Inquiry.Course != null ? s.Inquiry.Course.Name : null))
            .ForMember(d => d.InquiryStatus,
                o => o.MapFrom(s => s.Inquiry != null ? InquiryStatusText(s.Inquiry.Status) : null))
            // Computed against today, which a mapping has no business knowing.
            .ForMember(d => d.DaysOverdue, o => o.Ignore());

        // --- Registration core --------------------------------------------
        //
        // Every member resolved from another table is ignored here and
        // populated by the service that owns the query.

        CreateMap<Student, StudentDto>()
            .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender == null ? null : s.Gender.ToString()))
            .ForMember(d => d.EnrollmentId, o => o.Ignore())
            .ForMember(d => d.BatchId, o => o.Ignore())
            .ForMember(d => d.BatchName, o => o.Ignore())
            .ForMember(d => d.CourseName, o => o.Ignore());

        CreateMap<StudentDetailsRequest, Student>()
            .ForMember(d => d.Gender, o => o.MapFrom(s => ParseGender(s.Gender)))
            .ForMember(d => d.StudentId, o => o.Ignore())
            .ForMember(d => d.InquiryId, o => o.Ignore())
            .ForMember(d => d.RegDate, o => o.Ignore())
            .ForMember(d => d.Inquiry, o => o.Ignore());

        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.PaymentMode, o => o.MapFrom(s => PaymentModeText(s.PaymentMode)))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()))
            .ForMember(d => d.StudentName,
                o => o.MapFrom(s => s.Student != null ? s.Student.FirstName + " " + s.Student.LastName : null));

        CreateMap<Batch, BatchDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CourseName, o => o.MapFrom(s => s.Course != null ? s.Course.Name : null))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.Course != null ? (int?)s.Course.CategoryId : null))
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Course != null && s.Course.Category != null ? s.Course.Category.Name : null))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.Name : null))
            // Both counts are derived, never read from a column — the
            // service fills them.
            .ForMember(d => d.CurrentCount, o => o.Ignore())
            .ForMember(d => d.PlacedCount, o => o.Ignore());

        CreateMap<BatchRequest, Batch>()
            .ForMember(d => d.Status, o => o.MapFrom(s => ParseBatchStatus(s.Status)))
            .ForMember(d => d.BatchId, o => o.Ignore())
            .ForMember(d => d.CurrentCount, o => o.Ignore())
            .ForMember(d => d.Course, o => o.Ignore())
            .ForMember(d => d.Staff, o => o.Ignore());

        // --- Placements and albums ----------------------------------------

        CreateMap<PlacementDrive, PlacementDriveDto>()
            .ForMember(d => d.DriveMode, o => o.MapFrom(s => s.DriveMode.ToString()))
            .ForMember(d => d.DriveStatus, o => o.MapFrom(s => s.DriveStatus.ToString()))
            .ForMember(d => d.RecruiterCompanyName,
                o => o.MapFrom(s => s.Recruiter != null ? s.Recruiter.CompanyName : null))
            .ForMember(d => d.CourseName, o => o.MapFrom(s => s.Course != null ? s.Course.Name : null));

        CreateMap<PlacementDriveRequest, PlacementDrive>()
            .ForMember(d => d.DriveMode, o => o.MapFrom(s => ParseDriveMode(s.DriveMode)))
            .ForMember(d => d.DriveStatus, o => o.MapFrom(s => ParseDriveStatus(s.DriveStatus)))
            .ForMember(d => d.DriveId, o => o.Ignore())
            .ForMember(d => d.Recruiter, o => o.Ignore())
            .ForMember(d => d.Course, o => o.Ignore());

        CreateMap<PlacementRecord, PlacementRecordDto>()
            .ForMember(d => d.StudentName,
                o => o.MapFrom(s => s.Student != null ? s.Student.FirstName + " " + s.Student.LastName : null))
            .ForMember(d => d.StudentPhotoUrl, o => o.MapFrom(s => s.Student != null ? s.Student.PhotoUrl : null))
            .ForMember(d => d.BatchName, o => o.MapFrom(s => s.Batch != null ? s.Batch.BatchName : null))
            .ForMember(d => d.RecruiterCompanyName,
                o => o.MapFrom(s => s.Recruiter != null ? s.Recruiter.CompanyName : null))
            .ForMember(d => d.RecruiterLogoUrl, o => o.MapFrom(s => s.Recruiter != null ? s.Recruiter.LogoUrl : null));

        CreateMap<PlacementRecordRequest, PlacementRecord>()
            .ForMember(d => d.PlacementId, o => o.Ignore())
            .ForMember(d => d.Student, o => o.Ignore())
            .ForMember(d => d.Batch, o => o.Ignore())
            .ForMember(d => d.Recruiter, o => o.Ignore());

        CreateMap<BatchAlbumImage, BatchAlbumImageDto>()
            // Whether an image is the cover depends on the album, which the
            // image does not know about. Set by the service.
            .ForMember(d => d.IsCover, o => o.Ignore());

        CreateMap<ContactMessage, ContactMessageDto>();
        CreateMap<ContactMessageRequest, ContactMessage>()
            .ForMember(d => d.MessageId, o => o.Ignore())
            .ForMember(d => d.IsRead, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        CreateMap<GalleryImage, GalleryImageDto>();
        CreateMap<GalleryImageRequest, GalleryImage>()
            .ForMember(d => d.ImageId, o => o.Ignore())
            // Defaulted on insert; an edit must not silently re-date the photo.
            .ForMember(d => d.UploadDate, o => o.Ignore());
    }

    /// <summary>
    /// Tolerant of case, strict about validity. An unrecognised role is a
    /// bad request, not a silent default to the least-privileged option —
    /// quietly downgrading is how someone ends up wondering why the account
    /// they created cannot log in anywhere.
    /// </summary>
    private static StaffRole ParseRole(string? value) =>
        Enum.TryParse<StaffRole>(value, ignoreCase: true, out var role)
            ? role
            : throw new ArgumentException($"Unknown role: '{value}'");

    /// <summary>
    /// The database spelling of an enquiry status. React compares against
    /// these exact strings, so "InFollowup" would silently break every
    /// status check on the frontend.
    /// </summary>
    public static string InquiryStatusText(InquiryStatus status) => status switch
    {
        InquiryStatus.InFollowup => "In-Followup",
        InquiryStatus.NotInterested => "Not Interested",
        _ => status.ToString()
    };

    private static DriveMode ParseDriveMode(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Models.DriveMode.Offline
            : Enum.TryParse<DriveMode>(value, true, out var m)
                ? m
                : throw new ArgumentException($"Unknown drive mode: '{value}'");

    private static DriveStatus ParseDriveStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Models.DriveStatus.Scheduled
            : Enum.TryParse<DriveStatus>(value, true, out var s)
                ? s
                : throw new ArgumentException($"Unknown drive status: '{value}'");

    public static string PaymentModeText(PaymentMode mode) => mode switch
    {
        PaymentMode.BankTransfer => "Bank Transfer",
        _ => mode.ToString()
    };

    private static Gender? ParseGender(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<Gender>(value, true, out var g)
                ? g
                : throw new ArgumentException($"Unknown gender: '{value}'");

    private static BatchStatus ParseBatchStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? BatchStatus.Upcoming
            : Enum.TryParse<BatchStatus>(value, true, out var s)
                ? s
                : throw new ArgumentException($"Unknown batch status: '{value}'");

    public static string FollowupStatusText(FollowupStatus status) => status switch
    {
        FollowupStatus.NoResponse => "No Response",
        _ => status.ToString()
    };

    private static CourseLevel ParseLevel(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? CourseLevel.Beginner
            : Enum.TryParse<CourseLevel>(value, ignoreCase: true, out var level)
                ? level
                : throw new ArgumentException($"Unknown level: '{value}'");
}
