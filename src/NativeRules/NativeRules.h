#pragma once
#ifdef NATIVERULES_EXPORTS
#define NR_API __declspec(dllexport)
#else
#define NR_API __declspec(dllimport)
#endif

// Explains why a member is allowed or blocked from checking out a tool.
// Keep the assigned numbers unchanged because installed desktop clients may depend on them.
enum CheckoutEligibilityReasonCode
{
    NR_ALLOWED = 0,
    NR_INACTIVE = 1,
    NR_OVERDUE = 2,
    NR_CHECKOUT_LIMIT_REACHED = 3,
    NR_TIER_UNSUPPORTED = 4
};

extern "C"
{
    NR_API int __stdcall CheckoutLimit(const wchar_t *tier);
    NR_API int __stdcall MaximumLoanDays(const wchar_t *tier);
    // Gives the desktop client a reason it can display before sending a checkout request.
    // The database still makes the final decision and can reject the checkout.
    // Keep this function and its result numbers compatible with installed 32-bit clients.
    // It can be removed after all supported clients use the service decision and the checkout,
    // limit, and overdue scenarios produce the same results through that service.
    NR_API CheckoutEligibilityReasonCode __stdcall CheckoutEligibilityReasonV1(int active,
                                                                               int hasOverdueLoan,
                                                                               int openLoans,
                                                                               const wchar_t *tier);
    NR_API int __stdcall IsEligibleForCheckout(int active, int hasOverdueLoan, int openLoans,
                                               const wchar_t *tier);
}
