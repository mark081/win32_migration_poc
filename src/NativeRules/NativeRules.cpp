#define NATIVERULES_EXPORTS
#include "NativeRules.h"
#include <cwchar>

static bool tier_is(const wchar_t *value, const wchar_t *expected)
{
    return value && _wcsicmp(value, expected) == 0;
}
int __stdcall CheckoutLimit(const wchar_t *tier)
{
    if (tier_is(tier, L"STAFF"))
        return 10;
    if (tier_is(tier, L"SUPPORTER"))
        return 5;
    return tier_is(tier, L"STANDARD") ? 2 : 0;
}
int __stdcall MaximumLoanDays(const wchar_t *tier)
{
    if (tier_is(tier, L"STAFF"))
        return 30;
    if (tier_is(tier, L"SUPPORTER"))
        return 14;
    return tier_is(tier, L"STANDARD") ? 7 : 0;
}
CheckoutEligibilityReasonCode __stdcall CheckoutEligibilityReasonV1(int active, int overdue,
                                                                    int openLoans,
                                                                    const wchar_t *tier)
{
    if (!active)
        return NR_INACTIVE;

    const int limit = CheckoutLimit(tier);
    if (limit <= 0)
        return NR_TIER_UNSUPPORTED;
    if (overdue)
        return NR_OVERDUE;
    if (openLoans >= limit)
        return NR_CHECKOUT_LIMIT_REACHED;
    return NR_ALLOWED;
}
int __stdcall IsEligibleForCheckout(int active, int overdue, int openLoans, const wchar_t *tier)
{
    return CheckoutEligibilityReasonV1(active, overdue, openLoans, tier) == NR_ALLOWED;
}
