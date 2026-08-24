$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8088/api/v1'
$headers = @{
    'X-Api-Key' = 'demo-local-key'
    'X-Actor' = 'integration.test'
}

function Assert($ok, $message) {
    if (-not $ok) { throw "ASSERTION FAILED: $message" }
}

function Post($path, $body, $key = ([guid]::NewGuid().ToString())) {
    $requestHeaders = $headers.Clone()
    $requestHeaders['Idempotency-Key'] = $key
    Invoke-RestMethod -Uri "$base/$path" -Method Post -Headers $requestHeaders `
        -ContentType 'application/json' -Body ($body | ConvertTo-Json)
}

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

$key = [guid]::NewGuid().ToString()
$due = (Get-Date).Date.AddDays(3).ToString('yyyy-MM-dd')
$one = Post 'checkouts' @{ toolId = 1; memberId = 1; dueOn = $due } $key
$two = Post 'checkouts' @{ toolId = 1; memberId = 1; dueOn = $due } $key
Assert ($one.id -eq $two.id) 'idempotent checkout'

try {
    Post 'checkouts' @{ toolId = 5; memberId = 1; dueOn = $due } | Out-Null
    throw 'maintenance checkout succeeded'
}
catch {
    Assert ($_.Exception.Response.StatusCode.value__ -eq 409) 'maintenance rejected'
}

try {
    Post 'checkouts' @{ toolId = 6; memberId = 3; dueOn = $due } | Out-Null
    throw 'overdue checkout succeeded'
}
catch {
    Assert ($_.Exception.Response.StatusCode.value__ -eq 409) 'overdue rejected'
}

$returned = Post 'returns' @{ loanId = 2 }
Assert ($returned.lateFee -gt 0) 'late fee calculated'

$audit = Invoke-RestMethod -Uri "$base/audit?take=50" -Headers $headers
Assert (@($audit | Where-Object { $_.operation -eq 'CHECKOUT' }).Count -ge 1) 'checkout audited'
Assert (@($audit | Where-Object { $_.operation -eq 'RETURN' }).Count -ge 1) 'return audited'

$jobs = 1..2 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $requestHeaders, $dueOn)
        $requestHeaders['Idempotency-Key'] = [guid]::NewGuid().ToString()
        try {
            Invoke-WebRequest -UseBasicParsing -Uri "$url/checkouts" -Method Post `
                -Headers $requestHeaders -ContentType 'application/json' `
                -Body (@{ toolId = 6; memberId = 2; dueOn = $dueOn } | ConvertTo-Json) | Out-Null
            200
        }
        catch {
            [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $base, $headers, $due
}
$codes = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
Assert (@($codes | Where-Object { $_ -eq 200 }).Count -eq 1) 'exactly one concurrent checkout succeeded'
Assert (@($codes | Where-Object { $_ -eq 409 }).Count -eq 1) 'competing checkout conflicted'

try {
    Invoke-RestMethod -Uri "$base/tools" | Out-Null
    throw 'unauthorized request succeeded'
}
catch {
    Assert ($_.Exception.Response.StatusCode.value__ -eq 401) 'unauthorized rejected'
}

Write-Host 'API integration tests passed.'
