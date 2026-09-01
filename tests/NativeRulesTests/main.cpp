#include <iostream>
#include "../../src/NativeRules/NativeRules.h"
static_assert(NR_ALLOWED == 0, "version 1 allowed reason changed");
static_assert(NR_INACTIVE == 1, "version 1 inactive reason changed");
static_assert(NR_OVERDUE == 2, "version 1 overdue reason changed");
static_assert(NR_CHECKOUT_LIMIT_REACHED == 3, "version 1 limit reason changed");
static_assert(NR_TIER_UNSUPPORTED == 4, "version 1 unsupported-tier reason changed");
static int failures = 0;
static void check(bool value, const char *name)
{
    if (!value)
    {
        std::cerr << "FAIL: " << name << "\n";
        ++failures;
    }
}
static void checkEligibility(int active, int overdue, int openLoans, const wchar_t *tier,
                             CheckoutEligibilityReasonCode expected, const char *name)
{
    const CheckoutEligibilityReasonCode reason =
        CheckoutEligibilityReasonV1(active, overdue, openLoans, tier);
    check(reason == expected, name);
    check((IsEligibleForCheckout(active, overdue, openLoans, tier) != 0) == (reason == NR_ALLOWED),
          "legacy boolean matches structured eligibility");
}
int main()
{
    check(CheckoutLimit(L"STANDARD") == 2, "standard limit");
    check(CheckoutLimit(L"SUPPORTER") == 5, "supporter limit");
    check(CheckoutLimit(L"STAFF") == 10, "staff limit");
    check(CheckoutLimit(L"UNKNOWN") == 0, "unknown tier limit");
    check(MaximumLoanDays(L"STANDARD") == 7, "standard duration");
    check(MaximumLoanDays(L"SUPPORTER") == 14, "supporter duration");
    check(MaximumLoanDays(L"STAFF") == 30, "staff duration");
    check(MaximumLoanDays(L"UNKNOWN") == 0, "unknown tier duration");
    check(IsEligibleForCheckout(1, 0, 0, L"STANDARD") == 1, "standard eligible with no open loans");
    check(IsEligibleForCheckout(1, 0, 1, L"STANDARD") == 1, "standard eligible below limit");
    check(IsEligibleForCheckout(1, 1, 0, L"STANDARD") == 0, "overdue blocked");
    check(IsEligibleForCheckout(1, 0, 2, L"STANDARD") == 0, "standard blocked at limit");
    check(IsEligibleForCheckout(1, 0, 3, L"STANDARD") == 0, "standard blocked above limit");
    check(IsEligibleForCheckout(1, 0, 4, L"SUPPORTER") == 1, "supporter eligible below limit");
    check(IsEligibleForCheckout(1, 0, 5, L"SUPPORTER") == 0, "supporter blocked at limit");
    check(IsEligibleForCheckout(1, 0, 9, L"STAFF") == 1, "staff eligible below limit");
    check(IsEligibleForCheckout(1, 0, 10, L"STAFF") == 0, "staff blocked at limit");
    check(IsEligibleForCheckout(0, 0, 0, L"STAFF") == 0, "inactive blocked");
    check(IsEligibleForCheckout(1, 0, 0, L"UNKNOWN") == 0, "unknown tier blocked");

    checkEligibility(1, 0, 0, L"STANDARD", NR_ALLOWED, "structured allowed");
    checkEligibility(1, 0, 1, L"STANDARD", NR_ALLOWED, "structured standard below limit");
    checkEligibility(0, 0, 0, L"STANDARD", NR_INACTIVE, "structured inactive");
    checkEligibility(0, 0, 0, L"STAFF", NR_INACTIVE, "structured inactive staff");
    checkEligibility(1, 1, 0, L"STANDARD", NR_OVERDUE, "structured overdue");
    checkEligibility(1, 0, 2, L"STANDARD", NR_CHECKOUT_LIMIT_REACHED,
                     "structured standard at limit");
    checkEligibility(1, 0, 3, L"STANDARD", NR_CHECKOUT_LIMIT_REACHED,
                     "structured standard above limit");
    checkEligibility(1, 0, 5, L"SUPPORTER", NR_CHECKOUT_LIMIT_REACHED,
                     "structured supporter at limit");
    checkEligibility(1, 0, 4, L"SUPPORTER", NR_ALLOWED, "structured supporter below limit");
    checkEligibility(1, 0, 10, L"STAFF", NR_CHECKOUT_LIMIT_REACHED, "structured staff at limit");
    checkEligibility(1, 0, 9, L"STAFF", NR_ALLOWED, "structured staff below limit");
    checkEligibility(1, 0, 0, L"UNKNOWN", NR_TIER_UNSUPPORTED, "structured unknown tier");
    checkEligibility(1, 0, 0, nullptr, NR_TIER_UNSUPPORTED, "structured null tier");

    checkEligibility(0, 1, 2, L"STANDARD", NR_INACTIVE, "inactive reason has precedence");
    checkEligibility(1, 1, 0, L"UNKNOWN", NR_TIER_UNSUPPORTED,
                     "unsupported tier reason has precedence over overdue");
    std::cout << (failures ? "Native rule tests failed\n" : "Native rule tests passed\n");
    return failures ? 1 : 0;
}
