# AWS DynamoDB Authentication Error - Lösung

## Problem
```
Amazon.DynamoDBv2.AmazonDynamoDBException: Invalid key=value pair (missing equal-sign) in Authorization header (hashed with SHA-256 and encoded with Base64)
```

## Ursache
Die AWS Credentials sind nicht korrekt konfiguriert. Die appsettings.json enthält Platzhalter-Werte (`"<set in env>"`), aber die entsprechenden Environment Variables sind nicht gesetzt.

## Lösung

### Option 1: Environment Variables setzen (Empfohlen)

Setzen Sie die folgenden Environment Variables:

```bash
# Windows (PowerShell)
$env:AWS_S3_ACCESS_KEY="YOUR_ACCESS_KEY"
$env:AWS_S3_SECRET_KEY="YOUR_SECRET_KEY"
$env:AWS_DYNAMODB_ACCESS_KEY="YOUR_ACCESS_KEY"  
$env:AWS_DYNAMODB_SECRET_KEY="YOUR_SECRET_KEY"
$env:AWS_DYNAMODB_TABLE_NAME="storage-items"

# Windows (Command Prompt)
set AWS_S3_ACCESS_KEY=YOUR_ACCESS_KEY
set AWS_S3_SECRET_KEY=YOUR_SECRET_KEY
set AWS_DYNAMODB_ACCESS_KEY=YOUR_ACCESS_KEY
set AWS_DYNAMODB_SECRET_KEY=YOUR_SECRET_KEY
set AWS_DYNAMODB_TABLE_NAME=storage-items

# Linux/Mac
export AWS_S3_ACCESS_KEY="YOUR_ACCESS_KEY"
export AWS_S3_SECRET_KEY="YOUR_SECRET_KEY"
export AWS_DYNAMODB_ACCESS_KEY="YOUR_ACCESS_KEY"
export AWS_DYNAMODB_SECRET_KEY="YOUR_SECRET_KEY"
export AWS_DYNAMODB_TABLE_NAME="storage-items"
```

### Option 2: User Secrets (Für Development)

```bash
dotnet user-secrets set "AWS:S3:AccessKey" "YOUR_ACCESS_KEY"
dotnet user-secrets set "AWS:S3:SecretKey" "YOUR_SECRET_KEY"
dotnet user-secrets set "AWS:DynamoDB:AccessKey" "YOUR_ACCESS_KEY"
dotnet user-secrets set "AWS:DynamoDB:SecretKey" "YOUR_SECRET_KEY"
```

### Option 3: appsettings.Development.json (Nicht empfohlen)

Erstellen Sie eine `appsettings.Development.json` mit echten Werten:

```json
{
  "AWS": {
    "S3": {
      "AccessKey": "YOUR_ACCESS_KEY",
      "SecretKey": "YOUR_SECRET_KEY",
      "Region": "eu-central-1",
      "BucketName": "speech-storage-bucket"
    },
    "DynamoDB": {
      "AccessKey": "YOUR_ACCESS_KEY",
      "SecretKey": "YOUR_SECRET_KEY", 
      "Region": "eu-central-1",
      "TableName": "storage-items"
    }
  }
}
```

?? **WICHTIG**: Committen Sie diese Datei NICHT ins Repository!

## Debugging

Die Anwendung zeigt jetzt AWS-Konfiguration beim Start im Development-Modus an. Überprüfen Sie die Logs:

```
=== AWS Configuration Debug ===
S3 Configuration:
  AccessKey: AKIA****
  SecretKey: ****
  Region: eu-central-1
  BucketName: speech-storage-bucket
DynamoDB Configuration:
  AccessKey: AKIA****
  SecretKey: ****
  Region: eu-central-1
  TableName: storage-items
Environment Variables:
  AWS_S3_ACCESS_KEY: AKIA****
  AWS_S3_SECRET_KEY: ****
  AWS_DYNAMODB_ACCESS_KEY: AKIA****
  AWS_DYNAMODB_SECRET_KEY: ****
=== End AWS Configuration Debug ===
```

## Fehlerbehebung

1. **Überprüfen Sie Ihre AWS Credentials**
   - Sind sie korrekt und aktiv?
   - Haben sie die nötigen Permissions?

2. **Überprüfen Sie die Environment Variables**
   - Sind sie in der aktuellen Session gesetzt?
   - Starten Sie die Anwendung nach dem Setzen neu

3. **Überprüfen Sie die Region**
   - Existieren Ihre AWS Resources in der konfigurierten Region?

4. **Permissions prüfen**
   - DynamoDB: `dynamodb:GetItem`, `dynamodb:PutItem`, `dynamodb:DeleteItem`, `dynamodb:Scan`
   - S3: `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject`

## Nächste Schritte nach Lösung

Starten Sie die Anwendung neu. Die verbesserte Konfiguration wird:
- Detaillierte Fehlermeldungen anzeigen
- AWS Configuration beim Start debuggen
- Früh validieren und klare Hinweise geben