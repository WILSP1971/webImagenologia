'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form[action*="Account/Login"]');

    if (!form) {
        return;
    }

    form.addEventListener('submit', function () {
        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.setAttribute('aria-busy', 'true');
        }
    });
});
