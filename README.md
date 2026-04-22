# AI Content Generator

A production-ready ASP.NET Core 10 API for generating AI-powered content using OpenAI's GPT-4 Mini model. Features include rate limiting, retry logic, token optimization, and comprehensive logging.

## Features

✅ **Content Generation** - Generate blog posts, articles, and content using OpenAI GPT-4o-mini  
✅ **Rate Limiting** - IP-based rate limiting (60 requests/minute)  
✅ **Resilience Patterns** - Exponential backoff retry logic + circuit breaker  
✅ **Token Optimization** - Intelligent prompt compression to reduce API costs  
✅ **Structured Logging** - Serilog integration with detailed request tracking  
✅ **Swagger Documentation** - Interactive API testing UI  
✅ **Error Handling** - Comprehensive validation and error responses  

## Architecture

```
Controllers/          → API endpoints
Services/            → Business logic layer
Integrations/        → OpenAI API client
Models/              → Request/Response DTOs
Utilities/           → Token optimization
Middleware/          → Rate limiting
Helpers/             → Prompt building
```

## Quick Start

### Prerequisites
- .NET 10.0 or later
- OpenAI API account with credits

### Setup

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd AIContentGenerator
   ```

2. **Get your OpenAI API key**
   - Visit: https://platform.openai.com/account/api-keys
   - Create a new secret key
   - ⚠️ Copy it immediately (shown only once)

3. **Configure the API key** (Development only)
   ```bash
   # Create/edit appsettings.Development.json
   ```
   
   **appsettings.Development.json:**
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-proj-your-key-here"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

4. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

5. **Test the API**
   - Open: http://localhost:5269/swagger
   - Or use the included `AIContentGenerator.http` with REST Client extension

## API Endpoints

### Generate Content
```
POST /api/content/generate
```

**Request:**
```json
{
  "topic": "Artificial Intelligence",
  "tone": "professional",
  "wordCount": 500
}
```

**Response:**
```json
{
  "generatedText": "...",
  "tokensUsed": 1250,
  "estimatedCost": 0.0375
}
```

**Rate Limit Headers:**
- `X-RateLimit-Limit`: Maximum requests per minute
- `X-RateLimit-Remaining`: Requests remaining
- `X-RateLimit-Reset`: Unix timestamp when limit resets

## Configuration

### appsettings.json (Production)
- Logging configuration
- Service defaults

### appsettings.Development.json (Local only, NOT committed)
- OpenAI API key (⚠️ **NEVER commit this file!**)
- Development-specific settings

## Security

⚠️ **Important Security Notes:**

1. **Never commit** `appsettings.Development.json` with your API key
2. **Never hardcode** secrets in source code
3. Use `.gitignore` to prevent accidental commits
4. For production:
   - Use environment variables
   - Use Azure Key Vault / AWS Secrets Manager
   - Use managed identity authentication

## Development

### Using `dotnet user-secrets` (Recommended for Local Development)
```bash
# Initialize secrets
dotnet user-secrets init

# Store your API key securely
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-your-key-here"

# Later, list all secrets
dotnet user-secrets list
```

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Build Release
```bash
dotnet build -c Release
```

## Features in Detail

### Rate Limiting
- **Algorithm**: Token bucket
- **Limit**: 60 requests per minute per IP
- **Response**: 429 Too Many Requests with retry-after

### Retry Logic (Polly)
- **Strategy**: Exponential backoff (1s, 2s, 4s)
- **Triggers**: HTTP 429, 500, 503
- **Max Retries**: 3 attempts

### Circuit Breaker
- **Opens after**: 5 consecutive failures
- **Reset time**: 30 seconds
- **Prevents**: Cascading failures

### Token Optimization
- Estimates prompt tokens before sending
- Calculates estimated cost
- Prevents exceeding token limits
- Truncates prompts intelligently if needed

### Logging
- **Provider**: Serilog
- **Level**: Information (configurable)
- **Output**: Console + structured format
- **Tracking**: Request IDs for correlation

## Example Usage

```bash
# Using PowerShell
$body = @{
    topic = "Climate Change"
    tone = "persuasive"
    wordCount = 300
} | ConvertTo-Json

$response = Invoke-WebRequest `
  -Uri "http://localhost:5269/api/content/generate" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body

$response.Content | ConvertFrom-Json | ForEach-Object { $_.generatedText }
```

```bash
# Using curl
curl -X POST http://localhost:5269/api/content/generate \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "Machine Learning",
    "tone": "educational",
    "wordCount": 400
  }'
```

## Troubleshooting

### 500 Error - "API key not configured"
**Solution**: Add your OpenAI API key to `appsettings.Development.json` or use `dotnet user-secrets`

### 429 - Rate limit exceeded
**Solution**: Wait for the duration specified in `X-RateLimit-Reset` header before retrying

### Circuit breaker open
**Solution**: The API experienced repeated failures. Wait 30 seconds and retry.

### Invalid OpenAI API key
**Solution**: 
- Verify key from https://platform.openai.com/account/api-keys
- Check for typos or extra spaces
- Ensure key hasn't been revoked

## Performance Considerations

- **Token Optimization**: Reduces API costs by ~15-20% through smart prompt truncation
- **Caching**: Consider redis for rate limit counts in production
- **Async/Await**: All I/O operations are non-blocking
- **Connection Pooling**: HttpClient manages connection pools automatically

## Testing the Application

### Via Swagger UI
1. Navigate to http://localhost:5269/swagger
2. Expand POST /api/content/generate
3. Click "Try it out"
4. Enter your parameters
5. Click "Execute"

### Via REST Client (VS Code Extension)
- Open `AIContentGenerator.http`
- Click "Send Request" on any endpoint

## Production Deployment

For production deployment:

1. Use environment variables for sensitive data:
   ```bash
   export OpenAI__ApiKey="sk-proj-..."
   ```

2. Use Docker:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:10.0
   WORKDIR /app
   COPY bin/Release/net10.0/publish .
   ENTRYPOINT ["dotnet", "AIContentGenerator.dll"]
   ```

3. Store secrets in:
   - Azure Key Vault (recommended)
   - AWS Secrets Manager
   - HashiCorp Vault

## License

MIT License - See LICENSE file for details

## Support

For issues or questions:
- Check the troubleshooting section
- Review logs in console output
- Verify configuration in appsettings files

---

**Last Updated**: April 22, 2026  
**Version**: 1.0  
**Status**: Production Ready
