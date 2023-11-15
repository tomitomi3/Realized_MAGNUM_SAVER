# IPアドレス 送る文字(0～9）
param (
    [Parameter(Mandatory=$true)]
    [string]$ipAddress,

    [Parameter(Mandatory=$true)]
    [string]$message
)

# IPアドレスとポート番号
$endpoint = New-Object System.Net.IPEndPoint ([System.Net.IPAddress]::Parse($ipAddress), 12345)
$udpclient = New-Object System.Net.Sockets.UdpClient
$byteData = [Text.Encoding]::ASCII.GetBytes($message)
$udpclient.Send($byteData, $byteData.Length, $endpoint)
$udpclient.Close()
