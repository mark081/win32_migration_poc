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
    check(MaximumLoanDays(L"STAFF") == 30, "staff duration");
    check(IsEligibleForCheckout(1, 0, 1, L"STANDARD") == 1, "eligible standard");
    check(IsEligibleForCheckout(1, 1, 0, L"STANDARD") == 0, "overdue blocked");
    check(IsEligibleForCheckout(1, 0, 2, L"STANDARD") == 0, "limit blocked");
    check(IsEligibleForCheckout(0, 0, 0, L"STAFF") == 0, "inactive blocked");
    std::cout << (failures ? "Native rule tests failed\n" : "Native rule tests passed\n");
    return failures ? 1 : 0;
}
