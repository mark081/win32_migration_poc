// PURPOSE
//
// This file defines the small checkout input, NativeRules, request, response, and message helpers
// used by the Connected migration. It does not send HTTP requests, authorize checkout, or write
// data. Legacy and compare retain the native adapter; service mode uses only service responses.

#pragma once

#include <string>

enum class ClientRuleMode;

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

// Returns whether the selected mode must run the retained NativeRules eligibility decision.
// Service mode is the only mode that bypasses both NativeRules and member policy fields.
bool RequiresNativeCheckoutDecision(ClientRuleMode mode);

// Serializes one read-only service-mode request without native evidence, a tool ID, or a write key.
std::string BuildServiceDecisionRequest(const std::wstring &memberId, const std::wstring &dueOn,
                                        const std::wstring &configurationVersion);

// Accepts only a current version 1 service response with a known, internally consistent reason.
bool IsValidServiceDecision(int contractVersion, const std::wstring &effectiveMode, bool allowed,
                            const std::wstring &reason, const std::wstring &configurationVersion,
                            const std::wstring &expectedConfigurationVersion);

// Maps a stable service denial reason to operator-safe text. Unknown and allowed reasons return
// null because they must not be presented as a completed denial.
const wchar_t *ServiceDecisionMessage(const std::wstring &reason);

// Accepts a positive decimal identifier that fits the service's 32-bit request contract.
bool IsPositiveCheckoutId(const std::wstring &value);

// Accepts a real calendar date in the UI's documented YYYY-MM-DD form. It checks shape only and
// leaves eligibility and allowed-duration decisions to the selected rule owner and PostgreSQL.
bool IsCheckoutDate(const std::wstring &value);
