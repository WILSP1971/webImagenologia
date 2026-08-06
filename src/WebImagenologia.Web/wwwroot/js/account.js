'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('loginForm');

    if (!form) {
        return;
    }

    form.addEventListener('submit', function (event) {
        const submitButton = form.querySelector('button[type="submit"]');

        if (typeof window.jQuery !== 'undefined' && window.jQuery.fn.validate) {
            const $form = window.jQuery(form);
            if ($form.data('validator') && !$form.valid()) {
                return;
            }
        }

        if (submitButton) {
            submitButton.disabled = true;
            submitButton.setAttribute('aria-busy', 'true');
            submitButton.textContent = 'Validando acceso…';
        }
    });
});
