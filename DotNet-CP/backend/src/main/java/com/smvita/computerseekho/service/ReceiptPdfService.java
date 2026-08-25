package com.smvita.computerseekho.service;

import com.lowagie.text.Document;
import com.lowagie.text.Element;
import com.lowagie.text.Font;
import com.lowagie.text.FontFactory;
import com.lowagie.text.PageSize;
import com.lowagie.text.Paragraph;
import com.lowagie.text.Phrase;
import com.lowagie.text.pdf.PdfPCell;
import com.lowagie.text.pdf.PdfPTable;
import com.lowagie.text.pdf.PdfWriter;
import com.smvita.computerseekho.dto.FeeBreakdownDto;
import com.smvita.computerseekho.entity.Enrollment;
import com.smvita.computerseekho.entity.Payment;
import com.smvita.computerseekho.entity.Student;
import org.springframework.stereotype.Service;

import java.awt.Color;
import java.io.ByteArrayOutputStream;
import java.math.BigDecimal;
import java.time.format.DateTimeFormatter;

/**
 * Renders the registration receipt with OpenPDF.
 *
 * The KB (§3.2, Turn 4) upgraded this from a printed paper form to an
 * "online receipt", so it has to stand on its own as the student's record:
 * both installments and their due dates are shown, not just the amount
 * collected today, otherwise the student leaves the counter with no written
 * statement of what's still owed.
 */
@Service
public class ReceiptPdfService {

    private static final DateTimeFormatter DATE = DateTimeFormatter.ofPattern("dd MMM yyyy");
    private static final Color INK = new Color(30, 42, 58);
    private static final Color MUTED = new Color(100, 116, 139);
    private static final Color RULE = new Color(226, 232, 240);

    private static final Font H1 = FontFactory.getFont(FontFactory.HELVETICA_BOLD, 16, INK);
    private static final Font H2 = FontFactory.getFont(FontFactory.HELVETICA_BOLD, 10, INK);
    private static final Font BODY = FontFactory.getFont(FontFactory.HELVETICA, 10, INK);
    private static final Font BODY_BOLD = FontFactory.getFont(FontFactory.HELVETICA_BOLD, 10, INK);
    private static final Font SMALL = FontFactory.getFont(FontFactory.HELVETICA, 8, MUTED);

    public byte[] render(Student student, Enrollment enrollment, Payment payment, FeeBreakdownDto fees) {
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        Document doc = new Document(PageSize.A4, 48, 48, 48, 48);
        PdfWriter.getInstance(doc, out);
        doc.open();

        doc.add(titleBlock(payment));
        doc.add(spacer(12));
        doc.add(studentBlock(student, enrollment));
        doc.add(spacer(12));
        doc.add(feeBlock(payment, fees));
        doc.add(spacer(16));
        doc.add(footer());

        doc.close();
        return out.toByteArray();
    }

    private Element titleBlock(Payment payment) {
        PdfPTable table = new PdfPTable(2);
        table.setWidthPercentage(100);
        table.setWidths(new float[] { 3f, 2f });

        PdfPCell left = borderless(new Paragraph(
                "Shriram Mantri Vidyanidhi Info Tech Academy\n", H1));
        left.addElement(new Paragraph("Authorised Training Centre of C-DAC ACTS", SMALL));
        left.addElement(new Paragraph("REGISTRATION RECEIPT", H2));
        table.addCell(left);

        PdfPCell right = borderless(new Paragraph("Receipt No\n", SMALL));
        right.addElement(new Paragraph(payment.getReceiptNo(), BODY_BOLD));
        right.addElement(new Paragraph("Date: " + payment.getPaymentDate().format(DATE), BODY));
        right.setHorizontalAlignment(Element.ALIGN_RIGHT);
        table.addCell(right);

        return table;
    }

