# URI Format Exception - L�sung

## Problem
```
System.UriFormatException: "Invalid URI: The format of the URI could not be determined."
bei Program.cs line 63
```

## Ursache
Das Problem trat auf, weil die OpenAI `BaseUrl` aus der Konfiguration ung�ltig war oder leer, und `new Uri(opts.BaseUrl)` einen ung�ltigen URI-String erhalten hat.

## ? Implementierte L�sung

### 1. **Robuste URI-Validierung**
```csharp
// Validate and ensure BaseUrl is a valid URI
var baseUrl = string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://api.openai.com" : opts.BaseUrl;

if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var validUri))
{
    throw new InvalidOperationException($"OpenAI BaseUrl '{baseUrl}' is not a valid URI. Please check your configuration.");
}

return new OpenAIClient(new ApiKeyCredential(opts.ApiKey), new OpenAIClientOptions
{
    Endpoint = validUri,
});
```

### 2. **Options-Validierung hinzugef�gt**
```csharp
public void Validate()
{
    if (string.IsNullOrWhiteSpace(ApiKey))
        throw new InvalidOperationException("OpenAI ApiKey is required");
    
    if (string.IsNullOrWhiteSpace(BaseUrl))
        throw new InvalidOperationException("OpenAI BaseUrl is required");
    
    if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        throw new InvalidOperationException($"OpenAI BaseUrl '{BaseUrl}' is not a valid URI");
    
    if (Temperature < 0.0 || Temperature > 2.0)
        throw new InvalidOperationException("OpenAI Temperature must be between 0.0 and 2.0");
}
```

### 3. **Startup-Validierung**
```csharp
builder.Services.AddOptionsWithValidateOnStart<OpenAIOptions>()
    .PostConfigure(options => 
    {
        try 
        {
            options.Validate();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"OpenAI Configuration Error: {ex.Message}...", ex);
        }
    });
```

## ?? Debug-Features hinzugef�gt

### **OpenAI Configuration Debug-Logging**
Die Anwendung zeigt jetzt beim Start OpenAI-Konfiguration an:
```
=== OpenAI Configuration Debug ===
OpenAI Configuration:
  ApiKey: sk-p****
  BaseUrl: https://api.openai.com
  DefaultModel: gpt-4.1-nano-2025-04-14
  Temperature: 0
  BaseUrl is valid: True
=== End OpenAI Configuration Debug ===
```

## ?? M�gliche Ursachen des Problems

### 1. **Leere BaseUrl in Konfiguration**
```json
"OpenAI": {
  "BaseUrl": "",  // ? Problem!
  "ApiKey": "sk-..."
}
```

### 2. **Ung�ltige URL-Format**
```json
"OpenAI": {
  "BaseUrl": "not-a-valid-url",  // ? Problem!
  "ApiKey": "sk-..."
}
```

### 3. **Fehlende Konfiguration**
```json
"OpenAI": {
  // BaseUrl fehlt komplett ? Problem!
  "ApiKey": "sk-..."
}
```

## ? Korrekte Konfiguration

### **appsettings.json**
```json
"OpenAI": {
  "ApiKey": "<set in env>",
  "BaseUrl": "https://api.openai.com",
  "DefaultModel": "gpt-4.1-nano-2025-04-14", 
  "Temperature": 0.0
}
```

### **User Secrets (empfohlen)**
```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-..."
dotnet user-secrets set "OpenAI:BaseUrl" "https://api.openai.com"
```

## ?? Verbesserungen

### **Fehlerbehandlung**
- ? Fr�he Validierung beim Startup
- ? Detaillierte Fehlermeldungen  
- ? Fallback auf Standard-BaseUrl
- ? URI-Format-Validierung

### **Debugging**
- ? Konfiguration wird beim Start geloggt
- ? URI-Validit�t wird gepr�ft
- ? Maskierte Credential-Anzeige

### **Robustheit**
- ? Null/Empty-String Behandlung
- ? Uri.TryCreate statt new Uri()
- ? Options-Pattern-Validierung

Der `UriFormatException` sollte jetzt vollst�ndig behoben sein und detaillierte Fehlermeldungen geben, falls Konfigurationsprobleme auftreten! ??