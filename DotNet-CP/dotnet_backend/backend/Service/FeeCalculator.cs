using ComputerSeekho.DTO;
using ComputerSeekho.Models;

namespace ComputerSeekho.Service;

/// <summary>
/// The single source of truth for what a student owes and when.
///
/// Its own component so the figure shown on the registration form, the
/// figure written to payments, and the figure printed on the receipt all
/// come from one place and cannot disagree.
/// </summary>
public class FeeCalculator
{
    /// <summary>Fixed system-wide (BRD section 3.2), not staff-configurable.</summary>
    public const int TotalInstallments = 2;

    /// <summary>Installment two falls due 30 days after registration.</summary>
    public const int Installment2OffsetDays = 30;

    public FeeBreakdownDto BreakdownFor(Course course, DateOnly registrationDate)
    {
        var total = course.Fees;

        // Halve, rounding the first installment UP.
        //
        // On an odd amount the student pays the extra paisa first and the
        // remainder settles exactly, so the two always sum back to the fee.
        // Rounding both halves independently cannot guarantee that — 35001
        // would become 17500.50 twice, or 17501 and 17501.
        var first = Math.Ceiling(total / TotalInstallments * 100m) / 100m;
        var second = total - first;

        return new FeeBreakdownDto(
            course.CourseId,
            course.Name,
            decimal.Round(total, 2),
            TotalInstallments,
            first,
            registrationDate,
            second,
            registrationDate.AddDays(Installment2OffsetDays));
    }

    /// <summary>
    /// Receipt numbers are derived, not sequential — "VITA/2026-27/00012-1".
    ///
    /// A running counter needs either a sequence table or a read-then-
    /// increment that two concurrent registrations could both win.
    /// (studentId, installmentNumber) is already unique by construction, so
    /// deriving from it is collision-proof without extra machinery, and it
    /// still reads sensibly on a printed receipt.
    /// </summary>
    public string ReceiptNumberFor(int studentId, int installmentNumber, DateOnly date) =>
        $"VITA/{FinancialYear(date)}/{studentId:D5}-{installmentNumber}";

    /// <summary>Indian financial year: April to March, so 2026-07-29 is "2026-27".</summary>
    public string FinancialYear(DateOnly date)
    {
        var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }
}
