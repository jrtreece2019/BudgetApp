// Theme persistence helper
window.themeHelper = {
    getTheme: function () {
        return localStorage.getItem('app-theme') || 'dark';
    },
    setTheme: function (theme) {
        localStorage.setItem('app-theme', theme);
    }
};

