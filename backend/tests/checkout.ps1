$ErrorActionPreference = 'Stop'

$testDatabase = 'ShoppyCheckoutTest_' + [Guid]::NewGuid().ToString('N')
$databaseServer = '(localdb)\MSSQLLocalDB'
$testConnection = "Server=$databaseServer;Database=$testDatabase;Trusted_Connection=True;TrustServerCertificate=True;"
$log = Join-Path $env:TEMP "$testDatabase.stdout"
$errorLog = Join-Path $env:TEMP "$testDatabase.stderr"
$configuration = 'CheckoutTests'
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$origin = "http://127.0.0.1:$port"

function Invoke-Api($method, $path, $status, $body, $session) {
    $request = @{ Uri = "$origin/api/$path"; Method = $method; UseBasicParsing = $true; TimeoutSec = 10 }
    if ($session) { $request.WebSession = $session }
    if ($null -ne $body) {
        $request.ContentType = 'application/json'
        $request.Body = $body | ConvertTo-Json -Depth 5 -Compress
    }

    try {
        $response = Invoke-WebRequest @request
        $actual = [int]$response.StatusCode
    } catch {
        if (!$_.Exception.Response) { throw }
        $actual = [int]$_.Exception.Response.StatusCode
    }

    if ($actual -ne $status) { throw "$method $path returned $actual, expected $status." }
    if ($actual -in @(200, 201) -and $response.Content) {
        return $response.Content | ConvertFrom-Json
    }
}

function Get-Stock($productId, $session) {
    (Invoke-Api GET "products/$productId" 200 $null $session).stock
}

$overrides = @{
    ConnectionStrings__DefaultConnection = $testConnection
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $origin
    Email__Provider = 'Console'
    Frontend__BaseUrl = $origin
}
$previous = @{}
Push-Location (Join-Path $PSScriptRoot '..')
try {
    foreach ($setting in $overrides.GetEnumerator()) {
        $previous[$setting.Key] = [Environment]::GetEnvironmentVariable($setting.Key)
        [Environment]::SetEnvironmentVariable($setting.Key, $setting.Value)
    }

    dotnet build --configuration $configuration
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }

    dotnet ef database update --configuration $configuration --no-build --connection $testConnection | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Test database migration failed.' }

    $app = Start-Process dotnet -ArgumentList "bin/$configuration/net10.0/backend.dll" -WindowStyle Hidden -PassThru -RedirectStandardOutput $log -RedirectStandardError $errorLog
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($app.HasExited) { throw 'Test API exited during startup.' }
        if (Select-String -LiteralPath $log -SimpleMatch "Now listening on: $origin" -Quiet) { break }
        Start-Sleep -Milliseconds 250
    }

    $password = 'Test!' + [Guid]::NewGuid().ToString('N')
    $email = 'checkout@example.test'
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $null = Invoke-Api POST 'auth/register-customer' 200 @{
        fullName = 'Checkout Test'; email = $email; password = $password; confirmPassword = $password
    } $null
    $confirmation = [regex]::Match((Get-Content -LiteralPath $log -Raw), 'confirm-email\?userId=(\d+)&token=([A-Za-z0-9_-]+)')
    if (!$confirmation.Success) { throw 'Test account confirmation link was unavailable.' }
    $null = Invoke-Api POST 'auth/confirm-email' 200 @{
        userId = [int]$confirmation.Groups[1].Value; token = $confirmation.Groups[2].Value
    } $null
    $null = Invoke-Api POST 'auth/login' 200 @{ email = $email; password = $password; portal = 'Customer' } $session

    $product = Invoke-Api GET 'products/1' 200 $null $session
    $checkout = @{
        checkoutId = [Guid]::NewGuid().ToString()
        discountPercent = 100
        items = @(@{ productId = $product.id; quantity = 1 })
    }

    $firstOrder = Invoke-Api POST 'orders' 201 $checkout $session
    if ($firstOrder.total -ne $product.unitPrice -or $firstOrder.discountAmount -ne 0) {
        throw 'Customer input changed the price of the order.'
    }

    $retry = Invoke-Api POST 'orders' 201 $checkout $session
    if ($retry.orderDate -cne $firstOrder.orderDate) {
        throw 'Retry changed the order timestamp.'
    }
    if ($retry.id -ne $firstOrder.id -or (Get-Stock $product.id $session) -ne ($product.stock - 1)) {
        throw 'Retry created a different order or deducted stock twice.'
    }

    $checkout.items[0].quantity = 2
    Invoke-Api POST 'orders' 409 $checkout $session
    $null = Invoke-Api PATCH "orders/$($firstOrder.id)/cancel" 200 $null $session
    Invoke-Api PATCH "orders/$($firstOrder.id)/cancel" 400 $null $session
    $checkout.items[0].quantity = 1
    Invoke-Api POST 'orders' 409 $checkout $session
    if ((Get-Stock $product.id $session) -ne $product.stock) {
        throw 'Cancellation did not restore stock exactly once.'
    }

    Add-Type -AssemblyName System.Net.Http
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = $session.Cookies
    $client = [System.Net.Http.HttpClient]::new($handler)
    try {
        $checkout.checkoutId = [Guid]::NewGuid().ToString()
        $checkout.items[0].quantity = 1
        $json = $checkout | ConvertTo-Json -Depth 5 -Compress
        $requests = foreach ($attempt in 1..2) {
            $content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, 'application/json')
            $client.PostAsync("$origin/api/orders", $content)
        }
        $orders = foreach ($request in $requests) {
            $response = $request.GetAwaiter().GetResult()
            try {
                if ([int]$response.StatusCode -ne 201) { throw 'Parallel checkout request failed.' }
                $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
            } finally {
                $response.Dispose()
            }
        }
        if ($orders[0].id -ne $orders[1].id -or (Get-Stock $product.id $session) -ne ($product.stock - 1)) {
            throw 'Parallel retries created multiple orders or deducted stock twice.'
        }
    } finally {
        $client.Dispose()
    }

    Write-Output 'PASS: server pricing, sequential and parallel retries, key conflicts, and inventory restoration.'
} finally {
    try {
        if ($app -and !$app.HasExited) {
            Stop-Process -Id $app.Id
            $app.WaitForExit()
        }
        if ($testDatabase -match '^ShoppyCheckoutTest_[a-f0-9]{32}$') {
            $connection = [System.Data.SqlClient.SqlConnection]::new(
                "Server=$databaseServer;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
            )
            try {
                $connection.Open()
                $query = $connection.CreateCommand()
                $query.CommandText = "IF DB_ID(N'$testDatabase') IS NOT NULL DROP DATABASE [$testDatabase];"
                [void]$query.ExecuteNonQuery()
            } finally {
                $connection.Dispose()
            }
        }
    } finally {
        foreach ($file in @($log, $errorLog)) {
            if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
        }
        foreach ($setting in $previous.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($setting.Key, $setting.Value)
        }
        Pop-Location
    }
}
