$ErrorActionPreference='Stop';$base='http://localhost:8088/api/v1';$headers=@{'X-Api-Key'='demo-local-key';'X-Actor'='integration.test'}
function Assert($ok,$message){if(-not $ok){throw "ASSERTION FAILED: $message"}}
function Post($path,$body,$key=([guid]::NewGuid().ToString())){$h=$headers.Clone();$h['Idempotency-Key']=$key;Invoke-RestMethod -Uri "$base/$path" -Method Post -Headers $h -ContentType 'application/json' -Body ($body|ConvertTo-Json)}
$health=Invoke-RestMethod -Uri "$base/health" -Headers $headers;Assert ($health.status-eq'ok') 'health endpoint'
$tools=Invoke-RestMethod -Uri "$base/tools" -Headers $headers;Assert ($tools.Count-ge6) 'seeded tools visible'
$member=Invoke-RestMethod -Uri "$base/members/1" -Headers $headers;Assert ($member.checkoutLimit-eq2) 'member tier details'
$key=[guid]::NewGuid().ToString();$due=(Get-Date).Date.AddDays(3).ToString('yyyy-MM-dd');$one=Post 'checkouts' @{toolId=1;memberId=1;dueOn=$due} $key;$two=Post 'checkouts' @{toolId=1;memberId=1;dueOn=$due} $key;Assert ($one.id-eq$two.id) 'idempotent checkout'
try{Post 'checkouts' @{toolId=5;memberId=1;dueOn=$due}|Out-Null;throw 'maintenance checkout succeeded'}catch{Assert ($_.Exception.Response.StatusCode.value__-eq409) 'maintenance rejected'}
try{Post 'checkouts' @{toolId=6;memberId=3;dueOn=$due}|Out-Null;throw 'overdue checkout succeeded'}catch{Assert ($_.Exception.Response.StatusCode.value__-eq409) 'overdue rejected'}
$returned=Post 'returns' @{loanId=2};Assert ($returned.lateFee-gt0) 'late fee calculated'
$audit=Invoke-RestMethod -Uri "$base/audit?take=50" -Headers $headers;Assert (($audit|Where-Object {$_.operation -eq 'CHECKOUT'}).Count -ge 1) 'checkout audited';Assert (($audit|Where-Object {$_.operation -eq 'RETURN'}).Count -ge 1) 'return audited'
$jobs=1..2|ForEach-Object{Start-Job -ScriptBlock {param($u,$h,$d)$h['Idempotency-Key']=[guid]::NewGuid().ToString();try{Invoke-WebRequest -UseBasicParsing -Uri "$u/checkouts" -Method Post -Headers $h -ContentType 'application/json' -Body (@{toolId=6;memberId=2;dueOn=$d}|ConvertTo-Json)|Out-Null;200}catch{[int]$_.Exception.Response.StatusCode}} -ArgumentList $base,$headers,$due};$codes=$jobs|Wait-Job|Receive-Job;$jobs|Remove-Job;Assert (($codes|Where-Object{$_-eq200}).Count-eq1) 'exactly one concurrent checkout succeeded';Assert (($codes|Where-Object{$_-eq409}).Count-eq1) 'competing checkout conflicted'
try{Invoke-RestMethod -Uri "$base/tools"|Out-Null;throw 'unauthorized request succeeded'}catch{Assert ($_.Exception.Response.StatusCode.value__-eq401) 'unauthorized rejected'}
Write-Host 'API integration tests passed.'
