document.addEventListener("DOMContentLoaded", () => {
    const themeToggleBtn = document.getElementById("themeToggle");
    const bodyElement = document.body;

    // Check saved theme or default to dark
    const currentTheme = localStorage.getItem("sb-theme") || "dark";

    if (currentTheme === "light") {
        bodyElement.classList.add("light-mode");
        if (themeToggleBtn) {
            themeToggleBtn.innerHTML = '<i class="bi bi-sun-fill"></i>';
        }
    } else {
        bodyElement.classList.remove("light-mode");
        if (themeToggleBtn) {
            themeToggleBtn.innerHTML = '<i class="bi bi-moon-fill"></i>';
        }
    }

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", () => {
            bodyElement.classList.toggle("light-mode");

            if (bodyElement.classList.contains("light-mode")) {
                localStorage.setItem("sb-theme", "light");
                themeToggleBtn.innerHTML = '<i class="bi bi-sun-fill"></i>';
            } else {
                localStorage.setItem("sb-theme", "dark");
                themeToggleBtn.innerHTML = '<i class="bi bi-moon-fill"></i>';
            }
        });
    }

    // Apply fade-in animation to main container
    const mainContent = document.querySelector("main");
    if (mainContent) {
        mainContent.classList.add("fade-in");
    }
});
