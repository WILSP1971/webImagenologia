(function () {
    'use strict';

    var chkRadiologos = document.getElementById('chkRadiologos');
    var chkOperador = document.getElementById('chkOperador');
    var tipoHidden = document.getElementById('tipoProgramacionHidden');
    var formAutomatizacion = document.getElementById('formAutomatizacion');

    function syncTipoProgramacion(changed) {
        if (!tipoHidden) {
            return;
        }

        if (changed === chkRadiologos && chkRadiologos.checked && chkOperador) {
            chkOperador.checked = false;
        }

        if (changed === chkOperador && chkOperador.checked && chkRadiologos) {
            chkRadiologos.checked = false;
        }

        if (chkRadiologos && chkRadiologos.checked) {
            tipoHidden.value = 'RAD';
        } else if (chkOperador && chkOperador.checked) {
            tipoHidden.value = 'OPE';
        } else {
            tipoHidden.value = '';
        }
    }

    if (chkRadiologos) {
        chkRadiologos.addEventListener('change', function () {
            syncTipoProgramacion(chkRadiologos);
        });
    }

    if (chkOperador) {
        chkOperador.addEventListener('change', function () {
            syncTipoProgramacion(chkOperador);
        });
    }

    if (formAutomatizacion) {
        formAutomatizacion.addEventListener('submit', function () {
            syncTipoProgramacion(null);
        });
    }

    syncTipoProgramacion(null);

    document.querySelectorAll('.btn-toggle-estado').forEach(function (toggle) {
        toggle.addEventListener('change', function () {
            var form = toggle.closest('form');
            if (form) {
                form.submit();
            }
        });
    });
})();
