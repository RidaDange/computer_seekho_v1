/**
 * Mirrors the Bean Validation rules on InquiryCreateRequest so a visitor is
 * told about an obvious mistake before a round-trip. The server re-checks
 * everything regardless — this is a convenience layer, not the rule itself.
 *
 * Lives in its own module rather than beside EnquiryFields so that file
 * stays a pure component (React Fast Refresh only works on modules that
 * export components only).
 */
export function validateEnquiry(form) {
  const errors = {};

  if (!form.courseId) errors.courseId = "Please select a course";
  if (!form.enquirerName?.trim()) errors.enquirerName = "Name is required";

  if (!form.email?.trim()) {
    errors.email = "Email is required";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
    errors.email = "Please enter a valid email address";
  }

  if (!form.phone?.trim()) {
    errors.phone = "Phone number is required";
  } else if (!/^[6-9]\d{9}$/.test(form.phone.trim())) {
    errors.phone = "Please enter a valid 10-digit Indian mobile number";
  }

  return errors;
}
