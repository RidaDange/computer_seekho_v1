using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ComputerSeekho.Models;

/// <summary>
/// EF Core context over the existing `computerseekho` schema.
///
/// This is database-first: the schema already exists and is owned by
/// db/schema.sql, shared with the Java backend. Nothing here should ever
/// create or alter a table — if the entities and the database disagree, the
/// entities are wrong. That mirrors Hibernate running with
/// ddl-auto=validate on the Java side.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseStaff> CourseStaff => Set<CourseStaff>();
    public DbSet<ClosureReason> ClosureReasons => Set<ClosureReason>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    // Content and master tables behind the public site.
    public DbSet<Recruiter> Recruiters => Set<Recruiter>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<NewsEvent> NewsEvents => Set<NewsEvent>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();

    public DbSet<Followup> Followups => Set<Followup>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    // The transactional core.
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Placements and albums.
    public DbSet<PlacementDrive> PlacementDrives => Set<PlacementDrive>();
    public DbSet<PlacementRecord> PlacementRecords => Set<PlacementRecord>();
    public DbSet<BatchAlbum> BatchAlbums => Set<BatchAlbum>();
    public DbSet<BatchAlbumImage> BatchAlbumImages => Set<BatchAlbumImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Enum handling ------------------------------------------------
        //
        // MySQL stores these as ENUM strings, and two of the values contain
        // characters a C# identifier cannot: 'In-Followup' and
        // 'Not Interested'. Left to itself EF Core would either store the
        // integer ordinal (silently breaking every row Java wrote) or the
        // C# name (rejected by the column). Explicit converters are the only
        // correct answer, and they are the direct equivalent of Java's
        // InquiryStatusConverter and friends.

        modelBuilder.Entity<Staff>()
            .Property(s => s.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Course>()
            .Property(c => c.Level)
            .HasConversion<string>();

        modelBuilder.Entity<Inquiry>()
            .Property(i => i.Status)
            .HasConversion(new ValueConverter<InquiryStatus, string>(
                v => InquiryStatusToDb(v),
                v => InquiryStatusFromDb(v)));

        // 'No Response' contains a space, so the same explicit conversion
        // the enquiry status needs applies here.
        modelBuilder.Entity<Followup>()
            .Property(f => f.Status)
            .HasConversion(new ValueConverter<FollowupStatus, string>(
                v => FollowupStatusToDb(v),
                v => FollowupStatusFromDb(v)));

        modelBuilder.Entity<Batch>().Property(b => b.Status).HasConversion<string>();
        modelBuilder.Entity<Enrollment>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<Student>().Property(s => s.Gender).HasConversion<string>();
        modelBuilder.Entity<Payment>().Property(p => p.PaymentStatus).HasConversion<string>();

        // 'Bank Transfer' contains a space, so this one needs the explicit
        // treatment rather than HasConversion<string>().
        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentMode)
            .HasConversion(new ValueConverter<PaymentMode, string>(
                v => PaymentModeToDb(v),
                v => PaymentModeFromDb(v)));

        modelBuilder.Entity<PlacementDrive>().Property(d => d.DriveMode).HasConversion<string>();
        modelBuilder.Entity<PlacementDrive>().Property(d => d.DriveStatus).HasConversion<string>();

        // --- Relationships ------------------------------------------------
        //
        // Delete behaviour is stated explicitly to match the foreign keys in
        // schema.sql. EF Core's default for a required relationship is
        // Cascade, which would happily delete every enquiry belonging to a
        // course — the database itself uses RESTRICT there and would refuse,
        // so leaving the default in place just moves the failure later.

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CourseStaff>()
            .HasOne(cs => cs.Course)
            .WithMany(c => c.CourseStaff)
            .HasForeignKey(cs => cs.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CourseStaff>()
            .HasOne(cs => cs.Staff)
            .WithMany(s => s.CourseStaff)
            .HasForeignKey(cs => cs.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mirrors uk_course_staff — a person teaches a course once.
        modelBuilder.Entity<CourseStaff>()
            .HasIndex(cs => new { cs.CourseId, cs.StaffId })
            .IsUnique();

        modelBuilder.Entity<Inquiry>()
            .HasOne(i => i.Course)
            .WithMany()
            .HasForeignKey(i => i.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inquiry>()
            .HasOne(i => i.Staff)
            .WithMany(s => s.Inquiries)
            .HasForeignKey(i => i.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Inquiry>()
            .HasOne(i => i.ClosureReason)
            .WithMany()
            .HasForeignKey(i => i.ClosureReasonId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Followup>()
            .HasOne(f => f.Inquiry)
            .WithMany()
            .HasForeignKey(f => f.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Followup>()
            .HasOne(f => f.Staff)
            .WithMany()
            .HasForeignKey(f => f.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Course).WithMany()
            .HasForeignKey(b => b.CourseId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Staff).WithMany()
            .HasForeignKey(b => b.StaffId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Inquiry).WithMany()
            .HasForeignKey(s => s.InquiryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student).WithMany()
            .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Batch).WithMany()
            .HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Student).WithMany()
            .HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Enrollment).WithMany()
            .HasForeignKey(p => p.EnrollmentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>().HasIndex(p => p.ReceiptNo).IsUnique();
        modelBuilder.Entity<Student>().HasIndex(s => s.Email).IsUnique();

        modelBuilder.Entity<PlacementDrive>()
            .HasOne(d => d.Recruiter).WithMany()
            .HasForeignKey(d => d.RecruiterId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlacementDrive>()
            .HasOne(d => d.Course).WithMany()
            .HasForeignKey(d => d.CourseId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PlacementRecord>()
            .HasOne(r => r.Student).WithMany()
            .HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlacementRecord>()
            .HasOne(r => r.Batch).WithMany()
            .HasForeignKey(r => r.BatchId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PlacementRecord>()
            .HasOne(r => r.Recruiter).WithMany()
            .HasForeignKey(r => r.RecruiterId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatchAlbum>()
            .HasOne(a => a.Batch).WithMany()
            .HasForeignKey(a => a.BatchId).OnDelete(DeleteBehavior.Cascade);

        // One album per batch — the constraint that makes "the batch's
        // album" a meaningful phrase.
        modelBuilder.Entity<BatchAlbum>().HasIndex(a => a.BatchId).IsUnique();

        modelBuilder.Entity<BatchAlbumImage>()
            .HasOne(i => i.Album).WithMany()
            .HasForeignKey(i => i.AlbumId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recruiter>().HasIndex(r => r.CompanyName).IsUnique();

        modelBuilder.Entity<Staff>().HasIndex(s => s.Username).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(s => s.Email).IsUnique();
    }

    private static string PaymentModeToDb(PaymentMode mode) => mode switch
    {
        PaymentMode.BankTransfer => "Bank Transfer",
        _ => mode.ToString()
    };

    private static PaymentMode PaymentModeFromDb(string value) => value switch
    {
        "Bank Transfer" => PaymentMode.BankTransfer,
        _ => Enum.Parse<PaymentMode>(value)
    };

    private static string FollowupStatusToDb(FollowupStatus status) => status switch
    {
        FollowupStatus.NoResponse => "No Response",
        _ => status.ToString()
    };

    private static FollowupStatus FollowupStatusFromDb(string value) => value switch
    {
        "No Response" => FollowupStatus.NoResponse,
        _ => Enum.Parse<FollowupStatus>(value)
    };

    private static string InquiryStatusToDb(InquiryStatus status) => status switch
    {
        InquiryStatus.InFollowup => "In-Followup",
        InquiryStatus.NotInterested => "Not Interested",
        _ => status.ToString()
    };

    private static InquiryStatus InquiryStatusFromDb(string value) => value switch
    {
        "In-Followup" => InquiryStatus.InFollowup,
        "Not Interested" => InquiryStatus.NotInterested,
        _ => Enum.Parse<InquiryStatus>(value)
    };
}
