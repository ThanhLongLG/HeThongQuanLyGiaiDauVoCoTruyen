

// Video controls
function bindPerfVideoControls() {
    const perfVideo = document.getElementById('performanceVideo');
    if (!perfVideo) return;
    document.getElementById('playPauseBtn').onclick = () => {
        if (perfVideo.paused) perfVideo.play();
        else perfVideo.pause();
    };
    document.getElementById('slowMotionBtn').onclick = () => { perfVideo.playbackRate = 0.5; };
    document.getElementById('normalSpeedBtn').onclick = () => { perfVideo.playbackRate = 1.0; };
}
bindPerfVideoControls();
document.getElementById('perfUploadVideoBtn').onclick = function () {
    document.getElementById('perfVideoFileInput').click();
};
document.getElementById('perfVideoFileInput').onchange = function (e) {
    const file = e.target.files[0];
    if (!file) return;
    const url = URL.createObjectURL(file);
    const container = document.querySelector('#errorAnalysisView .video-container');
    if (container) {
        container.innerHTML = `<video id="performanceVideo" width="100%" height="340" controls style="background:#000;"><source src="${url}"></video>`;
    }
    window.perfVideo = document.getElementById('performanceVideo');
    bindPerfVideoControls();
};

document.getElementById('perfVideoFileInput').onchange = function (e) {
    const file = e.target.files[0];
    console.log('File selected:', file);
    if (!file) return;
    const url = URL.createObjectURL(file);
    const container = document.querySelector('#errorAnalysisView .video-container');
    if (container) {
        container.innerHTML = `<video id="performanceVideo" width="100%" height="340" controls style="background:#000;"><source src="${url}"></video>`;
    }
    window.perfVideo = document.getElementById('performanceVideo');
    bindPerfVideoControls();
};