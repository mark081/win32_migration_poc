#include <iostream>
#include "../../src/NativeRules/NativeRules.h"
static int failures = 0;
static void check(bool value, const char *name)
{
    if (!value)
    {
        std::cerr << "FAIL: " << name << "\n";
        ++failures;
    }
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
    std::cout << (failures ? "Native rule tests failed\n" : "Native rule tests passed\n");
    return failures ? 1 : 0;
}
