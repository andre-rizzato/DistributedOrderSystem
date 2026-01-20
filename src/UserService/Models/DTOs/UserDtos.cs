using System.ComponentModel.DataAnnotations;

namespace UserService.Models.DTOs;

public record UserProfileDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? ProfilePictureUrl,
    string PreferredLanguage,
    string PreferredCurrency,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool IsPrimeMember,
    DateTime? PrimeMembershipExpiry,
    int LoyaltyPoints,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? ProfilePictureUrl
);

public record UpdatePreferencesRequest(
    string? PreferredLanguage,
    string? PreferredCurrency,
    bool? EmailNotificationsEnabled,
    bool? SmsNotificationsEnabled,
    bool? PushNotificationsEnabled
);

public record AddressDto(
    Guid Id,
    string FullName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string PostalCode,
    string CountryCode,
    string? PhoneNumber,
    string Type,
    bool IsDefault
);

public record CreateAddressRequest(
    [Required] string FullName,
    [Required] string AddressLine1,
    string? AddressLine2,
    [Required] string City,
    string? StateProvince,
    [Required] string PostalCode,
    [Required] string CountryCode,
    string? PhoneNumber,
    AddressType Type,
    bool IsDefault
);

public record UpdateAddressRequest(
    string? FullName,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? CountryCode,
    string? PhoneNumber,
    AddressType? Type,
    bool? IsDefault
);

public record PaymentMethodDto(
    Guid Id,
    string Type,
    string CardHolderName,
    string Last4Digits,
    string CardBrand,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault,
    bool IsExpired
);

public record CreatePaymentMethodRequest(
    [Required] PaymentType Type,
    [Required] string CardHolderName,
    [Required] string CardNumber, // Será encriptado
    [Required] string CardBrand,
    [Required] int ExpiryMonth,
    [Required] int ExpiryYear,
    [Required] string CVV, // Nunca se guarda
    bool IsDefault
);
