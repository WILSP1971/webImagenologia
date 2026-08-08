(function () {
    'use strict';

    const configEl = document.getElementById('visor-ver-imagenes-config');
    if (!configEl) {
        return;
    }

    const resolverUrl = configEl.dataset.resolverUrl || '/Visor/Resolver';
    const tokenUrl = configEl.dataset.tokenUrl || '/Visor/Token';
    const gridBody = document.getElementById('gridEstudiosBody');
    const alertPortal = document.getElementById('alertPortal');
    const panelDetalle = document.getElementById('panelDetalleCaso');

    function getAntiForgeryToken() {
        const input = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function mostrarAlerta(mensaje, tipo) {
        if (!alertPortal) {
            window.alert(mensaje);
            return;
        }
        alertPortal.textContent = mensaje;
        alertPortal.className = 'alert alert-' + tipo;
        alertPortal.classList.remove('d-none');
    }

    function crearBoton(noCuenta) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary visor-btn-ver-imagenes';
        btn.textContent = 'Ver Imágenes';
        btn.setAttribute('aria-label', 'Ver imágenes del caso ' + noCuenta);
        btn.dataset.noCuenta = noCuenta;
        return btn;
    }

    function inyectarEnFila(tr) {
        if (!tr || tr.dataset.visorBoton === '1') {
            return;
        }

        const noCuenta = tr.dataset.noCuenta || tr.getAttribute('data-no-cuenta') || '';
        if (!noCuenta) {
            return;
        }

        const celdaAcciones = tr.querySelector('td:last-child');
        if (!celdaAcciones) {
            return;
        }

        celdaAcciones.classList.add('visor-acciones-cell');
        const btn = crearBoton(noCuenta);
        btn.addEventListener('click', function (ev) {
            ev.preventDefault();
            ev.stopPropagation();
            abrirVisorPorCaso(noCuenta);
        });
        celdaAcciones.appendChild(btn);
        tr.dataset.visorBoton = '1';
    }

    function inyectarEnTodasLasFilas() {
        if (!gridBody) {
            return;
        }
        gridBody.querySelectorAll('tr.estudio-row').forEach(inyectarEnFila);
    }

    function asegurarBotonEnDetalle(noCuenta) {
        if (!panelDetalle || !noCuenta) {
            return;
        }

        let wrap = document.getElementById('visorDetalleAcciones');
        if (!wrap) {
            wrap = document.createElement('div');
            wrap.id = 'visorDetalleAcciones';
            wrap.className = 'mt-3';
            panelDetalle.appendChild(wrap);
        }

        wrap.innerHTML = '';
        const btn = crearBoton(noCuenta);
        btn.classList.remove('btn-sm');
        btn.addEventListener('click', function () {
            abrirVisorPorCaso(noCuenta);
        });
        wrap.appendChild(btn);
    }

    async function abrirVisorPorCaso(noCuenta) {
        try {
            const url = resolverUrl + '?caso=' + encodeURIComponent(noCuenta);
            const resp = await fetch(url, { credentials: 'same-origin' });
            if (resp.status === 404) {
                mostrarAlerta('No se encontraron estudios DICOM para la cuenta ' + noCuenta + '.', 'warning');
                return;
            }
            if (!resp.ok) {
                mostrarAlerta('No fue posible consultar el PACS/Orthanc.', 'danger');
                return;
            }

            const data = await resp.json();
            const estudios = data.estudios || data.Estudios || [];
            if (!estudios.length) {
                mostrarAlerta('No se encontraron estudios DICOM para la cuenta ' + noCuenta + '.', 'warning');
                return;
            }

            let studyUid = estudios[0].studyInstanceUID || estudios[0].StudyInstanceUID;
            if (estudios.length > 1) {
                studyUid = await seleccionarEstudio(estudios);
                if (!studyUid) {
                    return;
                }
            }

            await emitirTokenYAbrir(studyUid);
        } catch {
            mostrarAlerta('Error al abrir el visor de imágenes.', 'danger');
        }
    }

    function seleccionarEstudio(estudios) {
        return new Promise(function (resolve) {
            const modalId = 'visorSelectorModal';
            let modalEl = document.getElementById(modalId);
            if (modalEl) {
                modalEl.remove();
            }

            const items = estudios.map(function (e, idx) {
                const uid = e.studyInstanceUID || e.StudyInstanceUID || '';
                const mod = e.modality || e.Modality || '—';
                const fecha = e.studyDate || e.StudyDate || '';
                const desc = e.studyDescription || e.StudyDescription || '';
                return '<button type="button" class="btn btn-outline-primary visor-selector-item" data-uid="' +
                    uid.replace(/"/g, '') + '" data-idx="' + idx + '">' +
                    '<strong>' + mod + '</strong> ' + fecha +
                    (desc ? ' — ' + desc : '') +
                    '<br><small class="text-muted">' + uid + '</small></button>';
            }).join('');

            modalEl = document.createElement('div');
            modalEl.id = modalId;
            modalEl.className = 'modal fade';
            modalEl.tabIndex = -1;
            modalEl.innerHTML =
                '<div class="modal-dialog"><div class="modal-content">' +
                '<div class="modal-header"><h2 class="modal-title h5">Seleccione el estudio</h2>' +
                '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button></div>' +
                '<div class="modal-body visor-selector-list">' + items + '</div>' +
                '<div class="modal-footer"><button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button></div>' +
                '</div></div>';
            document.body.appendChild(modalEl);

            let chosen = null;
            modalEl.querySelectorAll('[data-uid]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    chosen = btn.getAttribute('data-uid');
                    if (typeof bootstrap !== 'undefined') {
                        bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                    }
                });
            });

            modalEl.addEventListener('hidden.bs.modal', function () {
                resolve(chosen);
                modalEl.remove();
            });

            if (typeof bootstrap !== 'undefined') {
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            } else {
                resolve(estudios[0].studyInstanceUID || estudios[0].StudyInstanceUID);
            }
        });
    }

    async function emitirTokenYAbrir(studyUid) {
        const resp = await fetch(tokenUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ studyInstanceUID: studyUid })
        });

        if (!resp.ok) {
            mostrarAlerta('No fue posible autorizar la apertura del visor.', 'danger');
            return;
        }

        const data = await resp.json();
        const viewerUrl = data.viewerUrl || data.ViewerUrl;
        if (!viewerUrl) {
            mostrarAlerta('El servidor no devolvió URL del visor.', 'danger');
            return;
        }

        window.open(viewerUrl, '_blank', 'noopener,noreferrer');
    }

    // Observa recreación del grid (cambio de empresa en portalRadiologos.js).
    if (gridBody && typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(function () {
            inyectarEnTodasLasFilas();
        });
        observer.observe(gridBody, { childList: true });
    }

    document.addEventListener('click', function (ev) {
        const target = ev.target;
        if (!(target instanceof Element)) {
            return;
        }
        const sel = target.closest('.btn-seleccionar');
        if (!sel) {
            return;
        }
        const tr = sel.closest('tr.estudio-row');
        if (!tr) {
            return;
        }
        const noCuenta = tr.dataset.noCuenta || tr.getAttribute('data-no-cuenta') || '';
        setTimeout(function () {
            asegurarBotonEnDetalle(noCuenta);
        }, 0);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', inyectarEnTodasLasFilas);
    } else {
        inyectarEnTodasLasFilas();
    }
})();
