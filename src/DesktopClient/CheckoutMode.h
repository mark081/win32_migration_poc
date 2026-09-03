// PURPOSE
//
// This file defines the small NativeRules adapter and compare-request serializer used by the
// Connected checkout migration. It does not send HTTP requests, authorize checkout, or write data.
// Legacy and compare modes use it; service mode removes this call in the next migration task.

#pragma once

#include <string>

// Carries the stable version 1 NativeRules result sent only as compare-mode evidence.
struct NativeCheckoutObservation
{
    bool allowed = false;
    std::string reason;
};

// Calls the versioned NativeRules export once and maps its code to the service's stable reason.
NativeCheckoutObservation ObserveNativeCheckout(bool active, bool overdue, int openLoans,
                                                const std::wstring &tier);

// Serializes one read-only compare request. Inputs are validated by the existing UI and again by
// the service; the result contains no command, idempotency key, credential, or persistence ID.
std::string BuildCompareDecisionRequest(const std::wstring &memberId, const std::wstring &dueOn,
                                        const std::wstring &configurationVersion,
                                        const NativeCheckoutObservation &observation);
