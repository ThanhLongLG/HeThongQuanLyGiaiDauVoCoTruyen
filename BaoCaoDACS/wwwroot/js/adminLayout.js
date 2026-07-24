(() => {
    "use strict";

    const sidebar = document.getElementById("adminSidebar");
    const toggler = document.getElementById("sideBarToggler");
    const overlay = document.querySelector(".vct-admin .overlay");
    const mobileQuery = window.matchMedia("(max-width: 768px)");

    if (!sidebar || !toggler || !overlay) {
        return;
    }

    const updateControl = (isOpen) => {
        toggler.setAttribute("aria-expanded", isOpen ? "true" : "false");
        toggler.setAttribute("aria-label", isOpen ? "Đóng menu quản trị" : "Mở menu quản trị");
    };

    const closeMobileMenu = () => {
        sidebar.classList.remove("show");
        overlay.classList.remove("show");
        document.body.classList.remove("admin-menu-open");
        updateControl(false);
    };

    const openMobileMenu = () => {
        sidebar.classList.add("show");
        overlay.classList.add("show");
        document.body.classList.add("admin-menu-open");
        updateControl(true);
    };

    toggler.addEventListener("click", () => {
        if (mobileQuery.matches) {
            if (sidebar.classList.contains("show")) {
                closeMobileMenu();
            } else {
                openMobileMenu();
            }
            return;
        }

        const isCollapsed = sidebar.classList.toggle("collapsed");
        updateControl(!isCollapsed);
    });

    overlay.addEventListener("click", closeMobileMenu);

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && sidebar.classList.contains("show")) {
            closeMobileMenu();
            toggler.focus();
        }
    });

    const syncResponsiveState = () => {
        if (mobileQuery.matches) {
            sidebar.classList.remove("collapsed");
            closeMobileMenu();
        } else {
            sidebar.classList.remove("show");
            overlay.classList.remove("show");
            document.body.classList.remove("admin-menu-open");
            updateControl(!sidebar.classList.contains("collapsed"));
        }
    };

    mobileQuery.addEventListener("change", syncResponsiveState);
    syncResponsiveState();
})();