    private Element studentBlock(Student student, Enrollment enrollment) {
        PdfPTable table = new PdfPTable(2);
        table.setWidthPercentage(100);
        table.setWidths(new float[] { 1f, 2f });

        row(table, "Student", student.getFirstName() + " " + student.getLastName());
        row(table, "Student ID", String.valueOf(student.getStudentId()));
        row(table, "Parent / Guardian", student.getParentName());
        row(table, "Contact", student.getPhone() + "  ·  " + student.getEmail());
        row(table, "Course", enrollment.getBatch().getCourse().getName());
        row(table, "Batch", enrollment.getBatch().getBatchName());
        row(table, "Registered on", student.getRegDate().format(DATE));
        row(table, "Enquiry Ref", "#" + student.getInquiry().getInquiryId());

        return table;
    }

    private Element feeBlock(Payment payment, FeeBreakdownDto fees) {
        PdfPTable table = new PdfPTable(4);
        table.setWidthPercentage(100);
        table.setWidths(new float[] { 2.4f, 1.4f, 1.2f, 1.2f });

        header(table, "Particulars");
        header(table, "Due date");
        header(table, "Amount");
        header(table, "Status");

        boolean firstPaid = payment.getInstallmentNumber() == 1;
        cell(table, "Installment 1 of " + fees.totalInstallments(), false);
        cell(table, fees.installment1DueDate().format(DATE), false);
        cell(table, money(fees.installment1Amount()), false);
        cell(table, firstPaid ? "PAID" : "Due", true);

        cell(table, "Installment 2 of " + fees.totalInstallments(), false);
        cell(table, fees.installment2DueDate().format(DATE), false);
        cell(table, money(fees.installment2Amount()), false);
        cell(table, payment.getInstallmentNumber() == 2 ? "PAID" : "Due", true);

        cell(table, "Total course fee", true);
        cell(table, "", false);
        cell(table, money(fees.totalFees()), true);
        cell(table, "", false);

        cell(table, "Received now (" + payment.getPaymentMode().toDbValue() + ")", true);
        cell(table, "", false);
        cell(table, money(payment.getAmount()), true);
        cell(table, payment.getPaymentStatus().name(), false);

        BigDecimal balance = fees.totalFees().subtract(payment.getAmount());
        cell(table, "Balance outstanding", true);
        cell(table, fees.installment2DueDate().format(DATE), false);
        cell(table, money(balance), true);
        cell(table, balance.signum() <= 0 ? "Settled" : "Due", false);

        return table;
    }

    private Element footer() {
        Paragraph p = new Paragraph();
        p.add(new Phrase("This is a computer-generated receipt and does not require a signature.\n", SMALL));
        p.add(new Phrase("Please retain it for your records. For queries, contact the SMVITA front desk.", SMALL));
        return p;
    }

    // ------------------------------------------------------------ plumbing

    private static String money(BigDecimal amount) {
        return "INR " + amount.toPlainString();
    }

    private static Paragraph spacer(float height) {
        Paragraph p = new Paragraph(" ");
        p.setSpacingAfter(height);
        return p;
    }

    private static PdfPCell borderless(Paragraph content) {
        PdfPCell cell = new PdfPCell();
        cell.setBorder(com.lowagie.text.Rectangle.NO_BORDER);
        cell.addElement(content);
        return cell;
    }

    private static void row(PdfPTable table, String label, String value) {
        PdfPCell l = new PdfPCell(new Phrase(label, SMALL));
        PdfPCell v = new PdfPCell(new Phrase(value == null ? "—" : value, BODY));
        for (PdfPCell c : new PdfPCell[] { l, v }) {
            c.setBorder(com.lowagie.text.Rectangle.BOTTOM);
            c.setBorderColor(RULE);
            c.setPadding(5f);
        }
        table.addCell(l);
        table.addCell(v);
    }

    private static void header(PdfPTable table, String label) {
        PdfPCell c = new PdfPCell(new Phrase(label, H2));
        c.setBorder(com.lowagie.text.Rectangle.BOTTOM);
        c.setBorderColor(INK);
        c.setPadding(5f);
        table.addCell(c);
    }

    private static void cell(PdfPTable table, String value, boolean bold) {
        PdfPCell c = new PdfPCell(new Phrase(value, bold ? BODY_BOLD : BODY));
        c.setBorder(com.lowagie.text.Rectangle.BOTTOM);
        c.setBorderColor(RULE);
        c.setPadding(5f);
        table.addCell(c);
    }
}
