


let timeRemaining = 120;
let timerInterval = null;

function updateTimerDisplay() {
    const timerElement = document.getElementById("combat-timer"); // đúng ID ở HTML
    const minutes = Math.floor(timeRemaining / 60);
    const seconds = timeRemaining % 60;
    timerElement.textContent = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function startTimer() {
    if (!timerInterval) {
        timerInterval = setInterval(() => {
            if (timeRemaining > 0) {
                timeRemaining--;
                updateTimerDisplay();
            } else {
                clearInterval(timerInterval);
                timerInterval = null;
                alert("Hết giờ!");
            }
        }, 1000);
    }
}

function pauseTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

function resetTimer() {
    timeRemaining = 120;
    updateTimerDisplay();
    pauseTimer();
}

// Gắn sự kiện sau khi DOM tải xong
window.onload = function () {
    document.getElementById("start-timer").addEventListener("click", startTimer);
    document.getElementById("pause-timer").addEventListener("click", pauseTimer);
    document.getElementById("reset-timer").addEventListener("click", resetTimer);

    updateTimerDisplay(); // cập nhật ban đầu
};
 

    // Tab switching for main menu
    const navTabs = document.querySelectorAll('.nav-tab');
    const mainSections = document.querySelectorAll('.tab-main-section');
        const mainContent = document.querySelector('main > .container');
    const scoringTabs = document.querySelector('.scoring-tabs');
    const scoringTabContents = document.querySelectorAll('.tab-content');
    const matchInfos = document.querySelectorAll('.tab-match-info');

    function showMainTab(tab) {
        navTabs.forEach(t => t.classList.remove('active'));
            mainSections.forEach(s => s.classList.remove('active'));
    if (tab === 'chamdiem') {
        mainContent.style.display = '';
    scoringTabs.style.display = '';
                scoringTabContents.forEach((c, i) => {
                    if (i === 0) c.classList.add('active');
    else c.classList.remove('active');
                });
                matchInfos.forEach((m, i) => {
                    if (i === 0) m.classList.add('active');
    else m.classList.remove('active');
                });
            } else {
        mainContent.style.display = 'none';
    scoringTabs.style.display = 'none';
    document.getElementById(tab + '-section').classList.add('active');
            }
        }

        navTabs.forEach(tab => {
        tab.addEventListener('click', (e) => {
            e.preventDefault();
            const tabName = tab.getAttribute('data-main-tab');
            showMainTab(tabName);
            tab.classList.add('active');
        });
        });

    // Mặc định hiển thị Chấm điểm
    showMainTab('chamdiem');

// funsion tinh diem


