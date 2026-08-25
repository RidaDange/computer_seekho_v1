namespace ComputerSeekho.Models;

/// <summary>
/// Roles a staff member can hold. The names match the MySQL ENUM in the
/// staff table exactly, and also the strings the React app compares against
/// in ProtectedRoute — so they cannot be renamed on a whim.
/// </summary>
public enum StaffRole
{
    Admin,
    Counselor,
    Faculty,
    Manager,
    Receptionist
}

/// <summary>
/// Course difficulty, mirroring the courses.level ENUM.
/// </summary>
public enum CourseLevel
{
    Beginner,
    Intermediate,
    Advanced
}

/// <summary>
/// Enquiry lifecycle.
///
/// Two of the database values contain characters a C# identifier cannot
/// hold: 'In-Followup' has a hyphen and 'Not Interested' a space. The names
/// below are the legal C# forms; AppDbContext converts between these and
/// the database strings, exactly as InquiryStatusConverter does on the Java
/// side. Relying on Enum.ToString() here would silently write "InFollowup"
/// into a column that only accepts "In-Followup".
/// </summary>
public enum InquiryStatus
{
    New,
    InFollowup,
    Converted,
    Lost,
    NotInterested
}

/// <summary>Follow-up call outcome. 'No Response' contains a space.</summary>
public enum FollowupStatus
{
    Pending,
    Done,
    NoResponse
}

/// <summary>Batch lifecycle.</summary>
public enum BatchStatus
{
    Upcoming,
    Ongoing,
    Completed,
    Cancelled
}

/// <summary>Enrolment lifecycle.</summary>
public enum EnrollmentStatus
{
    Active,
    Completed,
    Dropped
}

/// <summary>Payment mode. 'Bank Transfer' contains a space.</summary>
public enum PaymentMode
{
    Cash,
    UPI,
    Card,
    BankTransfer,
    Cheque
}

/// <summary>Payment outcome.</summary>
public enum PaymentStatus
{
    Success,
    Pending,
    Failed,
    Refunded
}

/// <summary>Student gender, nullable on the entity.</summary>
public enum Gender
{
    Male,
    Female,
    Other
}

/// <summary>How a placement drive is run.</summary>
public enum DriveMode
{
    Online,
    Offline,
    Hybrid
}

/// <summary>Placement drive lifecycle.</summary>
public enum DriveStatus
{
    Scheduled,
    Completed,
    Cancelled
}
