(function () {
    'use strict';

    document.querySelectorAll('.btn-toggle-estado').forEach(function (toggle) {
        toggle.addEventListener('change', function () {
            var form = toggle.closest('form');
            if (form) {
                form.submit();
            }
        });
    });
})();
