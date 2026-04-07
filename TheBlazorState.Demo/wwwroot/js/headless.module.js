// Focus trap: cycles Tab focus within an element
export function trapFocus(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const focusable = el.querySelectorAll(
        'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])'
    );
    if (focusable.length > 0) focusable[0].focus();

    const handler = (e) => {
        if (e.key !== 'Tab') return;
        const first = focusable[0], last = focusable[focusable.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    };
    el._headlessTrap = handler;
    el.addEventListener('keydown', handler);
}

export function releaseFocus(elementId) {
    const el = document.getElementById(elementId);
    if (el?._headlessTrap) {
        el.removeEventListener('keydown', el._headlessTrap);
        delete el._headlessTrap;
    }
}

// Click-outside detection: calls .NET when click lands outside the element
export function onClickOutside(elementId, dotNetRef) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const handler = (e) => {
        if (!el.contains(e.target)) {
            dotNetRef.invokeMethodAsync('OnClickOutside');
        }
    };
    setTimeout(() => document.addEventListener('mousedown', handler), 0);
    el._headlessClickOutside = handler;
}

export function removeClickOutside(elementId) {
    const el = document.getElementById(elementId);
    if (el?._headlessClickOutside) {
        document.removeEventListener('mousedown', el._headlessClickOutside);
        delete el._headlessClickOutside;
    }
}

// Focus a specific element by ID
export function focusElement(elementId) {
    document.getElementById(elementId)?.focus();
}
