using System.Diagnostics;
using VideoStreamingCDN.Models;

namespace VideoStreamingCDN.Services;

public class OptimizedVideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OptimizedVideoProcessingService> _logger;
    private readonly string _videoStoragePath;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly bool _enableHardwareAcceleration;
    private readonly string _hardwareEncoder;
    private readonly string _preset;
    private readonly int _crf;
    private readonly int _hlsSegmentTime;

    public OptimizedVideoProcessingService(IConfiguration configuration, ILogger<OptimizedVideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _videoStoragePath = _configuration["VideoStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "videos");
        _ffmpegPath = _configuration["FFmpeg:ExecutablePath"] ?? "ffmpeg";
        _ffprobePath = _configuration["FFmpeg:FFprobePath"] ?? "ffprobe";
        _enableHardwareAcceleration = _configuration.GetValue<bool>("FFmpeg:EnableHardwareAcceleration", false);
        _hardwareEncoder = _configuration["FFmpeg:HardwareEncoder"] ?? "h264_nvenc";
        _preset = _configuration["FFmpeg:DefaultPreset"] ?? "ultrafast";
        _crf = _configuration.GetValue<int>("FFmpeg:DefaultCRF", 28);
        _hlsSegmentTime = _configuration.GetValue<int>("FFmpeg:HLSSegmentTime", 6);
        
        ValidateFFmpegAvailability();
    }

    public async Task<VideoUploadResponse> ProcessVideoAsync(VideoUploadRequest request)
    {
        var videoId = request.VideoId;
        var videoDirectory = Path.Combine(_videoStoragePath, videoId);
        Directory.CreateDirectory(videoDirectory);

        // Save file with streaming for better performance
        var tempVideoPath = Path.Combine(videoDirectory, "temp_" + request.VideoFile.FileName);
        await SaveFileStreamingAsync(request.VideoFile, tempVideoPath);

        _logger.LogInformation($"Video saved to: {tempVideoPath}");

        try
        {
            // Quick format check - don't do full encoding check
            var isOptimalFormat = await IsOptimalFormatAsync(tempVideoPath);
            
            if (isOptimalFormat)
            {
                // Direct HLS conversion without re-encoding
                await ConvertToHLSDirectAsync(tempVideoPath, videoDirectory);
            }
            else
            {
                // Fast encoding with hardware acceleration if available
                await ConvertToHLSWithEncodingAsync(tempVideoPath, videoDirectory);
            }

            // Clean up temp file
            File.Delete(tempVideoPath);

            var videoUrl = $"http://localhost:5288/videos/{videoId}/master.m3u8";
            
            _logger.LogInformation($"Video processing completed for {videoId}");

            return new VideoUploadResponse
            {
                VideoId = videoId,
                CdnUrl = videoUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing video {videoId}");
            
            // Clean up on error
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);
                
            throw;
        }
    }

    private async Task SaveFileStreamingAsync(IFormFile file, string outputPath)
    {
        const int bufferSize = 1024 * 1024; // 1MB buffer
        using var inputStream = file.OpenReadStream();
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        
        await inputStream.CopyToAsync(outputStream);
        await outputStream.FlushAsync();
    }

    private async Task<bool> IsOptimalFormatAsync(string videoPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -select_streams v:0 -show_entries stream=codec_name,width,height -of csv=p=0 \"{videoPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) return false;

            var parts = output.Trim().Split(',');
            if (parts.Length >= 3)
            {
                var codec = parts[0].Trim();
                var width = int.TryParse(parts[1], out var w) ? w : 0;
                var height = int.TryParse(parts[2], out var h) ? h : 0;

                // Consider optimal if already H.264 and reasonable resolution
                return codec == "h264" && width <= 1920 && height <= 1080;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check video format");
            return false;
        }
    }

    private async Task ConvertToHLSDirectAsync(string inputPath, string outputDirectory)
    {
        var playlistPath = Path.Combine(outputDirectory, "master.m3u8");
        var segmentPattern = Path.Combine(outputDirectory, "segment_%03d.ts");

        var arguments = $"-i \"{inputPath}\" " +
                       $"-c copy " + // Copy codecs without re-encoding
                       $"-hls_time {_hlsSegmentTime} " +
                       $"-hls_list_size 0 " +
                       $"-hls_flags delete_segments " +
                       $"-hls_segment_filename \"{segmentPattern}\" " +
                       $"\"{playlistPath}\"";

        await RunFFmpegAsync(arguments, "Direct HLS conversion");
    }

    private async Task ConvertToHLSWithEncodingAsync(string inputPath, string outputDirectory)
    {
        var playlistPath = Path.Combine(outputDirectory, "master.m3u8");
        var segmentPattern = Path.Combine(outputDirectory, "segment_%03d.ts");

        var videoCodec = _enableHardwareAcceleration ? _hardwareEncoder : "libx264";
        var encodingOptions = _enableHardwareAcceleration 
            ? $"-c:v {videoCodec} -preset {_preset} -crf {_crf}"
            : $"-c:v {videoCodec} -preset {_preset} -crf {_crf} -x264-params keyint=60:min-keyint=60";

        var arguments = $"-i \"{inputPath}\" " +
                       $"{encodingOptions} " +
                       $"-c:a aac -b:a 128k " +
                       $"-hls_time {_hlsSegmentTime} " +
                       $"-hls_list_size 0 " +
                       $"-hls_flags delete_segments " +
                       $"-hls_segment_filename \"{segmentPattern}\" " +
                       $"\"{playlistPath}\"";

        await RunFFmpegAsync(arguments, "HLS conversion with encoding");
    }

    private async Task RunFFmpegAsync(string arguments, string operation)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _logger.LogInformation($"Starting {operation}: {arguments}");

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new InvalidOperationException($"Failed to start FFmpeg for {operation}");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            var errorMessage = $"{operation} failed with exit code {process.ExitCode}. Error: {error}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation($"{operation} completed successfully");
    }

    private void ValidateFFmpegAvailability()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit(5000); // 5 second timeout
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"FFmpeg validated successfully at: {_ffmpegPath}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"FFmpeg validation failed at path: {_ffmpegPath}");
        }

        var errorMessage = $"FFmpeg is not available at path: {_ffmpegPath}. " +
                          "Please install FFmpeg and ensure it's in your PATH or update the FFmpeg:ExecutablePath configuration.";
        _logger.LogError(errorMessage);
        _logger.LogWarning("Video processing will be disabled until FFmpeg is properly configured.");
    }
}
