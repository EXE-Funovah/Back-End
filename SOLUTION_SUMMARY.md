# SOLUTION SUMMARY: 403 Forbidden Error Fixed

## Problem
Your AI service at `ai.mascoteach.com` was getting **403 Forbidden** errors when trying to access document URLs stored in the database because **presigned URLs expire**.

## Root Cause
- Presigned URLs are temporary (expire after 60 minutes)
- Database was storing the **expiring URL** instead of the permanent S3 Key
- When AI service accessed the URL hours/days later ? **403 Forbidden**

## Solution Implemented

### ?? What Changed (NO DATABASE MIGRATION REQUIRED)

#### 1. **Database Storage Strategy**
- **Old:** `FileUrl` column stored expiring presigned URL
- **New:** `FileUrl` column stores permanent S3 Key

```
Example:
Old: "https://s3.amazonaws.com/bucket/file.pdf?Signature=EXPIRES_SOON"
New: "documents/2025/03/07/abc-123-def-456.pdf"
```

#### 2. **API Response Format**
- **Old:** API returned the stored URL directly
- **New:** API generates fresh presigned URL on every request

```json
{
  "id": 1,
  "s3Key": "documents/2025/03/07/abc-123.pdf",
  "presignedUrl": "https://...?Signature=FRESH_60MIN",
  "uploadedAt": "2025-03-07T10:00:00Z",
  "isDeleted": false
}
```

#### 3. **New Service Method**
Added `GeneratePresignedDownloadUrlAsync()` to create fresh download URLs

### ?? Files Modified

1. **DocumentCreateRequest.cs**
   - Changed from `FileUrl` to `S3Key`
   
2. **DocumentResponse.cs**
   - Added `S3Key` property
   - Added `PresignedUrl` property (generated on-demand)

3. **IS3Service.cs & S3Service.cs**
   - Added `GeneratePresignedDownloadUrlAsync()` method

4. **DocumentService.cs**
   - Stores S3 Key in database (not URL)
   - Generates fresh presigned URLs when returning documents

5. **DocumentProfile.cs**
   - Updated AutoMapper to map `FileUrl` ? `S3Key`

6. **DocumentController.cs**
   - Updated PUT endpoint to accept S3Key

### ?? Upload Flow (Frontend)

```javascript
// Step 1: Get presigned upload URL
const { uploadUrl, s3Key } = await api.post('/api/Document/generate-upload-url', {
  fileName: 'document.pdf',
  contentType: 'application/pdf'
});

// Step 2: Upload to S3
await fetch(uploadUrl, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/pdf' },
  body: file
});

// Step 3: Save S3 KEY to database (NOT URL!)
await api.post('/api/Document', {
  s3Key: s3Key  // ? Store the KEY
});
```

### ?? AI Service Integration

```python
# Your AI service should do this:
def get_document(document_id, auth_token):
    # 1. Call your API to get fresh presigned URL
    response = requests.get(
        f"https://your-api.com/api/Document/{document_id}",
        headers={"Authorization": f"Bearer {auth_token}"}
    )
    
    data = response.json()
    
    # 2. Use the fresh presignedUrl (valid for 60 minutes)
    presigned_url = data['presignedUrl']
    
    # 3. Download from S3
    file_response = requests.get(presigned_url)
    return file_response.content
```

### ? Benefits

1. **No More 403 Errors** - Fresh URLs generated on every request
2. **No Database Migration** - Uses existing `FileUrl` column
3. **Better Security** - URLs expire after 60 minutes
4. **AI Service Compatible** - External services can get fresh URLs via API
5. **Scalable** - S3 handles all file downloads

### ?? Important Notes

- **Database stores:** S3 Key (permanent)
- **API returns:** Fresh presigned URL (expires in 60 min)
- **AI service:** Must call API to get fresh URL each time
- **Frontend:** Must save `s3Key` to database, not `uploadUrl`

### ?? Documentation Created

1. **S3_UPLOAD_GUIDE.md** - Complete frontend implementation guide
2. **AI_SERVICE_INTEGRATION_GUIDE.md** - How your AI service should integrate
3. **Mascoteach_Upload_Postman_Collection.json** - API testing collection

### ?? Testing

Build successful ?

**Test the flow:**
1. Upload a document (store S3 key in database)
2. Call `GET /api/Document/{id}` (get fresh presigned URL)
3. Use presigned URL to download (works even hours later because it's freshly generated)

### ?? Next Steps

1. **Update Frontend Code** - Change to save `s3Key` instead of URL
2. **Update AI Service** - Implement the new integration pattern (see AI_SERVICE_INTEGRATION_GUIDE.md)
3. **Configure AWS** - Ensure your AWS credentials in appsettings.json are correct
4. **Test End-to-End** - Upload ? Save ? Retrieve ? AI Process

---

## Quick Reference

### What to Store in Database
```json
{
  "s3Key": "documents/2025/03/07/abc-123.pdf"
}
```

### What API Returns
```json
{
  "s3Key": "documents/2025/03/07/abc-123.pdf",
  "presignedUrl": "https://mascoteach-documents.s3.amazonaws.com/...?Signature=...",
  "expiresAt": "2025-03-07T11:00:00Z"
}
```

### How AI Service Accesses Files
```
AI Service ? Your API ? Fresh Presigned URL ? S3 Download
(Always works because URL is freshly generated!)
```
