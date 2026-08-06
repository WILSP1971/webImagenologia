/**
 * Submenús anidados en Parámetros (Bootstrap 5 dropend dentro de dropdown).
 */
(function () {
    'use strict';

    function closeSiblingDropends(current) {
        document.querySelectorAll('.navbar .dropend .dropdown-menu.show').forEach(function (menu) {
            if (!current.contains(menu)) {
                menu.classList.remove('show');
            }
        });
    }

    document.querySelectorAll('.navbar .dropend > .dropdown-toggle').forEach(function (toggle) {
        toggle.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();

            var parent = toggle.closest('.dropend');
            var menu = parent.querySelector(':scope > .dropdown-menu');
            if (!menu) {
                return;
            }

            closeSiblingDropends(parent);
            menu.classList.toggle('show');
            toggle.setAttribute('aria-expanded', menu.classList.contains('show'));
        });
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.navbar .dropend')) {
            closeSiblingDropends(document.body);
        }
    });
})();
