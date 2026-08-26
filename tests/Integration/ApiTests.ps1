$ErrorActionPreference = 'Stop'

$base = 'http://localhost:8088/api/v1'
$headers = @{
    'X-Api-Key' = 'demo-local-key'
    'X-Actor' = 'integration.test'
}

function Assert($condition, $message) {
    if (-not $condition) { throw "ASSERTION FAILED: $message" }
}

function Post([string]$path, $body, [string]$key = ([guid]::NewGuid().ToString())) {
    $requestHeaders = $headers.Clone()
    $requestHeaders['Idempotency-Key'] = $key
    Invoke-RestMethod -Uri "$base/$path" -Method Post -Headers $requestHeaders `
        -ContentType 'application/json' -Body ($body | ConvertTo-Json)
}

function Post-Raw([string]$path, [string]$body, [string]$key = ([guid]::NewGuid().ToString())) {
    $requestHeaders = $headers.Clone()
    $requestHeaders['Idempotency-Key'] = $key
    Invoke-RestMethod -Uri "$base/$path" -Method Post -Headers $requestHeaders `
        -ContentType 'application/json' -Body $body
}

function Assert-ApiError(
    [scriptblock]$action,
    [int]$expectedStatus,
    [string]$expectedCode,
    [string]$message
) {
    try {
        & $action | Out-Null
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw }

        Assert ([int]$response.StatusCode -eq $expectedStatus) `
            "$message returned HTTP $([int]$response.StatusCode), expected $expectedStatus"

        if ($expectedCode) {
            $responseBody = $_.ErrorDetails.Message
            Assert (-not [string]::IsNullOrWhiteSpace($responseBody)) `
                "$message did not return an error response body"
            $errorPayload = $responseBody | ConvertFrom-Json
            Assert ($errorPayload.code -eq $expectedCode) `
                "$message returned code '$($errorPayload.code)', expected '$expectedCode'"
        }
        return
    }
    throw "ASSERTION FAILED: $message unexpectedly succeeded"
}

$today = (Get-Date).Date
$due = $today.AddDays(3).ToString('yyyy-MM-dd')
$reservationEnd = $today.AddDays(2).ToString('yyyy-MM-dd')

# Read-only endpoint and seeded-data contracts.
$health = Invoke-RestMethod -Uri "$base/health" -Headers $headers
Assert ($health.status -eq 'ok') 'health endpoint'

$tools = Invoke-RestMethod -Uri "$base/tools" -Headers $headers
Assert ($tools.Count -ge 6) 'seeded tools visible'
$borrowed = $tools | Where-Object { $_.toolId -eq 3 }
Assert ($borrowed.loanId -eq 1) 'tool includes open loan ID'
Assert ($borrowed.borrowedBy -eq 'Overdue Owen') 'tool includes borrower'

$member = Invoke-RestMethod -Uri "$base/members/3" -Headers $headers
Assert ($member.checkoutLimit -eq 2) 'member tier details'
Assert ($member.outstandingLoans.Count -eq 1) 'member outstanding loans visible'
Assert ($member.outstandingLoans[0].loanId -eq 1) 'member loan ID visible'
Assert ($member.outstandingLoans[0].tool -eq 'Extension Ladder') 'member loan tool visible'

# Invalid bodies and idempotency keys.
Assert-ApiError { Post-Raw 'checkouts' 'null' } 400 $null 'null checkout body'
Assert-ApiError { Post-Raw 'reservations' 'null' } 400 $null 'null reservation body'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 0; memberId = 1; dueOn = $due } } `
    400 $null 'invalid checkout model'
Assert-ApiError { Post 'returns' @{ loanId = 0 } } 400 $null 'invalid return model'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 1; memberId = 1; dueOn = $due } 'not-a-guid' } `
    400 $null 'malformed idempotency key'

# Reservation success and representative reservation failures.
$reservation = Post 'reservations' @{
    toolId = 6
    memberId = 2
    startsOn = $today.ToString('yyyy-MM-dd')
    endsOn = $reservationEnd
}
Assert ($reservation.id -gt 0) 'reservation created'
Assert ($reservation.status -eq 'ACTIVE') 'reservation is active'

Assert-ApiError {
    Post 'reservations' @{
        toolId = 1; memberId = 1
        startsOn = $today.AddDays(-1).ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 409 'TL001' 'reservation with invalid dates'
Assert-ApiError {
    Post 'reservations' @{
        toolId = 1; memberId = 4
        startsOn = $today.ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 409 'TL002' 'reservation by inactive member'
Assert-ApiError {
    Post 'reservations' @{
        toolId = 5; memberId = 1
        startsOn = $today.ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 409 'TL003' 'reservation for maintenance tool'
Assert-ApiError {
    Post 'reservations' @{
        toolId = 2; memberId = 1
        startsOn = $today.ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 409 'TL003' 'reservation for already-reserved tool'
Assert-ApiError {
    Post 'reservations' @{
        toolId = 999; memberId = 1
        startsOn = $today.ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 404 'TL404' 'reservation for missing tool'
Assert-ApiError {
    Post 'reservations' @{
        toolId = 1; memberId = 999
        startsOn = $today.ToString('yyyy-MM-dd'); endsOn = $reservationEnd
    }
} 404 'TL404' 'reservation for missing member'

# Checkout success, idempotency, and business-rule failures.
$idempotencyKey = [guid]::NewGuid().ToString()
$checkoutBody = @{ toolId = 1; memberId = 1; dueOn = $due }
$firstCheckout = Post 'checkouts' $checkoutBody $idempotencyKey
$replayedCheckout = Post 'checkouts' $checkoutBody $idempotencyKey
Assert ($firstCheckout.id -eq $replayedCheckout.id) 'idempotent checkout'

Assert-ApiError `
    { Post 'checkouts' @{ toolId = 1; memberId = 4; dueOn = $due } } `
    409 'TL002' 'checkout by inactive member'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 5; memberId = 1; dueOn = $due } } `
    409 'TL003' 'maintenance checkout'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 6; memberId = 3; dueOn = $due } } `
    409 'TL005' 'overdue-member checkout'
Assert-ApiError {
    Post 'checkouts' @{
        toolId = 1; memberId = 1
        dueOn = $today.AddDays(60).ToString('yyyy-MM-dd')
    }
} 409 'TL007' 'checkout with invalid due date'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 2; memberId = 1; dueOn = $due } } `
    409 'TL008' 'checkout reserved by another member'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 999; memberId = 1; dueOn = $due } } `
    404 'TL404' 'checkout for missing tool'
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 1; memberId = 999; dueOn = $due } } `
    404 'TL404' 'checkout for missing member'

