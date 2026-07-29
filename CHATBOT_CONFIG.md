# Cấu hình Chatbot - Google Gemini via YeScale

## ⚠️ Bảo Mật

**KHÔNG BƯỚC NHƯ:**
- ❌ Không lưu API key trong `appsettings.json` hoặc code
- ❌ Không commit API key vào Git
- ❌ Không chia sẻ API key qua message/email

**PHẢI LÀM:**
- ✅ Lưu API key trong **Environment Variables**
- ✅ Hoặc dùng **User Secrets** (Development)
- ✅ Hoặc dùng **Azure Key Vault** (Production)

---

## 🔧 Cấu hình Development

### Option 1: Environment Variable (Khuyên dùng)

#### Windows (PowerShell):
```powershell
$env:Chatbot__ApiKey = "sk-Mt8bI5pN80WST32n3xW1N2inNoNL9esfnSrRlYFaeMff1L0g"
cd "C:\Users\naml4\Downloads\Web_ban_đt\Web_ban_đt"
dotnet run
```

#### Windows (Command Prompt):
```cmd
set Chatbot__ApiKey=sk-Mt8bI5pN80WST32n3xW1N2inNoNL9esfnSrRlYFaeMff1L0g
cd C:\Users\naml4\Downloads\Web_ban_đt\Web_ban_đt
dotnet run
```

#### Linux/Mac:
```bash
export Chatbot__ApiKey="sk-Mt8bI5pN80WST32n3xW1N2inNoNL9esfnSrRlYFaeMff1L0g"
cd Web_ban_đt
dotnet run
```

### Option 2: User Secrets (Development Only)

```bash
cd "C:\Users\naml4\Downloads\Web_ban_đt\Web_ban_đt"
dotnet user-secrets init
dotnet user-secrets set "Chatbot:ApiKey" "sk-Mt8bI5pN80WST32n3xW1N2inNoNL9esfnSrRlYFaeMff1L0g"
```

Kiểm tra:
```bash
dotnet user-secrets list
```

### Option 3: .env File (Không commit)

Tạo file `.env` trong thư mục project (thêm vào `.gitignore`):

```env
Chatbot__ApiKey=sk-Mt8bI5pN80WST32n3xW1N2inNoNL9esfnSrRlYFaeMff1L0g
Chatbot__Provider=Google
Chatbot__Model=gemini-2.5-flash-lite
```

---

## 📋 Cấu hình Mặc định

File: `appsettings.json`

```json
{
  "Chatbot": {
    "Provider": "Google",
    "Model": "gemini-2.5-flash-lite",
    "ApiKey": "",
    "ApiUrl": "https://api.yescale.io/v1beta/models/gemini-2.5-flash-lite:generateContent",
    "ApiVersion": "v1beta",
    "MaxRetrievedChunks": 8
  }
}
```

### Để chuyển sang OpenAI:
```json
{
  "Chatbot": {
    "Provider": "OpenAI",
    "Model": "gpt-4o",
    "ApiKey": "",
    "ApiUrl": "https://api.yescale.io/v1/chat/completions",
    "ApiVersion": "v1",
    "MaxRetrievedChunks": 8
  }
}
```

---

## 🚀 Production Deployment

### Heroku/Azure/Vercel:

Thêm biến môi trường qua dashboard hoặc CLI:

**Azure:**
```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myWebApp \
  --settings Chatbot__ApiKey="your-api-key"
```

**Heroku:**
```bash
heroku config:set Chatbot__ApiKey="your-api-key"
```

---

## ✅ Kiểm Tra Cấu Hình

Sau khi set environment variable, app sẽ tự động load API key.

### Test API:

```bash
# Test chatbot endpoint
curl -X POST http://localhost:5178/chatbot/ask \
  -H "Content-Type: application/json" \
  -d '{"message":"Điện thoại nào tốt dưới 5 triệu?"}'
```

### Debug Log:

Mở `appsettings.Development.json` để set log level:

```json
{
  "Logging": {
    "LogLevel": {
      "TechStoreWeb.Services": "Debug"
    }
  }
}
```

---

## 🔄 Chuyển đổi giữa OpenAI và Google

1. Update `appsettings.json` (model & URL)
2. Set `Chatbot__ApiKey` environment variable
3. Restart app

**Code tự động detect:**
- Google API nếu URL chứa `generateContent`
- OpenAI API nếu URL chứa `/chat/completions`

---

## 📞 Troubleshooting

| Vấn đề | Giải pháp |
|--------|----------|
| "Dịch vụ đang bảo trì" | API key hết token hoặc invalid. Kiểm tra logs |
| Empty response | Prompt quá dài. Giảm `MaxRetrievedChunks` từ 8 → 5 |
| Authentication failed | API key sai format. Kiểm tra `echo $Chatbot__ApiKey` |
| Timeout (>5s) | YeScale bị slow. Try retry hoặc switch provider |

### Logs:
```bash
tail -f /tmp/dotnet.log
```

---

## 🔐 Security Checklist

- [ ] API key không trong `appsettings.json`
- [ ] `.env` được add vào `.gitignore`
- [ ] Không share API key qua public channels
- [ ] Rotate API key định kỳ
- [ ] Use different keys cho Dev/Staging/Prod
- [ ] Monitor usage qua YeScale dashboard

---

## 📖 Tài liệu Tham Khảo

- [YeScale Docs](https://docs.yescale.io)
- [Google Gemini API](https://ai.google.dev)
- [ASP.NET Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)
