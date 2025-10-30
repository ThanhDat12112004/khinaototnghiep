# Video Streaming CDN

A .NET 9 microservice for video upload, processing, and CDN delivery with HLS streaming support.

## Prerequisites

### FFmpeg Installation

This application requires FFmpeg for video processing. Choose one of the following installation methods:

#### Option 1: Download and Extract (Recommended for Windows)

1. Download FFmpeg from https://ffmpeg.org/download.html
2. Extract to a folder (e.g., `C:\ffmpeg`)
3. Add the `bin` folder to your system PATH or update the configuration

#### Option 2: Using Package Manager

**Windows (Chocolatey):**
```powershell
choco install ffmpeg
```

**Windows (Winget):**
```powershell
winget install ffmpeg
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install ffmpeg
```

**macOS (Homebrew):**
```bash
brew install ffmpeg
```

### Configuration

Update `appsettings.json` to point to your FFmpeg installation:

```json
{
  "FFmpeg": {
    "ExecutablePath": "ffmpeg",          // or "C:\\ffmpeg\\bin\\ffmpeg.exe"
    "FFprobePath": "ffprobe",            // or "C:\\ffmpeg\\bin\\ffprobe.exe"
    "DefaultPreset": "medium",
    "DefaultCRF": 23,
    "HLSSegmentTime": 10
  }
}
```

## Features

- **Video Upload**: Support for MP4, MOV, AVI, MKV, WEBM formats
- **Video Processing**: Automatic H.264/AAC encoding
- **HLS Streaming**: Convert videos to HLS format for adaptive streaming
- **CDN Ready**: Optimized for CDN delivery
- **Health Checks**: Built-in health monitoring
- **Error Handling**: Comprehensive error handling and logging

## API Endpoints

### Upload Video
```http
POST /api/v1/videos/upload
Content-Type: multipart/form-data

videoId: string (required)
videoFile: file (required)
```

### Health Check
```http
GET /health
GET /api/v1/videos/health
```

### Cleanup Old Videos
```http
POST /api/v1/videos/cleanup?olderThanDays=7
```

## Running the Application

1. Ensure FFmpeg is installed and configured
2. Run the application:
   ```bash
   dotnet run
   ```
3. The application will start on `https://localhost:5001` (or as configured)
4. Check logs to verify FFmpeg availability

## Configuration

### Video Storage
```json
{
  "VideoStorage": {
    "Path": "videos",
    "MaxFileSize": 524288000,
    "CleanupOlderThanDays": 30
  }
}
```

### CDN Settings
```json
{
  "CDN": {
    "BaseUrl": "https://cdn.example.com"
  }
}
```

## File Structure

- `videos/` - Video storage directory (auto-created)
- `Controllers/` - API controllers
- `Services/` - Business logic services
- `Models/` - Data models
- `Utils/` - Utility classes
- `Middleware/` - Custom middleware
- `VideoPlayer/` - Sample HTML5 video player

## Troubleshooting

### FFmpeg Not Found Error
```
System.ComponentModel.Win32Exception (2): The system cannot find the file specified.
```

**Solutions:**
1. Install FFmpeg using one of the methods above
2. Update `appsettings.json` with the correct FFmpeg path
3. Ensure FFmpeg is in your system PATH

### Large File Upload Issues
- Check `VideoStorage:MaxFileSize` configuration
- Verify server timeout settings
- Consider using chunked upload for very large files

## License

MIT License
