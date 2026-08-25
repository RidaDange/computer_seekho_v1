package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Past;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

import java.time.LocalDate;

/**
 * Step 2 of the registration wizard. Address is segregated per KB §4.2 —
 * confirmed as the student's own address; Annexure 1's "Office Address"
 * block is deliberately not carried into the online system.
 */
public record StudentDetailsRequest(

        @NotBlank(message = "First name is required")
        @Size(max = 150) String firstName,

        @NotBlank(message = "Last name is required")
        @Size(max = 150) String lastName,

        @NotBlank(message = "Parent/guardian name is required")
        @Size(max = 150) String parentName,

        @Pattern(regexp = "^$|^[6-9]\\d{9}$", message = "Enter a valid 10-digit mobile number")
        String parentPhone,

        @NotBlank(message = "Email is required")
        @Email(message = "Enter a valid email address")
        @Size(max = 150) String email,

        @NotBlank(message = "Phone number is required")
        @Pattern(regexp = "^[6-9]\\d{9}$", message = "Enter a valid 10-digit mobile number")
        String phone,

        @Past(message = "Date of birth must be in the past")
        LocalDate dob,

        String gender, // Male | Female | Other

        @Size(max = 255) String addressLine1,
        @Size(max = 255) String addressLine2,
        @Size(max = 100) String city,
        @Size(max = 100) String state,

        @Pattern(regexp = "^$|^\\d{6}$", message = "Enter a valid 6-digit pincode")
        String pincode,

        @Size(max = 500) String photoUrl,
        @Size(max = 150) String qualification
) {}