# Return success, late-fee calculation, and duplicate-return rejection.
$returned = Post 'returns' @{ loanId = 2 }
Assert ($returned.lateFee -gt 0) 'late fee calculated'
Assert-ApiError { Post 'returns' @{ loanId = 2 } } 409 'TL009' 'duplicate return'
Assert-ApiError { Post 'returns' @{ loanId = 999 } } 404 'TL404' 'return for missing loan'

# Successful writes produce audit records.
$audit = Invoke-RestMethod -Uri "$base/audit?take=50" -Headers $headers
Assert (@($audit | Where-Object { $_.operation -eq 'RESERVE' }).Count -ge 1) `
    'reservation audited'
Assert (@($audit | Where-Object { $_.operation -eq 'CHECKOUT' }).Count -ge 1) `
    'checkout audited'
Assert (@($audit | Where-Object { $_.operation -eq 'RETURN' }).Count -ge 1) `
    'return audited'

# Two callers compete for the same reserved tool; only one can succeed.
$jobs = 1..2 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $requestHeaders, $checkoutDue)
        $requestHeaders['Idempotency-Key'] = [guid]::NewGuid().ToString()
        try {
            Invoke-WebRequest -UseBasicParsing -Uri "$url/checkouts" -Method Post `
                -Headers $requestHeaders -ContentType 'application/json' `
                -Body (@{ toolId = 6; memberId = 2; dueOn = $checkoutDue } | ConvertTo-Json) `
                | Out-Null
            200
        }
        catch {
            [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $base, $headers.Clone(), $due
}

$statusCodes = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
Assert (@($statusCodes | Where-Object { $_ -eq 200 }).Count -eq 1) `
    'exactly one concurrent checkout succeeded'
Assert (@($statusCodes | Where-Object { $_ -eq 409 }).Count -eq 1) `
    'competing checkout conflicted'

# Reset to construct a deterministic checkout-limit scenario.
& "$PSScriptRoot\..\..\scripts\Reset-Demo.ps1" -Force
Post 'checkouts' @{ toolId = 1; memberId = 1; dueOn = $due } | Out-Null
Post 'checkouts' @{ toolId = 6; memberId = 1; dueOn = $due } | Out-Null
Assert-ApiError `
    { Post 'checkouts' @{ toolId = 5; memberId = 1; dueOn = $due } } `
    409 'TL006' 'member checkout limit'

# Authentication boundary.
Assert-ApiError { Invoke-RestMethod -Uri "$base/tools" } 401 $null 'unauthorized request'

Write-Host 'API integration tests passed.'
