(() => {
    const themeKey = "theme";
    const themeMenuIcon = document.getElementById("themeMenuIcon");
    const themeMenuText = document.getElementById("themeMenuText");
    const themeButtons = document.querySelectorAll("[data-theme-value]");
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    const themeIcons = {
        light: "bi-sun-fill",
        dark: "bi-moon-stars-fill",
        auto: "bi-circle-half"
    };

    const getStoredTheme = () => localStorage.getItem(themeKey);

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme();

        if (storedTheme === "light" || storedTheme === "dark") {
            return storedTheme;
        }

        return mediaQuery.matches ? "dark" : "light";
    };

    const getSelectedTheme = () => {
        const storedTheme = getStoredTheme();

        return storedTheme === "light" || storedTheme === "dark" ? storedTheme : "auto";
    };

    const setTheme = (theme) => {
        if (theme === "auto") {
            localStorage.removeItem(themeKey);
        } else {
            localStorage.setItem(themeKey, theme);
        }

        document.documentElement.setAttribute("data-bs-theme", getPreferredTheme());
        updateThemeControl(theme);
    };

    const updateThemeControl = (selectedTheme) => {
        if (themeMenuText) {
            themeMenuText.textContent = selectedTheme.charAt(0).toUpperCase() + selectedTheme.slice(1);
        }

        if (themeMenuIcon) {
            themeMenuIcon.classList.remove(...Object.values(themeIcons));
            themeMenuIcon.classList.add(themeIcons[selectedTheme]);
        }

        themeButtons.forEach((button) => {
            button.classList.toggle("active", button.dataset.themeValue === selectedTheme);
        });
    };

    themeButtons.forEach((button) => {
        button.addEventListener("click", () => setTheme(button.dataset.themeValue));
    });

    mediaQuery.addEventListener("change", () => {
        if (!getStoredTheme()) {
            document.documentElement.setAttribute("data-bs-theme", getPreferredTheme());
        }
    });

    updateThemeControl(getSelectedTheme());
})();
