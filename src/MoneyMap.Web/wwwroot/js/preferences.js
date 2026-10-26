// Theme + text-size settings, persisted in localStorage. Called from Blazor via JSInterop.
window.moneyMapPreferences = {
    setTheme: function (theme) {
        try {
            if (theme === 'system') {
                localStorage.removeItem('moneymap-theme');
                document.documentElement.removeAttribute('data-theme');
            } else {
                localStorage.setItem('moneymap-theme', theme);
                document.documentElement.setAttribute('data-theme', theme);
            }
        } catch (e) { /* ignore */ }
    },
    getTheme: function () {
        try {
            return localStorage.getItem('moneymap-theme') || 'system';
        } catch (e) {
            return 'system';
        }
    },
    setTextScale: function (scale) {
        try {
            localStorage.setItem('moneymap-text-scale', scale);
            document.documentElement.style.setProperty('--mm-text-scale', scale);
        } catch (e) { /* ignore */ }
    },
    getTextScale: function () {
        try {
            return localStorage.getItem('moneymap-text-scale') || '1';
        } catch (e) {
            return '1';
        }
    }
};
