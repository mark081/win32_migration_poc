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
int __stdcall IsEligibleForCheckout(int active, int overdue, int openLoans, const wchar_t *tier)
{
    const int limit = CheckoutLimit(tier);
    return active && !overdue && limit > 0 && openLoans < limit;
}
