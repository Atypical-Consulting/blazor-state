export function setThemeClass(theme) {
    if (theme === "dark") {
        document.documentElement.classList.add("dark");
    } else {
        document.documentElement.classList.remove("dark");
    }
}

export function setDensityClass(density) {
    if (density === "compact") {
        document.documentElement.classList.add("density-compact");
    } else {
        document.documentElement.classList.remove("density-compact");
    }
}

export function clearAllBlazorState() {
    var keysToRemove = [];
    for (var i = 0; i < localStorage.length; i++) {
        keysToRemove.push(localStorage.key(i));
    }
    keysToRemove.forEach(function (k) { localStorage.removeItem(k); });
    sessionStorage.clear();
    indexedDB.deleteDatabase("TheBlazorState");
    return true;
}

export function getStoredPreference(key) {
    try {
        var item = localStorage.getItem(key);
        return item ? JSON.parse(item).value : null;
    } catch {
        return null;
    }
}
