// PURPOSE
//
// This file calculates a service-side checkout decision from member facts already read from
// PostgreSQL. It also defines the business-date clock that later decision handling will use.
// The code does not submit a checkout, write audit data, or replace the database checkout rules.
// It supports the later compare and service migration tasks but is not called by a product route
// yet. PostgreSQL still makes the final checkout decision when a checkout command is submitted.

using System;

namespace ToolLending.AppServer
{
    // Supplies the current UTC business date once per service decision.
    // Later decision handling uses this instead of the desktop workstation clock.
    internal interface IBusinessDateClock
    {
        // Returns the current UTC calendar date with no time-of-day value.
        DateTime Today { get; }
    }

    // Reads the current UTC date from the operating system for production decisions.
    // Tests replace this class with a fixed clock so date boundaries are repeatable.
    internal sealed class SystemBusinessDateClock : IBusinessDateClock
    {
        // Returns today's UTC date. Reading this property has no database or workflow side effects.
        public DateTime Today => DateTime.UtcNow.Date;
    }

    // Holds only the member facts needed to calculate checkout eligibility.
    // Repository reads create this value; the evaluator uses it, but PostgreSQL still decides
    // whether a later checkout command commits.
    internal sealed class MemberEligibilityContext
    {
        // Captures one member read. Limits and maximum days must come from PostgreSQL tier
        // functions rather than a second service-owned tier table.
        public MemberEligibilityContext(
            int memberId,
            string tier,
            bool active,
            int openLoans,
            bool hasOverdueLoan,
            int checkoutLimit,
            int maximumLoanDays
        )
        {
            MemberId = memberId;
            Tier = tier;
            Active = active;
            OpenLoans = openLoans;
            HasOverdueLoan = hasOverdueLoan;
            CheckoutLimit = checkoutLimit;
            MaximumLoanDays = maximumLoanDays;
        }

        public int MemberId { get; }
        public string Tier { get; }
        public bool Active { get; }
        public int OpenLoans { get; }
        public bool HasOverdueLoan { get; }
        public int CheckoutLimit { get; }
        public int MaximumLoanDays { get; }
    }

    // Defines stable reason text used by service rule results and later API responses.
    // These values are a compatibility contract and must not be renamed casually.
    internal static class CheckoutDecisionReasons
    {
        public const string Allowed = "ALLOWED";
        public const string MemberNotFound = "MEMBER_NOT_FOUND";
        public const string MemberInactive = "MEMBER_INACTIVE";
        public const string Overdue = "OVERDUE";
        public const string CheckoutLimitReached = "CHECKOUT_LIMIT_REACHED";
        public const string DueDateInvalid = "DUE_DATE_INVALID";
        public const string TierUnsupported = "TIER_UNSUPPORTED";
    }

    // Holds a read-only service rule result and the tier facts the desktop may later display.
    // This result permits a checkout attempt; it never reports that a checkout has succeeded.
    internal sealed class CheckoutDecision
    {
        // Captures one allow or deny result. Limit and duration are null when no valid member tier
        // facts exist. Construction changes no workflow, audit, or retry data.
        public CheckoutDecision(
            bool allowed,
            string reason,
            int? checkoutLimit,
            int? maximumLoanDays
        )
        {
            Allowed = allowed;
            Reason = reason;
            CheckoutLimit = checkoutLimit;
            MaximumLoanDays = maximumLoanDays;
        }

        public bool Allowed { get; }
        public string Reason { get; }
        public int? CheckoutLimit { get; }
        public int? MaximumLoanDays { get; }
    }

    // Calculates checkout eligibility from one member read and an explicit business date.
    // The decision is side-effect-free and cannot force PostgreSQL to accept a later command.
    internal interface ICheckoutRuleEvaluator
    {
        // Applies the documented reason order to the supplied member facts and due date.
        // A null member means the member was not found. Dates are compared by calendar date.
        CheckoutDecision Evaluate(
            MemberEligibilityContext member,
            DateTime dueOn,
            DateTime businessDate
        );
    }

    // Implements the service checkout decision table for LOAN-001, LOAN-002, and LOAN-003.
    // PostgreSQL tier functions supply limit and duration facts; this class only compares them.
    internal sealed class CheckoutRuleEvaluator : ICheckoutRuleEvaluator
    {
        // Returns the first matching reason in the approved presentation order.
        // The method reads only its arguments and performs no database, audit, or retry writes.
        public CheckoutDecision Evaluate(
            MemberEligibilityContext member,
            DateTime dueOn,
            DateTime businessDate
        )
        {
            if (member == null)
                return Decision(false, CheckoutDecisionReasons.MemberNotFound, null);

            if (!member.Active)
                return Decision(false, CheckoutDecisionReasons.MemberInactive, member);

            if (
                !IsSupportedTier(member.Tier)
                || member.CheckoutLimit <= 0
                || member.MaximumLoanDays <= 0
            )
            {
                return Decision(false, CheckoutDecisionReasons.TierUnsupported, null);
            }

            if (member.HasOverdueLoan)
                return Decision(false, CheckoutDecisionReasons.Overdue, member);

            if (member.OpenLoans >= member.CheckoutLimit)
                return Decision(false, CheckoutDecisionReasons.CheckoutLimitReached, member);

            var firstAllowedDate = businessDate.Date;
            var lastAllowedDate = firstAllowedDate.AddDays(member.MaximumLoanDays);
            var requestedDate = dueOn.Date;
            if (requestedDate < firstAllowedDate || requestedDate > lastAllowedDate)
                return Decision(false, CheckoutDecisionReasons.DueDateInvalid, member);

            return Decision(true, CheckoutDecisionReasons.Allowed, member);
        }

        // Recognizes the three tiers supported by both NativeRules and PostgreSQL.
        // A missing or unknown tier is denied even if a caller supplies positive numeric limits.
        private static bool IsSupportedTier(string tier)
        {
            return string.Equals(tier, "STANDARD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tier, "SUPPORTER", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tier, "STAFF", StringComparison.OrdinalIgnoreCase);
        }

        // Builds one result and includes tier facts only when a valid member tier was read.
        private static CheckoutDecision Decision(
            bool allowed,
            string reason,
            MemberEligibilityContext member
        )
        {
            return new CheckoutDecision(
                allowed,
                reason,
                member?.CheckoutLimit,
                member?.MaximumLoanDays
            );
        }
    }
}
