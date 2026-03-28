document.addEventListener("DOMContentLoaded", () => {
    const themeToggleBtn = document.getElementById("themeToggle");
    const htmlElement = document.documentElement;
    const bodyElement = document.body;
    
    // Check saved theme or default to light
    const currentTheme = localStorage.getItem("theme") || "light";
    
    if (currentTheme === "dark") {
        bodyElement.classList.add("dark-mode");
        if(themeToggleBtn) {
            themeToggleBtn.innerHTML = '<i class="bi bi-sun-fill"></i>';
        }
    } else {
        if(themeToggleBtn) {
            themeToggleBtn.innerHTML = '<i class="bi bi-moon-fill"></i>';
        }
    }
    
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", () => {
            bodyElement.classList.toggle("dark-mode");
            
            if (bodyElement.classList.contains("dark-mode")) {
                localStorage.setItem("theme", "dark");
                themeToggleBtn.innerHTML = '<i class="bi bi-sun-fill"></i>';
            } else {
                localStorage.setItem("theme", "light");
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
