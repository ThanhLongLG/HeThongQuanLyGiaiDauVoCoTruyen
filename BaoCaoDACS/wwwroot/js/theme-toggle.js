(() => {
    "use strict";

    const storageKey = "vct-theme";
    const root = document.documentElement;
    const systemTheme = window.matchMedia("(prefers-color-scheme: light)");

    const getSavedTheme = () => {
        try {
            const savedTheme = localStorage.getItem(storageKey);
            return savedTheme === "light" || savedTheme === "dark" ? savedTheme : null;
        } catch {
            return null;
        }
    };

    const updateControls = (theme) => {
        const nextThemeLabel = theme === "dark"
            ? "Chuyển sang giao diện sáng"
            : "Chuyển sang giao diện tối";

        document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
            button.setAttribute("aria-label", nextThemeLabel);
            button.setAttribute("title", nextThemeLabel);
            button.setAttribute("aria-pressed", theme === "light" ? "true" : "false");
        });

        const themeColor = document.getElementById("vctThemeColor");
        if (themeColor) {
            themeColor.setAttribute("content", theme === "light" ? "#f5f0e7" : "#1c1a16");
        }
    };

    const applyTheme = (theme, persist) => {
        root.dataset.theme = theme;
        updateControls(theme);

        if (persist) {
            try {
                localStorage.setItem(storageKey, theme);
            } catch {
                // Theme vẫn được áp dụng trong phiên hiện tại nếu storage bị chặn.
            }
        }
    };

    const initialTheme = root.dataset.theme === "light" ? "light" : "dark";
    applyTheme(initialTheme, false);

    document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            applyTheme(root.dataset.theme === "dark" ? "light" : "dark", true);
        });
    });

    systemTheme.addEventListener("change", (event) => {
        if (!getSavedTheme()) {
            applyTheme(event.matches ? "light" : "dark", false);
        }
    });

    requestAnimationFrame(() => root.classList.add("vct-theme-ready"));
})();
