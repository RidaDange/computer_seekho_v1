using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// One fee installment.
///
/// ReceiptNo is unique and derived rather than sequential — see
/// FeeCalculator for why a running counter would be a race.
/// </summary>
[Table("payments")]
public class Payment
{
    [Key]
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("enrollment_id")]
    public int EnrollmentId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("installment_number")]
    public int InstallmentNumber { get; set; } = 1;

    /// <summary>Fixed at 2 system-wide (BRD section 3.2).</summary>
    [Column("total_installments")]
    public int TotalInstallments { get; set; } = 2;

    [Column("payment_date")]
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("payment_mode")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Success;

    [Column("transaction_id")]
    [MaxLength(100)]
    public string? TransactionId { get; set; }

    [Column("receipt_no")]
    [Required, MaxLength(100)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Column("remarks")]
    public string? Remarks { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(EnrollmentId))]
    public Enrollment? Enrollment { get; set; }
}
