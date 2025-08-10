# AWS Services Configuration

This document describes how to configure AWS services for the SpeakStoreLocate API.

## Configuration Structure

The AWS services are configured using the Options pattern with proper validation and environment variable support.

### Supported Services

- **Amazon S3** - File storage
- **Amazon DynamoDB** - NoSQL database
- **Amazon Transcribe** - Speech-to-text service

## Configuration Methods

### 1. appsettings.json Configuration

```json
{
  "AWS": {
    "S3": {
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "Region": "eu-central-1",
      "BucketName": "your-bucket-name"
    },
    "DynamoDB": {
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "Region": "eu-central-1",
      "TableName": "your-table-name"
    },
    "Transcribe": {
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "Region": "eu-central-1",
      "LanguageCode": "de-DE",
      "SampleRateHertz": 16000
    }
  }
}
```

### 2. Environment Variables (Recommended for Production)

Environment variables take precedence over appsettings.json values:

#### S3 Service
- `AWS_S3_ACCESS_KEY`
- `AWS_S3_SECRET_KEY`
- `AWS_S3_REGION`
- `AWS_S3_BUCKET_NAME`

#### DynamoDB Service
- `AWS_DYNAMODB_ACCESS_KEY`
- `AWS_DYNAMODB_SECRET_KEY`
- `AWS_DYNAMODB_REGION`
- `AWS_DYNAMODB_TABLE_NAME`

#### Transcribe Service
- `AWS_TRANSCRIBE_ACCESS_KEY`
- `AWS_TRANSCRIBE_SECRET_KEY`
- `AWS_TRANSCRIBE_REGION`
- `AWS_TRANSCRIBE_LANGUAGE_CODE`
- `AWS_TRANSCRIBE_SAMPLE_RATE`

## Features

### Validation
- All required configuration values are validated at startup
- Missing or invalid configurations will prevent the application from starting
- Clear error messages indicate which configuration is missing

### Type Safety
- Strongly typed options classes with proper defaults
- Regional endpoint resolution
- Credential management

### Dependency Injection
- All AWS services are properly registered in the DI container
- Services are configured as singletons for optimal performance
- DynamoDB context is scoped for proper lifecycle management

## Best Practices

1. **Never commit credentials to source control**
2. **Use environment variables in production**
3. **Use IAM roles when running on AWS infrastructure**
4. **Keep different credentials for different environments**
5. **Regularly rotate access keys**

## Development Setup

For local development, you can use appsettings.Development.json:

```json
{
  "AWS": {
    "S3": {
      "AccessKey": "dev-access-key",
      "SecretKey": "dev-secret-key",
      "Region": "eu-central-1",
      "BucketName": "dev-bucket"
    }
    // ... other services
  }
}
```

## Production Setup

Set environment variables in your production environment:

```bash
export AWS_S3_ACCESS_KEY="prod-access-key"
export AWS_S3_SECRET_KEY="prod-secret-key"
export AWS_S3_REGION="eu-central-1"
export AWS_S3_BUCKET_NAME="prod-bucket"
```