// PURPOSE
//
// This file is the AppServer's only path to PostgreSQL. It contains read queries and the existing
// transaction-protected workflow writes. The desktop must never call PostgreSQL directly.
// PostgreSQL routines still make the final checkout, reservation, and return decisions.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace ToolLending.AppServer
{
    // Reads application data and submits workflow commands to PostgreSQL for AppServer controllers.
    // Read methods do not change data. Existing write methods use PostgreSQL transactions, where
    // the database makes the final business decision and writes the audit record.
    public sealed class Repository
    {
        private readonly string connectionString;

        // Uses the configured ToolLending connection for normal AppServer work.
        // Startup fails as before when the required connection setting is absent.
        public Repository()
            : this(ConfigurationManager.ConnectionStrings["ToolLending"].ConnectionString) { }

        // Uses an explicit connection for focused repository tests without changing product
        // configuration. Empty values are rejected before any database connection is attempted.
        internal Repository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A database connection string is required.");

            this.connectionString = connectionString;
        }

        private NpgsqlConnection Open()
        {
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public IList<ToolDto> GetTools()
        {
            var tools = new List<ToolDto>();

            using (var connection = Open())
            using (
                var command = new NpgsqlCommand(
                    @"SELECT
                      t.tool_id,
                      t.asset_tag,
                      t.display_name,
                      t.daily_late_fee,
                      t.status::text,
                      t.version,
                      l.loan_id,
                      m.member_id,
                      m.display_name
                  FROM tool_lending.tools t
                  LEFT JOIN tool_lending.loans l
                    ON l.tool_id = t.tool_id
                   AND l.status = 'OPEN'
                  LEFT JOIN tool_lending.members m
                    ON m.member_id = l.member_id
                  ORDER BY t.tool_id",
                    connection
                )
            )
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tools.Add(
                        new ToolDto
                        {
                            ToolId = reader.GetInt32(0),
                            AssetTag = reader.GetString(1),
                            DisplayName = reader.GetString(2),
                            DailyLateFee = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            Version = reader.GetInt32(5),
                            LoanId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6),
                            BorrowedByMemberId = reader.IsDBNull(7)
                                ? (int?)null
                                : reader.GetInt32(7),
                            BorrowedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                        }
                    );
                }
            }

            return tools;
        }

        public WriteResult CreateMember(
            CreateMemberRequest member,
            string actor,
            Guid requestId,
            Guid idempotencyKey
        )
        {
            return Execute(
                "create_member",
                idempotencyKey,
                member,
                requestId,
                (connection, transaction) =>
                {
                    int id;
                    const string insertSql =
                        @"
                        INSERT INTO tool_lending.members(display_name, tier, active)
                        VALUES (@name, CAST(@tier AS tool_lending.member_tier), @active)
                        RETURNING member_id";

                    using (var command = new NpgsqlCommand(insertSql, connection, transaction))
                    {
                        command.Parameters.Add("name", NpgsqlDbType.Varchar).Value =
                            member.DisplayName.Trim();
                        command.Parameters.Add("tier", NpgsqlDbType.Varchar).Value = member.Tier;
                        command.Parameters.Add("active", NpgsqlDbType.Boolean).Value =
                            member.Active;
                        id = Convert.ToInt32(command.ExecuteScalar());
                    }

                    InsertAudit(
                        connection,
                        transaction,
                        actor,
                        "CREATE_MEMBER",
                        "member",
                        id,
                        requestId
                    );
                    return new WriteResult
                    {
                        Id = id,
                        Status = member.Active ? "ACTIVE" : "INACTIVE",
                    };
                }
            );
        }

        public WriteResult CreateTool(
            CreateToolRequest tool,
            string actor,
            Guid requestId,
            Guid idempotencyKey
        )
        {
            return Execute(
                "create_tool",
                idempotencyKey,
                tool,
                requestId,
                (connection, transaction) =>
                {
                    int id;
                    const string insertSql =
                        @"
                        INSERT INTO tool_lending.tools(asset_tag, display_name, daily_late_fee)
                        VALUES (@assetTag, @name, @fee)
                        RETURNING tool_id";

                    using (var command = new NpgsqlCommand(insertSql, connection, transaction))
                    {
                        command.Parameters.Add("assetTag", NpgsqlDbType.Varchar).Value =
                            tool.AssetTag.Trim();
                        command.Parameters.Add("name", NpgsqlDbType.Varchar).Value =
                            tool.DisplayName.Trim();
                        command.Parameters.Add("fee", NpgsqlDbType.Numeric).Value =
                            tool.DailyLateFee;
                        id = Convert.ToInt32(command.ExecuteScalar());
                    }

                    InsertAudit(
                        connection,
                        transaction,
                        actor,
                        "CREATE_TOOL",
                        "tool",
                        id,
                        requestId
                    );
                    return new WriteResult { Id = id, Status = "AVAILABLE" };
                }
            );
        }

        public MemberDto GetMember(int id)
        {
            const string sql =
                @"
                SELECT
                    m.member_id,
                    m.display_name,
                    m.tier::text,
                    m.active,
                    count(l.loan_id)::int,
                    coalesce(bool_or(l.due_on < CURRENT_DATE), false),
                    tool_lending.tier_checkout_limit(m.tier),
                    tool_lending.tier_max_loan_days(m.tier)
                FROM tool_lending.members m
                LEFT JOIN tool_lending.loans l
                    ON l.member_id = m.member_id
                    AND l.status = 'OPEN'
                WHERE m.member_id = @id
                GROUP BY m.member_id";

            using (var connection = Open())
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.Add("id", NpgsqlDbType.Integer).Value = id;

                MemberDto member;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    member = new MemberDto
                    {
                        MemberId = reader.GetInt32(0),
                        DisplayName = reader.GetString(1),
                        Tier = reader.GetString(2),
                        Active = reader.GetBoolean(3),
                        OpenLoans = reader.GetInt32(4),
                        HasOverdueLoan = reader.GetBoolean(5),
                        CheckoutLimit = reader.GetInt32(6),
                        MaxLoanDays = reader.GetInt32(7),
                    };
                }

                const string loansSql =
                    @"
                    SELECT l.loan_id, t.tool_id, t.asset_tag, t.display_name, l.due_on
                    FROM tool_lending.loans l
                    JOIN tool_lending.tools t ON t.tool_id = l.tool_id
                    WHERE l.member_id = @id AND l.status = 'OPEN'
                    ORDER BY l.due_on, l.loan_id";

                using (var loansCommand = new NpgsqlCommand(loansSql, connection))
                {
                    loansCommand.Parameters.Add("id", NpgsqlDbType.Integer).Value = id;
                    using (var reader = loansCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            member.OutstandingLoans.Add(
                                new OutstandingLoanDto
                                {
                                    LoanId = reader.GetInt64(0),
                                    ToolId = reader.GetInt32(1),
                                    AssetTag = reader.GetString(2),
                                    Tool = reader.GetString(3),
                                    DueOn = reader.GetDateTime(4),
                                }
                            );
                        }
                    }
                }

                return member;
            }
        }

        // Reads the member facts used by the service checkout evaluator for the supplied business
        // date. Limits come from the existing PostgreSQL tier functions. Open and overdue loans
        // come from the existing loans table. A missing member returns null. This method performs
        // one parameterized SELECT and writes no workflow, audit, or idempotency data.
        internal MemberEligibilityContext GetMemberEligibilityContext(
            int memberId,
            DateTime businessDate
        )
        {
            const string sql =
                @"
                SELECT
                    m.member_id,
                    m.tier::text,
                    m.active,
                    count(l.loan_id)::int,
                    coalesce(bool_or(l.due_on < @businessDate), false),
                    tool_lending.tier_checkout_limit(m.tier),
                    tool_lending.tier_max_loan_days(m.tier)
                FROM tool_lending.members m
                LEFT JOIN tool_lending.loans l
                    ON l.member_id = m.member_id
                    AND l.status = 'OPEN'
                WHERE m.member_id = @memberId
                GROUP BY m.member_id";

            using (var connection = Open())
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.Add("memberId", NpgsqlDbType.Integer).Value = memberId;
                command.Parameters.Add("businessDate", NpgsqlDbType.Date).Value = businessDate.Date;

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new MemberEligibilityContext(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetBoolean(2),
                        reader.GetInt32(3),
                        reader.GetBoolean(4),
                        reader.GetInt32(5),
                        reader.GetInt32(6)
                    );
                }
            }
        }

        public IList<AuditDto> GetAudit(int take)
        {
            const string sql =
                @"
                SELECT
                    audit_id,
                    occurred_at,
                    actor,
                    operation,
                    entity_type,
                    entity_id,
                    request_id,
                    details::text
                FROM tool_lending.audit_log
                ORDER BY audit_id DESC
                LIMIT @take";

            var auditEntries = new List<AuditDto>();

            using (var connection = Open())
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.Add("take", NpgsqlDbType.Integer).Value = Math.Min(
                    Math.Max(take, 1),
                    500
                );

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        auditEntries.Add(
                            new AuditDto
                            {
                                AuditId = reader.GetInt64(0),
                                OccurredAt = reader.GetDateTime(1),
                                Actor = reader.GetString(2),
                                Operation = reader.GetString(3),
                                EntityType = reader.GetString(4),
                                EntityId = reader.GetString(5),
                                RequestId = reader.GetGuid(6),
                                Details = reader.GetString(7),
                            }
                        );
                    }
                }
            }

            return auditEntries;
        }

        public WriteResult Reserve(
            ReservationRequest reservation,
            string actor,
            Guid requestId,
            Guid idempotencyKey
        )
        {
            const string sql =
                @"
                SELECT
                    reservation_id,
                    status::text,
                    NULL::date,
                    NULL::timestamptz,
                    NULL::numeric
                FROM tool_lending.reserve_tool(
                    @tool,
                    @member,
                    @start,
                    @end,
                    @actor,
                    @request)";

            return Execute(
                "reserve",
                idempotencyKey,
                reservation,
                requestId,
                (connection, transaction) =>
                    Call(
                        connection,
                        transaction,
                        sql,
                        reservation.ToolId,
                        reservation.MemberId,
                        reservation.StartsOn,
                        reservation.EndsOn,
                        default(DateTime),
                        0,
                        actor,
                        requestId
                    )
            );
        }

        public WriteResult Checkout(
            CheckoutRequest checkout,
            string actor,
            Guid requestId,
            Guid idempotencyKey
        )
        {
            const string sql =
                @"
                SELECT
                    loan_id,
                    status::text,
                    due_on,
                    NULL::timestamptz,
                    NULL::numeric
                FROM tool_lending.checkout_tool(
                    @tool,
                    @member,
                    @due,
                    @actor,
                    @request)";

            return Execute(
                "checkout",
                idempotencyKey,
                checkout,
                requestId,
                (connection, transaction) =>
                    Call(
                        connection,
                        transaction,
                        sql,
                        checkout.ToolId,
                        checkout.MemberId,
                        default(DateTime),
                        default(DateTime),
                        checkout.DueOn,
                        0,
                        actor,
                        requestId
                    )
            );
        }

        public WriteResult Return(
            ReturnRequest returnRequest,
            string actor,
            Guid requestId,
            Guid idempotencyKey
        )
        {
            const string sql =
                @"
                SELECT
                    loan_id,
                    'RETURNED',
                    NULL::date,
                    returned_at,
                    late_fee
                FROM tool_lending.return_tool(
                    @loan,
                    @actor,
                    @request)";

            return Execute(
                "return",
                idempotencyKey,
                returnRequest,
                requestId,
                (connection, transaction) =>
                    Call(
                        connection,
                        transaction,
                        sql,
                        0,
                        0,
                        default(DateTime),
                        default(DateTime),
                        default(DateTime),
                        returnRequest.LoanId,
                        actor,
                        requestId
                    )
            );
        }

        private WriteResult Execute(
            string operation,
            Guid idempotencyKey,
            object payload,
            Guid requestId,
            Func<NpgsqlConnection, NpgsqlTransaction, WriteResult> work
        )
        {
            var requestHash = Hash(JsonConvert.SerializeObject(payload));

            using (var connection = Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                const string existingSql =
                    @"
                    SELECT
                        request_hash,
                        response_json::text
                    FROM tool_lending.idempotency_records
                    WHERE operation = @operation
                      AND idempotency_key = @key
                    FOR UPDATE";

                WriteResult previousResult = null;
                using (var command = new NpgsqlCommand(existingSql, connection, transaction))
                {
                    command.Parameters.Add("operation", NpgsqlDbType.Varchar).Value = operation;

                    command.Parameters.Add("key", NpgsqlDbType.Uuid).Value = idempotencyKey;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader.GetString(0) != requestHash)
                            {
                                throw new InvalidOperationException(
                                    "Idempotency key reused with a different request."
                                );
                            }

                            previousResult = JsonConvert.DeserializeObject<WriteResult>(
                                reader.GetString(1)
                            );
                        }
                    }
                }

                if (previousResult != null)
                {
                    transaction.Commit();
                    return previousResult;
                }

                var result = work(connection, transaction);
                result.RequestId = requestId;

                const string insertSql =
                    @"
                    INSERT INTO tool_lending.idempotency_records
                    (
                        operation,
                        idempotency_key,
                        request_hash,
                        response_json,
                        http_status
                    )
                    VALUES
                    (
                        @operation,
                        @key,
                        @hash,
                        CAST(@json AS jsonb),
                        200
                    )";

                using (var command = new NpgsqlCommand(insertSql, connection, transaction))
                {
                    command.Parameters.Add("operation", NpgsqlDbType.Varchar).Value = operation;

                    command.Parameters.Add("key", NpgsqlDbType.Uuid).Value = idempotencyKey;

                    command.Parameters.Add("hash", NpgsqlDbType.Char).Value = requestHash;

                    command.Parameters.Add("json", NpgsqlDbType.Text).Value =
                        JsonConvert.SerializeObject(result);

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return result;
            }
        }

        private static WriteResult Call(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string sql,
            int tool,
            int member,
            DateTime start,
            DateTime end,
            DateTime due,
            long loan,
            string actor,
            Guid requestId
        )
        {
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                if (sql.Contains("@tool"))
                {
                    command.Parameters.Add("tool", NpgsqlDbType.Integer).Value = tool;
                }

                if (sql.Contains("@member"))
                {
                    command.Parameters.Add("member", NpgsqlDbType.Integer).Value = member;
                }

                if (sql.Contains("@start"))
                {
                    command.Parameters.Add("start", NpgsqlDbType.Date).Value = start.Date;
                }

                if (sql.Contains("@end"))
                {
                    command.Parameters.Add("end", NpgsqlDbType.Date).Value = end.Date;
                }

                if (sql.Contains("@due"))
                {
                    command.Parameters.Add("due", NpgsqlDbType.Date).Value = due.Date;
                }

                if (sql.Contains("@loan"))
                {
                    command.Parameters.Add("loan", NpgsqlDbType.Bigint).Value = loan;
                }

                command.Parameters.Add("actor", NpgsqlDbType.Varchar).Value = actor;

                command.Parameters.Add("request", NpgsqlDbType.Uuid).Value = requestId;

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException(
                            "The database operation returned no result."
                        );
                    }

                    return new WriteResult
                    {
                        Id = reader.GetInt64(0),
                        Status = reader.GetString(1),
                        DueOn = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                        ReturnedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        LateFee = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4),
                    };
                }
            }
        }

        private static void InsertAudit(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string actor,
            string operation,
            string entityType,
            int entityId,
            Guid requestId
        )
        {
            const string sql =
                @"
                INSERT INTO tool_lending.audit_log
                    (actor, operation, entity_type, entity_id, request_id)
                VALUES
                    (@actor, @operation, @entityType, @entityId, @requestId)";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("actor", NpgsqlDbType.Varchar).Value = actor;
                command.Parameters.Add("operation", NpgsqlDbType.Varchar).Value = operation;
                command.Parameters.Add("entityType", NpgsqlDbType.Varchar).Value = entityType;
                command.Parameters.Add("entityId", NpgsqlDbType.Varchar).Value =
                    entityId.ToString();
                command.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
                command.ExecuteNonQuery();
            }
        }

        private static string Hash(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha256.ComputeHash(bytes);

                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
