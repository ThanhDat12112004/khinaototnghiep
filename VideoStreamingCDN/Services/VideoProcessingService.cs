using System.Diagnostics;
using VideoStreamingCDN.Models;

namespace VideoStreamingCDN.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string _videoStoragePath;
    private readonly string _cdnBaseUrl;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public VideoProcessingService(IConfiguration configuration, ILogger<VideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _videoStoragePath = _configuration["VideoStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "videos");
        _cdnBaseUrl = _configuration["CDN:BaseUrl"] ?? "https://cdn.example.com";
        _ffmpegPath = _configuration["FFmpeg:ExecutablePath"] ?? "ffmpeg";
        _ffprobePath = _configuration["FFmpeg:FFprobePath"] ?? "ffprobe";
        
        // Validate FFmpeg availability at startup
        ValidateFFmpegAvailability();
    }

    public async Task<VideoUploadResponse> ProcessVideoAsync(VideoUploadRequest request)
    {
        try
        {
            // Create video directory
            var videoDirectory = Path.Combine(_videoStoragePath, request.VideoId);
            Directory.CreateDirectory(videoDirectory);

            // Save uploaded file temporarily
            var tempVideoPath = Path.Combine(videoDirectory, "temp_" + request.VideoFile.FileName);
            using (var stream = new FileStream(tempVideoPath, FileMode.Create))
            {
                await request.VideoFile.CopyToAsync(stream);
            }

            _logger.LogInformation($"Video uploaded to temporary path: {tempVideoPath}");

            // Check if video needs encoding
            var needsEncoding = await CheckIfVideoNeedsEncodingAsync(tempVideoPath);

            string sourceVideoPath;
            if (needsEncoding)
            {
                // Encode video to H.264/AAC
                sourceVideoPath = await EncodeVideoAsync(tempVideoPath, videoDirectory);
                // Delete temp file after encoding
                File.Delete(tempVideoPath);
            }
            else
            {
                // Rename temp file to final name
                sourceVideoPath = Path.Combine(videoDirectory, "source.mp4");
                File.Move(tempVideoPath, sourceVideoPath);
            }

            // Convert to HLS
            await ConvertToHLSAsync(sourceVideoPath, videoDirectory);

            // Delete source video after HLS conversion
            File.Delete(sourceVideoPath);

            // Return localhost URL for development
            var videoUrl = $"http://localhost:5288/videos/{request.VideoId}/master.m3u8";

            _logger.LogInformation($"Video processing completed for {request.VideoId}");

            return new VideoUploadResponse
            {
                VideoId = request.VideoId,
                CdnUrl = videoUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing video {request.VideoId}");
            throw;
        }
    }

    private async Task<bool> CheckIfVideoNeedsEncodingAsync(string videoPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -select_streams v:0 -show_entries stream=codec_name -of csv=p=0 \"{videoPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start ffprobe process");

            var videoCodec = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();

            // Check audio codec
            processInfo.Arguments = $"-v quiet -select_streams a:0 -show_entries stream=codec_name -of csv=p=0 \"{videoPath}\"";
            using var audioProcess = Process.Start(processInfo);
            if (audioProcess == null)
                throw new InvalidOperationException("Failed to start ffprobe process for audio");

            var audioCodec = (await audioProcess.StandardOutput.ReadToEndAsync()).Trim();
            await audioProcess.WaitForExitAsync();

            _logger.LogInformation($"Video codec: {videoCodec}, Audio codec: {audioCodec}");

            // Check if already H.264/AAC (or if no audio codec detected, assume needs encoding)
            bool hasValidVideo = videoCodec.Equals("h264", StringComparison.OrdinalIgnoreCase);
            bool hasValidAudio = !string.IsNullOrEmpty(audioCodec) && audioCodec.Equals("aac", StringComparison.OrdinalIgnoreCase);

            return !(hasValidVideo && hasValidAudio);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking video codecs, assuming encoding is needed");
            return true; // Default to encoding if we can't determine
        }
    }

    private async Task<string> EncodeVideoAsync(string inputPath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory, "encoded.mp4");

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-i \"{inputPath}\" -c:v libx264 -c:a aac -preset medium -crf 23 -y \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _logger.LogInformation($"Starting video encoding: {processInfo.Arguments}");

        try
        {
            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start ffmpeg encoding process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"FFmpeg encoding failed with exit code {process.ExitCode}. Error: {error}");
                throw new InvalidOperationException($"Video encoding failed: {error}");
            }

            _logger.LogInformation("Video encoding completed successfully");
            return outputPath;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception)
        {
            var errorMessage = $"Failed to start FFmpeg process. Please ensure FFmpeg is installed and the path '{_ffmpegPath}' is correct.";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task ConvertToHLSAsync(string inputPath, string outputDirectory)
    {
        var playlistPath = Path.Combine(outputDirectory, "master.m3u8");
        var segmentPattern = Path.Combine(outputDirectory, "segment_%03d.ts");

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-i \"{inputPath}\" -c:v copy -c:a copy -hls_time 10 -hls_list_size 0 -hls_segment_filename \"{segmentPattern}\" \"{playlistPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _logger.LogInformation($"Starting HLS conversion: {processInfo.Arguments}");

        try
        {
            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start ffmpeg HLS conversion process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"FFmpeg HLS conversion failed with exit code {process.ExitCode}. Error: {error}");
                throw new InvalidOperationException($"HLS conversion failed: {error}");
            }

            _logger.LogInformation("HLS conversion completed successfully");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception)
        {
            var errorMessage = $"Failed to start FFmpeg process. Please ensure FFmpeg is installed and the path '{_ffmpegPath}' is correct.";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private void ValidateFFmpegAvailability()
    {
        try
        {
            // Test ffmpeg availability
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
                process.WaitForExit(5000); // Wait max 5 seconds
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"FFmpeg is available at: {_ffmpegPath}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"FFmpeg not found at: {_ffmpegPath}");
        }

        // If we reach here, FFmpeg is not available
        var errorMessage = $"FFmpeg is not available at path: {_ffmpegPath}. " +
                          "Please install FFmpeg and ensure it's in your PATH or update the FFmpeg:ExecutablePath configuration.";
        _logger.LogError(errorMessage);
        
        // For now, we'll continue but warn that video processing will fail
        _logger.LogWarning("Video processing will be disabled until FFmpeg is properly configured.");
    }
}
