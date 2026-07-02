# Map root .env → ASP.NET Core configuration (call after load-dotenv.ps1)
$env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Development" }

if ($env:DB_CONNECTION_STRING) {
    $env:ConnectionStrings__dbcontext = $env:DB_CONNECTION_STRING
    $env:ConnectionStrings__MessagingDb = $env:DB_CONNECTION_STRING
}
if ($env:JWT_SECRET) { $env:jwtSettings__Secret = $env:JWT_SECRET }
if ($env:ENCRYPTION_KEY) { $env:EncryptionSettings__Key = $env:ENCRYPTION_KEY }
if ($env:CORS_ALLOWED_ORIGINS) { $env:Cors__AllowedOrigins = $env:CORS_ALLOWED_ORIGINS }
if ($env:RABBITMQ_HOST) { $env:RabbitMQSettings__HostName = $env:RABBITMQ_HOST }
if ($env:RABBITMQ_USER) { $env:RabbitMQSettings__UserName = $env:RABBITMQ_USER }
if ($env:RABBITMQ_PASS) { $env:RabbitMQSettings__Password = $env:RABBITMQ_PASS }
$env:RabbitMQSettings__Port = "5672"

if ($env:EMAIL_HOST) { $env:EmailSettings__Host = $env:EMAIL_HOST }
if ($env:EMAIL_PORT) { $env:EmailSettings__Port = $env:EMAIL_PORT }
if ($env:EMAIL_FROM_NAME) { $env:EmailSettings__FromName = $env:EMAIL_FROM_NAME }
if ($env:EMAIL_FROM_EMAIL) { $env:EmailSettings__FromEmail = $env:EMAIL_FROM_EMAIL }
if ($env:EMAIL_USERNAME) { $env:EmailSettings__UserName = $env:EMAIL_USERNAME }
if ($env:EMAIL_PASSWORD) { $env:EmailSettings__Password = $env:EMAIL_PASSWORD }

if ($env:OSS_ACCESS_KEY_ID) { $env:OssSettings__AccessKeyId = $env:OSS_ACCESS_KEY_ID }
if ($env:OSS_ACCESS_KEY_SECRET) { $env:OssSettings__AccessKeySecret = $env:OSS_ACCESS_KEY_SECRET }
if ($env:OSS_BUCKET) { $env:OssSettings__BucketName = $env:OSS_BUCKET }
if ($env:OSS_REGION) { $env:OssSettings__Region = $env:OSS_REGION }
if ($env:OSS_ENDPOINT) { $env:OssSettings__Endpoint = $env:OSS_ENDPOINT }
if ($env:OSS_PUBLIC_BASE_URL) { $env:OssSettings__PublicBaseUrl = $env:OSS_PUBLIC_BASE_URL }
$env:OssSettings__UseInternalEndpoint = if ($env:OSS_USE_INTERNAL_ENDPOINT) { $env:OSS_USE_INTERNAL_ENDPOINT } else { "false" }

# Messaging API URL when API runs on host (not inside Docker network)
$messagingUrl = if ($env:MESSAGING_API_DEV_URL) { $env:MESSAGING_API_DEV_URL } else { "http://localhost:62901" }
$env:MessagingApi__BaseUrl = $messagingUrl
