// Video Streaming CDN Player - Pure JavaScript with HLS.js
class VideoStreamingApp {
    constructor() {
        this.apiBaseUrl = 'http://localhost:5288/api/v1';
        this.videoBaseUrl = 'http://localhost:5288';
        this.recentUploads = JSON.parse(localStorage.getItem('recentUploads') || '[]');
        this.player = null;

        this.initializeApp();
    }

    initializeApp() {
        this.setupEventListeners();
        this.initializeVideoPlayer();
        this.loadRecentUploads();
        this.checkApiHealth();
    }

    setupEventListeners() {
        const uploadForm = document.getElementById('uploadForm');
        uploadForm.addEventListener('submit', (e) => this.handleUpload(e));

        window.loadVideo = () => this.loadVideoFromUrl();
    }

    initializeVideoPlayer() {
        this.player = document.getElementById('videoPlayer');
        if (!this.player) {
            console.error('Video player element not found');
            this.showStatus('❌ Không tìm thấy video player', 'error');
            return;
        }
        console.log('Video player đã sẵn sàng');
    }

    async checkApiHealth() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/videos/health`);
            const data = await response.json();

            if (response.ok) {
                this.showStatus(`✅ API trạng thái: ${data.status} (v${data.version})`, 'success');
            } else {
                this.showStatus('❌ API không phản hồi', 'error');
            }
        } catch (error) {
            this.showStatus('❌ Không thể kết nối đến API server. Đảm bảo server đang chạy.', 'error');
            console.error('API Health check failed:', error);
        }
    }

    async handleUpload(event) {
        event.preventDefault();

        const videoId = document.getElementById('videoId').value.trim();
        const videoFile = document.getElementById('videoFile').files[0];

        // Validation
        if (!videoId) {
            this.showStatus('❌ Vui lòng nhập Video ID', 'error');
            return;
        }

        if (!videoFile) {
            this.showStatus('❌ Vui lòng chọn file video', 'error');
            return;
        }

        // Check file size (500MB limit)
        const maxSize = 500 * 1024 * 1024;
        if (videoFile.size > maxSize) {
            this.showStatus(`❌ File quá lớn. Tối đa ${maxSize / (1024 * 1024)}MB`, 'error');
            return;
        }

        // Check file type
        const allowedTypes = ['.mp4', '.mov', '.avi', '.mkv', '.webm'];
        const fileExt = '.' + videoFile.name.split('.').pop().toLowerCase();
        if (!allowedTypes.includes(fileExt)) {
            this.showStatus(`❌ Định dạng không hỗ trợ. Chấp nhận: ${allowedTypes.join(', ')}`, 'error');
            return;
        }

        this.setUploadingState(true);
        this.showStatus('📤 Đang upload video...', 'info');

        try {
            const formData = new FormData();
            formData.append('videoId', videoId);
            formData.append('videoFile', videoFile);

            // Use async endpoint for faster response
            const response = await fetch(`${this.apiBaseUrl}/videos/upload-async`, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (response.ok && result.success) {
                const uploadData = {
                    videoId: result.data.videoId,
                    cdnUrl: result.data.cdnUrl,
                    fileName: videoFile.name,
                    uploadTime: new Date().toISOString(),
                    fileSize: this.formatFileSize(videoFile.size),
                    status: result.data.status || 'processing'
                };

                this.addToRecentUploads(uploadData);
                
                if (result.processing === 'queued') {
                    this.showStatus(`✅ Upload hoàn tất! Video đang xử lý. ID: ${result.data.videoId}`, 'success');
                    
                    // Start polling for completion
                    this.pollVideoStatus(result.data.videoId, result.data.cdnUrl);
                } else {
                    this.showStatus(`✅ Upload thành công! Video ID: ${result.data.videoId}`, 'success');
                    
                    // Auto load video
                    document.getElementById('videoUrl').value = result.data.cdnUrl;
                    this.loadVideoFromUrl();
                }

                // Reset form
                document.getElementById('uploadForm').reset();

            } else {
                this.showStatus(`❌ Upload thất bại: ${result.error}`, 'error');
            }

        } catch (error) {
            console.error('Upload error:', error);
            this.showStatus('❌ Lỗi kết nối. Kiểm tra server có đang chạy không.', 'error');
        } finally {
            this.setUploadingState(false);
        }
    }

    async pollVideoStatus(videoId, cdnUrl, maxAttempts = 30) {
        let attempts = 0;
        const pollInterval = 2000; // 2 seconds

        const poll = async () => {
            try {
                attempts++;
                const response = await fetch(`${this.apiBaseUrl}/videos/status/${videoId}`);
                const result = await response.json();

                if (result.status === 'completed') {
                    this.showStatus(`🎉 Video đã xử lý xong! ID: ${videoId}`, 'success');
                    
                    // Update recent uploads
                    this.updateRecentUploadStatus(videoId, 'completed');
                    
                    // Auto load if it's the current video
                    const currentUrl = document.getElementById('videoUrl').value;
                    if (currentUrl === cdnUrl) {
                        this.loadVideoFromUrl();
                    }
                    return;
                }

                if (attempts >= maxAttempts) {
                    this.showStatus(`⏰ Thời gian chờ quá lâu cho video: ${videoId}`, 'warning');
                    return;
                }

                // Continue polling
                setTimeout(poll, pollInterval);
                
                // Update status message
                this.showStatus(`⏳ Đang xử lý video... (${attempts}/${maxAttempts})`, 'info');

            } catch (error) {
                console.error('Error polling video status:', error);
                if (attempts < maxAttempts) {
                    setTimeout(poll, pollInterval);
                }
            }
        };

        setTimeout(poll, pollInterval);
    }

    updateRecentUploadStatus(videoId, status) {
        const upload = this.recentUploads.find(u => u.videoId === videoId);
        if (upload) {
            upload.status = status;
            localStorage.setItem('recentUploads', JSON.stringify(this.recentUploads));
            this.loadRecentUploads();
        }
    }

    loadVideoFromUrl() {
        const videoUrl = document.getElementById('videoUrl').value.trim() || 'http://localhost:5288/videos/123/master.m3u8';

        if (!videoUrl) {
            this.showStatus('❌ Vui lòng nhập URL video', 'error');
            return;
        }

        if (!videoUrl.endsWith('.m3u8')) {
            this.showStatus('❌ URL phải là file .m3u8 (HLS)', 'error');
            return;
        }

        try {
            if (Hls.isSupported()) {
                const hls = new Hls();
                hls.loadSource(videoUrl);
                hls.attachMedia(this.player);
                hls.on(Hls.Events.MANIFEST_PARSED, () => {
                    this.player.play().catch(e => {
                        console.error('HLS Play Error:', e);
                        this.showStatus('❌ Không thể phát video: ' + e.message, 'error');
                    });
                });
                hls.on(Hls.Events.ERROR, (event, data) => {
                    console.error('HLS Error:', data);
                    this.showStatus('❌ HLS Error: ' + data.details, 'error');
                });
                this.showStatus(`✅ Đã load video: ${videoUrl}`, 'success');
            } else if (this.player.canPlayType('application/vnd.apple.mpegurl')) {
                this.player.src = videoUrl;
                this.player.play().catch(e => {
                    console.error('Native HLS Error:', e);
                    this.showStatus('❌ Native HLS Error: ' + e.message, 'error');
                });
                this.showStatus(`✅ Đã load video (Native HLS): ${videoUrl}`, 'success');
            } else {
                this.showStatus('❌ Browser không hỗ trợ HLS', 'error');
            }
        } catch (error) {
            console.error('Load video error:', error);
            this.showStatus('❌ Không thể load video: ' + error.message, 'error');
        }
    }

    addToRecentUploads(uploadData) {
        this.recentUploads.unshift(uploadData);

        // Keep max 10 recent uploads
        if (this.recentUploads.length > 10) {
            this.recentUploads = this.recentUploads.slice(0, 10);
        }

        localStorage.setItem('recentUploads', JSON.stringify(this.recentUploads));
        this.loadRecentUploads();
    }

    loadRecentUploads() {
        const recentList = document.getElementById('recentList');

        if (this.recentUploads.length === 0) {
            recentList.innerHTML = '<p class="no-uploads">Chưa có video nào được upload</p>';
            return;
        }

        const uploadsHtml = this.recentUploads.map(upload => {
            const statusIcon = upload.status === 'completed' ? '✅' : 
                              upload.status === 'processing' ? '⏳' : '📹';
            const statusText = upload.status === 'completed' ? 'Hoàn thành' : 
                              upload.status === 'processing' ? 'Đang xử lý' : 'Sẵn sàng';
            
            return `
            <div class="upload-item fade-in">
                <h4>${statusIcon} ${upload.videoId}</h4>
                <p><strong>File:</strong> ${upload.fileName}</p>
                <p><strong>Kích thước:</strong> ${upload.fileSize}</p>
                <p><strong>Trạng thái:</strong> ${statusText}</p>
                <p><strong>Thời gian:</strong> ${this.formatDate(upload.uploadTime)}</p>
                <div class="video-url">${upload.cdnUrl}</div>
                <button class="load-btn" onclick="loadVideoFromRecentUpload('${upload.cdnUrl}')" 
                        ${upload.status === 'processing' ? 'disabled' : ''}>
                    🎬 ${upload.status === 'processing' ? 'Đang xử lý...' : 'Xem Video'}
                </button>
                ${upload.status === 'processing' ? 
                    `<button class="status-btn" onclick="checkVideoStatus('${upload.videoId}')">🔄 Kiểm tra</button>` : ''}
            </div>
        `;
        }).join('');

        recentList.innerHTML = uploadsHtml;
    }

    setUploadingState(isUploading) {
        const uploadBtn = document.getElementById('uploadBtn');
        const spinner = document.getElementById('spinner');
        const btnText = uploadBtn.querySelector('.btn-text');

        if (isUploading) {
            uploadBtn.disabled = true;
            spinner.classList.remove('hidden');
            btnText.textContent = 'Đang upload...';
        } else {
            uploadBtn.disabled = false;
            spinner.classList.add('hidden');
            btnText.textContent = 'Upload Video';
        }
    }

    showStatus(message, type) {
        const statusDiv = document.getElementById('uploadStatus');
        statusDiv.className = `status-message ${type}`;
        statusDiv.textContent = message;
        statusDiv.style.display = 'block';

        // Auto hide after 5 seconds for success/info messages
        if (type === 'success' || type === 'info') {
            setTimeout(() => {
                statusDiv.style.display = 'none';
            }, 5000);
        }
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }
}

// Global functions
function loadVideoFromRecentUpload(url) {
    document.getElementById('videoUrl').value = url;
    app.loadVideoFromUrl();
}

async function checkVideoStatus(videoId) {
    try {
        const response = await fetch(`${app.apiBaseUrl}/videos/status/${videoId}`);
        const result = await response.json();
        
        if (result.status === 'completed') {
            app.updateRecentUploadStatus(videoId, 'completed');
            app.showStatus(`✅ Video ${videoId} đã hoàn thành!`, 'success');
        } else {
            app.showStatus(`⏳ Video ${videoId} vẫn đang xử lý...`, 'info');
        }
    } catch (error) {
        console.error('Error checking status:', error);
        app.showStatus('❌ Lỗi khi kiểm tra trạng thái', 'error');
    }
}

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new VideoStreamingApp();
});

// Test data function (for demo purposes)
function addTestData() {
    const testUploads = [
        {
            videoId: 'demo-video-1',
            cdnUrl: 'https://bitdash-a.akamaihd.net/content/sintel/hls/playlist.m3u8',
            fileName: 'demo1.mp4',
            uploadTime: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
            fileSize: '25.6 MB'
        },
        {
            videoId: 'demo-video-2',
            cdnUrl: 'https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8',
            fileName: 'demo2.mov',
            uploadTime: new Date(Date.now() - 7200000).toISOString(), // 2 hours ago
            fileSize: '42.3 MB'
        }
    ];

    localStorage.setItem('recentUploads', JSON.stringify(testUploads));
    if (window.app) {
        window.app.recentUploads = testUploads;
        window.app.loadRecentUploads();
    }
}

// Expose test function globally for console testing
window.addTestData = addTestData;
