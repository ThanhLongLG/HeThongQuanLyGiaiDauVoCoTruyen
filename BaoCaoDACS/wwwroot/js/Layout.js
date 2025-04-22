
    // Toggle navigation menu
    document.getElementById('menu-toggle').addEventListener('click', function () {
      const navMenu = document.getElementById('nav-menu');
      navMenu.style.display = navMenu.style.display === 'none' || !navMenu.style.display ? 'flex' : 'none';
    });

    // Smooth scrolling for navigation links
    document.querySelectorAll('#nav-menu a').forEach(link => {
      link.addEventListener('click', function (event) {
        event.preventDefault();
        const targetId = this.getAttribute('href').substring(1);
        const targetElement = document.getElementById(targetId);
        if (targetElement) {
          targetElement.scrollIntoView({ behavior: 'smooth' });
        }
      });
    });

    // Scroll to tournaments section
    document.getElementById('explore-btn').addEventListener('click', function () {
      document.querySelector('.tournaments').scrollIntoView({ behavior: 'smooth' });
    });

    // Login button action
    document.querySelector('#login-btn').addEventListener('click', function () {
      showModal('login-modal');
    });

    // Register button action
    document.querySelector('#register-btn').addEventListener('click', function () {
      showModal('register-modal');
    });

    // Function to show modal
    function showModal(modalId) {
      const modal = document.getElementById(modalId);
      modal.classList.add('show');
    }

    // Function to close modal
    function closeModal(modalId) {
      const modal = document.getElementById(modalId);
      modal.classList.remove('show');
    }

    // Function to toggle password visibility
    function togglePassword(inputId) {
      const input = document.getElementById(inputId);
      if (input.type === "password") {
        input.type = "text";
      } else {
        input.type = "password";
      }
    }

    // Close modal when clicking outside
    window.addEventListener('click', function(event) {
      const modals = document.querySelectorAll('.modal');
      modals.forEach(modal => {
        if (event.target === modal) {
          modal.classList.remove('show');
        }
      });
    });

    // Form submission for login
    document.getElementById('login-form').addEventListener('submit', function(e) {
      e.preventDefault();
      const email = document.getElementById('login-email').value.trim();
      const password = document.getElementById('login-password').value.trim();

      if (!email || !password) {
        alert('Vui lòng nhập đầy đủ thông tin đăng nhập.');
        return;
      }

      if (!validateEmail(email)) {
        alert('Email không hợp lệ. Vui lòng kiểm tra lại!');
        return;
      }

      alert(`Đăng nhập thành công! Chào mừng, ${email}`);
      closeModal('login-modal');
    });

    // Form submission for registration
    document.getElementById('register-form').addEventListener('submit', function(e) {
      e.preventDefault();
      const name = document.getElementById('register-name').value.trim();
      const email = document.getElementById('register-email').value.trim();
      const password = document.getElementById('register-password').value;
      const confirmPassword = document.getElementById('register-confirm-password').value;

      if (!name || !email || !password || !confirmPassword) {
        alert('Vui lòng nhập đầy đủ thông tin đăng ký.');
        return;
      }

      if (!validateEmail(email)) {
        alert('Email không hợp lệ. Vui lòng kiểm tra lại!');
        return;
      }

      if (password.length < 6) {
        alert('Mật khẩu phải có ít nhất 6 ký tự.');
        return;
      }

      if (password !== confirmPassword) {
        alert('Mật khẩu không khớp. Vui lòng kiểm tra lại!');
        return;
      }

      alert(`Đăng ký thành công! Chào mừng, ${name}`);
      closeModal('register-modal');
    });

    // Helper function to validate email format
    function validateEmail(email) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return emailRegex.test(email);
    }

    const tournaments = [
      { date: '2025-04-20', name: 'Giải Vô Địch Các CLB Võ Cổ Truyền Quốc Gia', location: 'Thái Nguyên' },
      { date: '2025-04-22', name: 'Giao Lưu Võ Thuật Việt Nam - Triều Tiên', location: 'Thái Nguyên' },
      { date: '2025-06-15', name: 'Giải Vô Địch Trẻ Võ Cổ Truyền Quốc Gia', location: 'Thanh Hóa' }
    ];

    function renderCalendar(month, year) {
      const firstDay = new Date(year, month, 1).getDay();
      const daysInMonth = new Date(year, month + 1, 0).getDate();
      calendarGrid.innerHTML = '';

      // Add day names
      dayNames.forEach(day => {
        const dayName = document.createElement('div');
        dayName.classList.add('day-name');
        dayName.textContent = day;
        calendarGrid.appendChild(dayName);
      });

      // Add empty slots for days before the first day
      for (let i = 0; i < firstDay; i++) {
        const emptySlot = document.createElement('div');
        emptySlot.classList.add('calendar-day', 'empty');
        calendarGrid.appendChild(emptySlot);
      }

      // Add days of the month
      for (let day = 1; day <= daysInMonth; day++) {
        const dayElement = document.createElement('div');
        dayElement.classList.add('calendar-day');
        const dateString = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
        dayElement.innerHTML = `<span class="day-number">${day}</span>`;

        // Highlight current day
        const today = new Date();
        if (
          today.getDate() === day &&
          today.getMonth() === month &&
          today.getFullYear() === year
        ) {
          dayElement.classList.add('current-day');
        }

        // Check if the day has a tournament
        const tournament = tournaments.find(t => t.date === dateString);
        if (tournament) {
          dayElement.classList.add('has-event');
          const tooltip = document.createElement('div');
          tooltip.classList.add('event-tooltip');
          tooltip.innerHTML = `<strong>${tournament.name}</strong><br>${tournament.location}`;
          dayElement.appendChild(tooltip);

          // Show tooltip on hover
          dayElement.addEventListener('mouseenter', () => {
            tooltip.style.display = 'block';
          });
          dayElement.addEventListener('mouseleave', () => {
            tooltip.style.display = 'none';
          });
        }

        calendarGrid.appendChild(dayElement);
      }
    }

    function updateMonthDisplay() {
      currentMonthDisplay.textContent = `${monthNames[currentMonth]}, ${currentYear}`;
    }

    document.getElementById('prev-month').addEventListener('click', () => {
      currentMonth--;
      if (currentMonth < 0) {
        currentMonth = 11;
        currentYear--;
      }
      updateMonthDisplay();
      renderCalendar(currentMonth, currentYear);
    });

    document.getElementById('next-month').addEventListener('click', () => {
      currentMonth++;
      if (currentMonth > 11) {
        currentMonth = 0;
        currentYear++;
      }
      updateMonthDisplay();
      renderCalendar(currentMonth, currentYear);
    });

    // Update calendar in real-time
    function updateCalendarRealTime() {
      const now = new Date();
      currentMonth = now.getMonth();
      currentYear = now.getFullYear();
      updateMonthDisplay();
      renderCalendar(currentMonth, currentYear);
    }

    // Initial render
    updateMonthDisplay();
    renderCalendar(currentMonth, currentYear);
    setInterval(updateCalendarRealTime, 60000); // Update every minute

    // Toggle modals
    const loginModal = document.getElementById('login-modal');
    const registerModal = document.getElementById('register-modal');

    document.querySelectorAll('.modal-close').forEach(button => {
      button.addEventListener('click', () => {
        loginModal.classList.remove('show');
        registerModal.classList.remove('show');
      });
    });

    document.querySelector('#switch-to-register').addEventListener('click', (e) => {
      e.preventDefault();
      loginModal.classList.remove('show');
      registerModal.classList.add('show');
    });

    document.querySelector('#switch-to-login').addEventListener('click', (e) => {
      e.preventDefault();
      registerModal.classList.remove('show');
      loginModal.classList.add('show');
    });

    // Toggle password visibility
    document.querySelectorAll('.toggle-password').forEach(button => {
      button.addEventListener('click', () => {
        const input = button.previousElementSibling;
        if (input.type === 'password') {
          input.type = 'text';
          button.textContent = '🙈';
        } else {
          input.type = 'password';
          button.textContent = '👁️';
        }
      });
    });

    // Validate password match in register form
    document.querySelector('#register-modal form').addEventListener('submit', (e) => {
      const password = document.getElementById('register-password').value;
      const confirmPassword = document.getElementById('confirm-password').value;

      if (password !== confirmPassword) {
        e.preventDefault();
        alert('Mật khẩu không khớp. Vui lòng kiểm tra lại.');
      }
    });

    function countdownTimer() {
      const events = [
        {
          name: "Giải vô địch các câu lạc bộ võ cổ truyền quốc gia lần thứ XIV năm 2025",
          date: new Date("2025-04-20T00:00:00+07:00"),
          elemIds: { days: "days-1", hours: "hours-1", minutes: "minutes-1", seconds: "seconds-1" },
        },
        {
          name: "Chương trình giao lưu biểu diễn võ thuật Việt Nam - Triều Tiên",
          date: new Date("2025-04-22T19:00:00+07:00"),
          elemIds: { days: "days-2", hours: "hours-2", minutes: "minutes-2", seconds: "seconds-2" },
        },
        {
          name: "Giải vô địch trẻ võ cổ truyền quốc gia lần thứ XXVI, năm 2025",
          date: new Date("2025-06-15T00:00:00+07:00"),
          elemIds: { days: "days-3", hours: "hours-3", minutes: "minutes-3", seconds: "seconds-3" },
        },
      ];

      function updateCountdowns() {
        const now = new Date().getTime();

        events.forEach((event) => {
          const timeLeft = event.date.getTime() - now;

          if (timeLeft > 0) {
            const days = Math.floor(timeLeft / (1000 * 60 * 60 * 24));
            const hours = Math.floor((timeLeft % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);

            document.getElementById(event.elemIds.days).textContent = formatTime(days);
            document.getElementById(event.elemIds.hours).textContent = formatTime(hours);
            document.getElementById(event.elemIds.minutes).textContent = formatTime(minutes);
            document.getElementById(event.elemIds.seconds).textContent = formatTime(seconds);
          } else {
            document.getElementById(event.elemIds.days).textContent = "00";
            document.getElementById(event.elemIds.hours).textContent = "00";
            document.getElementById(event.elemIds.minutes).textContent = "00";
            document.getElementById(event.elemIds.seconds).textContent = "00";

            const statusElement = document.getElementById(event.elemIds.days).closest(".tournament-card").querySelector(".tournament-status");
            if (statusElement) {
              statusElement.textContent = "Đã kết thúc";
              statusElement.style.backgroundColor = "#9E9E9E";
            }
          }
        });
      }

      function formatTime(time) {
        return time < 10 ? `0${time}` : time;
      }

      updateCountdowns();
      setInterval(updateCountdowns, 1000);
    }

    document.addEventListener("DOMContentLoaded", countdownTimer);

    // Real-time clock
    function updateClock() {
      const now = new Date();
      const hours = String(now.getHours()).padStart(2, '0');
      const minutes = String(now.getMinutes()).padStart(2, '0');
      const seconds = String(now.getSeconds()).padStart(2, '0');
      const timeString = `${hours}:${minutes}:${seconds}`;
      document.getElementById('real-time-clock').textContent = `Thời gian hiện tại: ${timeString}`;
    }

    setInterval(updateClock, 1000);
    updateClock();
  </script>
  <script>
    // Hàm chính để đếm ngược thời gian
    function countdownTimer() {
      // Lấy ngày hiện tại
      const now = new Date();
      
      // Thiết lập thời gian cho các sự kiện
      const events = [
        {
          name: "Giải vô địch các câu lạc bộ võ cổ truyền quốc gia lần thứ XIV năm 2025",
          date: new Date("2025-04-20T00:00:00+07:00"), // Điều chỉnh theo múi giờ Việt Nam
          elemIds: {
            days: "days-1",
            hours: "hours-1",
            minutes: "minutes-1",
            seconds: "seconds-1"
          }
        },
        {
          name: "Chương trình giao lưu biểu diễn võ thuật Việt Nam - Triều Tiên",
          date: new Date("2025-04-22T19:00:00+07:00"), // 19:00 giờ Việt Nam
          elemIds: {
            days: "days-2",
            hours: "hours-2",
            minutes: "minutes-2",
            seconds: "seconds-2"
          }
        },
        {
          name: "Giải vô đich trẻ võ cổ truyền quốc gia lần thứ XXVI, năm 2025",
          date: new Date("2025-06-15T00:00:00+07:00"), // Điều chỉnh theo múi giờ Việt Nam
          elemIds: {
            days: "days-3",
            hours: "hours-3",
            minutes: "minutes-3",
            seconds: "seconds-3"
          }
        }
      ];

      // Lưu trữ giá trị đếm ngược trước đó để phát hiện thay đổi
      let previousValues = {};
      events.forEach(event => {
        previousValues[event.name] = {
          days: -1,
          hours: -1,
          minutes: -1,
          seconds: -1
        };
      });

      // Cập nhật đếm ngược cho mỗi sự kiện
      function updateAllCountdowns() {
        const now = new Date().getTime();
        
        events.forEach(event => {
          const eventTime = event.date.getTime();
          const timeLeft = eventTime - now;
          
          if (timeLeft > 0) {
            // Tính toán ngày, giờ, phút, giây còn lại
            const days = Math.floor(timeLeft / (1000 * 60 * 60 * 24));
            const hours = Math.floor((timeLeft % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);
            
            // Lấy các phần tử DOM
            const daysElement = document.getElementById(event.elemIds.days);
            const hoursElement = document.getElementById(event.elemIds.hours);
            const minutesElement = document.getElementById(event.elemIds.minutes);
            const secondsElement = document.getElementById(event.elemIds.seconds);
            
            // Kiểm tra và thêm animation nếu giá trị thay đổi
            if (days !== previousValues[event.name].days) {
              addPulseAnimation(daysElement);
              previousValues[event.name].days = days;
            }
            
            if (hours !== previousValues[event.name].hours) {
              addPulseAnimation(hoursElement);
              previousValues[event.name].hours = hours;
            }
            
            if (minutes !== previousValues[event.name].minutes) {
              addPulseAnimation(minutesElement);
              previousValues[event.name].minutes = minutes;
            }
            
            if (seconds !== previousValues[event.name].seconds) {
              addPulseAnimation(secondsElement);
              previousValues[event.name].seconds = seconds;
            }
            
            // Cập nhật HTML
            daysElement.textContent = formatTime(days);
            hoursElement.textContent = formatTime(hours);
            minutesElement.textContent = formatTime(minutes);
            secondsElement.textContent = formatTime(seconds);
            
            // Cập nhật trạng thái sự kiện nếu còn ít hơn 24 giờ
            if (days === 0 && hours < 24) {
              const statusElement = daysElement.closest('.tournament-card').querySelector('.tournament-status');
              if (statusElement) {
                statusElement.textContent = "Sắp diễn ra";
                statusElement.style.backgroundColor = '#FF5722'; // Cam đậm hơn để thu hút sự chú ý
              }
            }
          } else {
            // Nếu đã hết thời gian, hiển thị 00
            document.getElementById(event.elemIds.days).textContent = "00";
            document.getElementById(event.elemIds.hours).textContent = "00";
            document.getElementById(event.elemIds.minutes).textContent = "00";
            document.getElementById(event.elemIds.seconds).textContent = "00";
            
            // Cập nhật trạng thái sự kiện nếu đã diễn ra
            const statusElement = document.getElementById(event.elemIds.days).closest('.tournament-card').querySelector('.tournament-status');
            if (statusElement) {
              const endDate = event.endDate ? new Date(event.endDate) : new Date(event.date.getTime() + (24 * 60 * 60 * 1000)); // Mặc định 1 ngày
              
              if (now > endDate) {
                statusElement.textContent = "Đã kết thúc";
                statusElement.className = "tournament-status";
                statusElement.style.backgroundColor = '#9E9E9E'; // Màu xám
              } else {
                statusElement.textContent = "Đang diễn ra";
                statusElement.className = "tournament-status";
                statusElement.style.backgroundColor = '#4CAF50'; // Màu xanh lá
              }
            }
          }
        });
      }
      
      // Thêm hiệu ứng pulse khi giá trị thay đổi
      function addPulseAnimation(element) {
        element.classList.add('countdown-pulse');
        setTimeout(() => {
          element.classList.remove('countdown-pulse');
        }, 500);
      }
      
      // Định dạng số thành chuỗi 2 chữ số (thêm số 0 đằng trước nếu cần)
      function formatTime(time) {
        return time < 10 ? `0${time}` : time;
      }

      // Cập nhật đồng hồ mỗi giây
      updateAllCountdowns();
      setInterval(updateAllCountdowns, 1000);
    }

    // Khởi chạy đồng hồ đếm ngược khi trang web đã tải xong
    document.addEventListener('DOMContentLoaded', countdownTimer);
