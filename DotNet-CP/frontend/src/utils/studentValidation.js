/**
 * Mirrors the Bean Validation rules on StudentDetailsRequest so the
 * registration wizard catches an obvious miss at step 2, where the field
 * actually is, rather than at step 3 after the counsellor has already
 * chosen a batch and typed a payment.
 *
 * The server still re-checks everything — this is about where the error
 * appears, not whether it's enforced.
 */
export function validateStudentDetails(d = {}) {
  const errors = {};

  if (!d.firstName?.trim()) errors.firstName = "First name is required";
  if (!d.lastName?.trim()) errors.lastName = "Last name is required";
  if (!d.parentName?.trim()) errors.parentName = "Parent/guardian name is required";

  if (!d.email?.trim()) {
    errors.email = "Email is required";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(d.email.trim())) {
    errors.email = "Enter a valid email address";
  }

  if (!d.phone?.trim()) {
    errors.phone = "Phone number is required";
  } else if (!/^[6-9]\d{9}$/.test(d.phone.trim())) {
    errors.phone = "Enter a valid 10-digit mobile number";
  }

  // Optional, but validated when present — the server rejects these too.
  if (d.parentPhone?.trim() && !/^[6-9]\d{9}$/.test(d.parentPhone.trim())) {
    errors.parentPhone = "Enter a valid 10-digit mobile number";
  }
  if (d.pincode?.trim() && !/^\d{6}$/.test(d.pincode.trim())) {
    errors.pincode = "Enter a valid 6-digit pincode";
  }
  if (d.dob && new Date(d.dob) >= new Date()) {
    errors.dob = "Date of birth must be in the past";
  }

  return errors;
}
