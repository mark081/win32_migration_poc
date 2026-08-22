#pragma once
#ifdef NATIVERULES_EXPORTS
#define NR_API __declspec(dllexport)
#else
#define NR_API __declspec(dllimport)
#endif

extern "C" {
NR_API int __stdcall CheckoutLimit(const wchar_t* tier);
NR_API int __stdcall MaximumLoanDays(const wchar_t* tier);
NR_API int __stdcall IsEligibleForCheckout(int active, int hasOverdueLoan, int openLoans, const wchar_t* tier);
}
