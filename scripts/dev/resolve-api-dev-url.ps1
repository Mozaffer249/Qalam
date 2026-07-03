function Get-UrlPort {
    param([string]$Url)
    $uri = [Uri]$Url
    if ($uri.Port -gt 0) { return $uri.Port }
    return if ($uri.Scheme -eq 'https') { 443 } else { 80 }
}

function Test-DevApiPortOpen {
    param([string]$BaseUrl)
    $port = Get-UrlPort $BaseUrl
    foreach ($hostName in @('127.0.0.1', 'localhost')) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $connect = $client.BeginConnect($hostName, $port, $null, $null)
            $opened = $connect.AsyncWaitHandle.WaitOne(500)
            if (-not $opened) {
                $client.Close()
                continue
            }
            $client.EndConnect($connect)
            $client.Close()
            return $true
        }
        catch {
            continue
        }
    }
    return $false
}

function Resolve-DevApiUrl {
    $nativeUrl = if ($env:API_DEV_URL) { $env:API_DEV_URL } else { "http://localhost:62900" }
    $dockerUrl = if ($env:TEACHER_API_URL) { $env:TEACHER_API_URL }
        elseif ($env:ADMIN_API_URL) { $env:ADMIN_API_URL }
        else { "http://localhost:8080" }

    if (Test-DevApiPortOpen $nativeUrl) { return $nativeUrl }
    if ($dockerUrl -ne $nativeUrl -and (Test-DevApiPortOpen $dockerUrl)) { return $dockerUrl }

    return $nativeUrl
}
