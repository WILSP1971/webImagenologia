(function () {
    'use strict';

    const ALLOWED_EXTENSIONS = ['.mp3', '.wav', '.ogg', '.m4a', '.aac', '.flac', '.webm', '.wma', '.opus', '.amr', '.3gp', '.aiff', '.aif', '.mp4', '.mpeg', '.mpga', '.weba'];
    const BLOCKED_EXTENSIONS = ['.exe', '.bat', '.cmd', '.msi', '.dll', '.com', '.scr'];
    const MAX_SIZE = 25 * 1024 * 1024;

    const selectEmpresa = document.getElementById('selectEmpresa');
    const nombreEmpresa = document.getElementById('nombreEmpresa');
    const gridBody = document.getElementById('gridEstudiosBody');
    const sinEstudiosMsg = document.getElementById('sinEstudiosMsg');
    const panelDetalleCaso = document.getElementById('panelDetalleCaso');
    const alertPortal = document.getElementById('alertPortal');

    const detalleNoCuenta = document.getElementById('detalleNoCuenta');
    const detalleNoOrden = document.getElementById('detalleNoOrden');
    const detalleServicio = document.getElementById('detalleServicio');
    const detalleDependencia = document.getElementById('detalleDependencia');
    const listaDiagnosticos = document.getElementById('listaDiagnosticos');
    const listaNotas = document.getElementById('listaNotas');

    const audioSinSeleccion = document.getElementById('audioSinSeleccion');
    const audioFormulario = document.getElementById('audioFormulario');
    const audioCasoLabel = document.getElementById('audioCasoLabel');
    const audioPlayerHint = document.getElementById('audioPlayerHint');
    const tabAudioBtn = document.getElementById('tab-audio-btn');

    const inputAudio = document.getElementById('inputAudio');
    const btnSubirAudio = document.getElementById('btnSubirAudio');
    const btnGrabar = document.getElementById('btnGrabar');
    const btnEliminarAudio = document.getElementById('btnEliminarAudio');
    const audioPlayer = document.getElementById('audioPlayer');
    const estudioConsecutivo = document.getElementById('estudioConsecutivo');
    const estudioEmpresa = document.getElementById('estudioEmpresa');

    const btnIniciarGrabacion = document.getElementById('btnIniciarGrabacion');
    const btnDetenerGrabacion = document.getElementById('btnDetenerGrabacion');
    const btnSubirGrabacion = document.getElementById('btnSubirGrabacion');
    const estadoGrabacion = document.getElementById('estadoGrabacion');
    const audioPreview = document.getElementById('audioPreview');
    const modalGrabar = document.getElementById('modalGrabar');

    let mediaRecorder = null;
    let recordedChunks = [];
    let recordedBlob = null;
    let recordedMimeType = 'audio/ogg';
    let hasAudio = false;
    let selectedNoCuenta = '';

    const detalleUrl = '/PortalRadiologos/DetalleEstudio';
    const subirAudioUrl = '/PortalRadiologos/SubirAudio';
    const obtenerAudioUrl = '/PortalRadiologos/ObtenerAudio';
    const eliminarAudioUrl = '/PortalRadiologos/EliminarAudio';

    function getAntiForgeryToken() {
        const input = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function mostrarAlerta(mensaje, tipo) {
        if (!alertPortal) {
            return;
        }

        alertPortal.textContent = mensaje;
        alertPortal.className = `alert alert-${tipo}`;
        alertPortal.classList.remove('d-none');
    }

    function ocultarAlerta() {
        if (alertPortal) {
            alertPortal.classList.add('d-none');
        }
    }

    function extensionDeArchivo(fileName) {
        const idx = (fileName || '').lastIndexOf('.');
        return idx >= 0 ? fileName.slice(idx).toLowerCase() : '';
    }

    function esArchivoAudioPermitido(file) {
        if (!file) {
            return 'Debe seleccionar un archivo de audio.';
        }

        const extension = extensionDeArchivo(file.name);
        if (BLOCKED_EXTENSIONS.includes(extension)) {
            return 'Formato no permitido.';
        }

        const tipo = (file.type || '').toLowerCase();
        if (tipo.startsWith('audio/')) {
            return null;
        }

        if (ALLOWED_EXTENSIONS.includes(extension)) {
            return null;
        }

        return 'Formato de audio no permitido.';
    }

    function validarArchivo(file) {
        const errorTipo = esArchivoAudioPermitido(file);
        if (errorTipo) {
            return errorTipo;
        }

        if (file.size > MAX_SIZE) {
            return 'El archivo supera el límite de 25 MB.';
        }

        return null;
    }

    function audioUrl(consecutivo, empresa) {
        return `${obtenerAudioUrl}?consecutivo=${encodeURIComponent(consecutivo)}&empresa=${encodeURIComponent(empresa)}&t=${Date.now()}`;
    }

    function htmlBotonPlayGrid(consecutivo, empresa, noCuenta) {
        return `<button type="button"
                    class="btn btn-sm btn-outline-success btn-play-grid"
                    data-consecutivo="${consecutivo}"
                    data-empresa="${empresa}"
                    data-no-cuenta="${noCuenta}"
                    aria-label="Reproducir audio cuenta ${noCuenta}"
                    title="Reproducir audio">
                ▶ Reproducir
            </button>`;
    }

    function renderGrid(estudios) {
        if (!gridBody) {
            return;
        }

        gridBody.innerHTML = '';
        resetPanelAudio();

        if (!estudios || estudios.length === 0) {
            if (sinEstudiosMsg) {
                sinEstudiosMsg.hidden = false;
            }

            if (panelDetalleCaso) {
                panelDetalleCaso.hidden = true;
            }

            if (listaDiagnosticos) {
                listaDiagnosticos.innerHTML = 'Seleccione un estudio del grid para ver los diagnósticos.';
            }

            if (listaNotas) {
                listaNotas.innerHTML = 'Seleccione un estudio del grid para ver las notas médicas.';
            }

            return;
        }

        if (sinEstudiosMsg) {
            sinEstudiosMsg.hidden = true;
        }

        estudios.forEach(function (estudio) {
            const consecutivo = estudio.consecutivo ?? estudio.Consecutivo;
            const empresa = estudio.empresa ?? estudio.Empresa;
            const noCuenta = estudio.noCuenta ?? estudio.NoCuenta;
            const paciente = estudio.paciente ?? estudio.Paciente ?? '—';
            const servicio = estudio.servicio ?? estudio.Servicio;
            const fecha = estudio.fechaProgramacion ?? estudio.FechaProgramacion;
            const dependencia = estudio.dependencia ?? estudio.Dependencia;
            const estado = estudio.estado ?? estudio.Estado;
            const tieneAudio = estudio.tieneAudio ?? estudio.TieneAudio;

            const tr = document.createElement('tr');
            tr.className = 'estudio-row';
            tr.dataset.consecutivo = consecutivo;
            tr.dataset.empresa = empresa;
            tr.dataset.noCuenta = noCuenta;

            const fechaTexto = typeof fecha === 'string'
                ? fecha.split('T')[0].split('-').reverse().join('/').replace(/^(\d{2}\/\d{2})\/(\d{4})$/, '$1/$2')
                : fecha;

            const celdaAudio = tieneAudio
                ? htmlBotonPlayGrid(consecutivo, empresa, noCuenta)
                : '<span class="text-muted small">—</span>';

            tr.innerHTML = `
                <td>${noCuenta}</td>
                <td>${paciente || '—'}</td>
                <td>${servicio}</td>
                <td>${fechaTexto}</td>
                <td>${dependencia}</td>
                <td>${estado}</td>
                <td class="audio-icon-cell">${celdaAudio}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-outline-primary btn-seleccionar" aria-label="Seleccionar estudio ${noCuenta}">
                        Seleccionar
                    </button>
                </td>`;

            gridBody.appendChild(tr);
        });
    }

    function resetPanelAudio() {
        hasAudio = false;
        selectedNoCuenta = '';

        if (estudioConsecutivo) {
            estudioConsecutivo.value = '';
        }

        if (estudioEmpresa) {
            estudioEmpresa.value = '';
        }

        if (audioSinSeleccion) {
            audioSinSeleccion.hidden = false;
        }

        if (audioFormulario) {
            audioFormulario.hidden = true;
        }

        if (inputAudio) {
            inputAudio.value = '';
        }

        actualizarControlesAudio(false);
    }

    function mostrarPanelAudio(noCuenta) {
        selectedNoCuenta = noCuenta || '';

        if (audioSinSeleccion) {
            audioSinSeleccion.hidden = true;
        }

        if (audioFormulario) {
            audioFormulario.hidden = false;
        }

        if (audioCasoLabel) {
            audioCasoLabel.textContent = selectedNoCuenta
                ? `No. de cuenta: ${selectedNoCuenta}`
                : '';
        }
    }

    function activarTabAudio() {
        if (!tabAudioBtn || typeof bootstrap === 'undefined') {
            return;
        }

        const tab = bootstrap.Tab.getOrCreateInstance(tabAudioBtn);
        tab.show();
    }

    async function cargarEstudiosPorEmpresa(empresa) {
        if (!selectEmpresa || !empresa) {
            return;
        }

        const url = `${selectEmpresa.dataset.estudiosUrl}?empresa=${encodeURIComponent(empresa)}`;

        try {
            const resp = await fetch(url);
            if (!resp.ok) {
                throw new Error('Error al cargar estudios');
            }

            const estudios = await resp.json();
            renderGrid(estudios);
        } catch {
            mostrarAlerta('No fue posible cargar los estudios programados.', 'danger');
        }
    }

    function renderDiagnosticos(diagnosticos) {
        if (!listaDiagnosticos) {
            return;
        }

        if (!diagnosticos || diagnosticos.length === 0) {
            listaDiagnosticos.innerHTML = '<p class="text-muted mb-0">Sin diagnósticos registrados.</p>';
            return;
        }

        listaDiagnosticos.innerHTML = diagnosticos.map(function (d) {
            const descripcion = d.descripcion ?? d.Descripcion ?? '—';
            return `<div class="mb-2"><strong>Cuenta ${d.noCuenta ?? d.NoCuenta}:</strong> ${descripcion}</div>`;
        }).join('');
    }

    function renderNotas(notas) {
        if (!listaNotas) {
            return;
        }

        if (!notas || notas.length === 0) {
            listaNotas.innerHTML = '<p class="text-muted mb-0">Sin notas médicas registradas.</p>';
            return;
        }

        listaNotas.innerHTML = notas.map(function (n) {
            const nota = n.nota ?? n.Nota ?? '—';
            const fecha = n.fecha ?? n.Fecha;
            const fechaTexto = fecha ? new Date(fecha).toLocaleDateString('es-CO') : '';
            return `<div class="mb-2"><strong>${fechaTexto}</strong><br>${nota}</div>`;
        }).join('');
    }

    function actualizarControlesAudio(tieneAudioFlag, consecutivo, empresa) {
        hasAudio = tieneAudioFlag;

        if (btnEliminarAudio) {
            btnEliminarAudio.disabled = !tieneAudioFlag;
        }

        if (btnGrabar) {
            btnGrabar.disabled = !consecutivo;
        }

        if (audioPlayer) {
            if (tieneAudioFlag && consecutivo && empresa) {
                audioPlayer.src = audioUrl(consecutivo, empresa);
                audioPlayer.load();
            } else {
                audioPlayer.pause();
                audioPlayer.removeAttribute('src');
                audioPlayer.load();
            }
        }

        if (audioPlayerHint) {
            audioPlayerHint.textContent = tieneAudioFlag
                ? 'Audio disponible. Use el reproductor para escucharlo.'
                : 'Sin audio cargado para este caso.';
        }
    }

    function marcarAudioEnGrid(consecutivo, empresa, noCuenta) {
        const row = gridBody?.querySelector(`tr[data-consecutivo="${consecutivo}"]`);
        if (!row) {
            return;
        }

        const cell = row.querySelector('.audio-icon-cell');
        if (cell) {
            cell.innerHTML = htmlBotonPlayGrid(consecutivo, empresa, noCuenta || row.dataset.noCuenta || '');
        }
    }

    function limpiarAudioEnGrid(consecutivo) {
        const row = gridBody?.querySelector(`tr[data-consecutivo="${consecutivo}"]`);
        const cell = row?.querySelector('.audio-icon-cell');
        if (cell) {
            cell.innerHTML = '<span class="text-muted small">—</span>';
        }
    }

    async function reproducirAudio(consecutivo, empresa, activarTab) {
        if (!consecutivo || !empresa) {
            return;
        }

        if (estudioConsecutivo) {
            estudioConsecutivo.value = consecutivo;
        }

        if (estudioEmpresa) {
            estudioEmpresa.value = empresa;
        }

        const row = gridBody?.querySelector(`tr[data-consecutivo="${consecutivo}"]`);
        if (row) {
            selectedNoCuenta = row.dataset.noCuenta || '';
            gridBody.querySelectorAll('tr').forEach(function (r) {
                r.classList.remove('table-active');
            });
            row.classList.add('table-active');
        }

        mostrarPanelAudio(selectedNoCuenta);
        actualizarControlesAudio(true, consecutivo, empresa);

        if (activarTab) {
            activarTabAudio();
        }

        if (audioPlayer) {
            try {
                await audioPlayer.play();
            } catch {
                /* El usuario puede iniciar la reproducción manualmente */
            }
        }
    }

    async function cargarDetalle(consecutivo, empresa, opciones) {
        const opts = opciones || {};
        ocultarAlerta();

        try {
            const resp = await fetch(`${detalleUrl}?consecutivo=${consecutivo}&empresa=${encodeURIComponent(empresa)}`);
            if (!resp.ok) {
                throw new Error('Error al cargar detalle');
            }

            const data = await resp.json();
            const estudio = data.estudio ?? data.Estudio;
            const tieneAudioFlag = data.hasAudio ?? data.HasAudio ?? false;

            if (panelDetalleCaso) {
                panelDetalleCaso.hidden = false;
            }

            if (estudioConsecutivo) {
                estudioConsecutivo.value = consecutivo;
            }

            if (estudioEmpresa) {
                estudioEmpresa.value = empresa;
            }

            const noCuenta = estudio.noCuenta ?? estudio.NoCuenta ?? '';
            selectedNoCuenta = noCuenta;

            if (detalleNoCuenta) {
                detalleNoCuenta.value = noCuenta;
            }

            if (detalleNoOrden) {
                detalleNoOrden.value = estudio.noOrden ?? estudio.NoOrden ?? '';
            }

            if (detalleServicio) {
                detalleServicio.value = estudio.servicio ?? estudio.Servicio ?? '';
            }

            if (detalleDependencia) {
                detalleDependencia.value = estudio.dependencia ?? estudio.Dependencia ?? '';
            }

            renderDiagnosticos(data.diagnosticos ?? data.Diagnosticos);
            renderNotas(data.notasMedicas ?? data.NotasMedicas);
            mostrarPanelAudio(noCuenta);
            actualizarControlesAudio(tieneAudioFlag, consecutivo, empresa);

            if (inputAudio) {
                inputAudio.value = '';
            }

            if (opts.activarTabAudio) {
                activarTabAudio();
            }
        } catch {
            mostrarAlerta('No fue posible cargar el detalle del estudio.', 'danger');
        }
    }

    async function subirAudio(file) {
        const error = validarArchivo(file);
        if (error) {
            mostrarAlerta(error, 'danger');
            return false;
        }

        if (!estudioConsecutivo?.value || !estudioEmpresa?.value) {
            mostrarAlerta('Seleccione un estudio antes de subir audio.', 'warning');
            return false;
        }

        const formData = new FormData();
        formData.append('archivo', file);
        formData.append('consecutivo', estudioConsecutivo.value);
        formData.append('empresa', estudioEmpresa.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch(subirAudioUrl, {
                method: 'POST',
                body: formData
            });

            if (!resp.ok) {
                const errorText = await resp.text();
                mostrarAlerta(errorText || 'Error al subir audio.', 'danger');
                return false;
            }

            mostrarAlerta('Audio guardado correctamente.', 'success');
            const consecutivo = estudioConsecutivo.value;
            const empresa = estudioEmpresa.value;
            actualizarControlesAudio(true, consecutivo, empresa);
            marcarAudioEnGrid(consecutivo, empresa, selectedNoCuenta);

            if (audioPlayer) {
                try {
                    await audioPlayer.play();
                } catch {
                    /* reproducción manual */
                }
            }

            return true;
        } catch {
            mostrarAlerta('Error de comunicación al subir audio.', 'danger');
            return false;
        }
    }

    async function eliminarAudio() {
        if (!estudioConsecutivo?.value || !estudioEmpresa?.value) {
            return;
        }

        const formData = new FormData();
        formData.append('consecutivo', estudioConsecutivo.value);
        formData.append('empresa', estudioEmpresa.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch(eliminarAudioUrl, {
                method: 'POST',
                body: formData
            });

            if (!resp.ok) {
                mostrarAlerta('No fue posible eliminar el audio.', 'danger');
                return;
            }

            mostrarAlerta('Audio eliminado correctamente.', 'success');
            actualizarControlesAudio(false, estudioConsecutivo.value, estudioEmpresa.value);
            limpiarAudioEnGrid(estudioConsecutivo.value);
        } catch {
            mostrarAlerta('Error de comunicación al eliminar audio.', 'danger');
        }
    }

    function resetGrabacion() {
        recordedChunks = [];
        recordedBlob = null;

        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.stop();
        }

        mediaRecorder = null;

        if (btnIniciarGrabacion) {
            btnIniciarGrabacion.disabled = false;
        }

        if (btnDetenerGrabacion) {
            btnDetenerGrabacion.disabled = true;
        }

        if (btnSubirGrabacion) {
            btnSubirGrabacion.disabled = true;
        }

        if (estadoGrabacion) {
            estadoGrabacion.textContent = 'Listo para grabar.';
        }

        if (audioPreview) {
            audioPreview.removeAttribute('src');
            audioPreview.hidden = true;
        }
    }

    if (selectEmpresa) {
        selectEmpresa.addEventListener('change', function () {
            const option = selectEmpresa.options[selectEmpresa.selectedIndex];
            if (nombreEmpresa) {
                nombreEmpresa.value = option?.dataset.nombre ?? option?.text ?? '';
            }

            cargarEstudiosPorEmpresa(selectEmpresa.value);
        });
    }

    if (gridBody) {
        gridBody.addEventListener('click', function (event) {
            const playBtn = event.target.closest('.btn-play-grid');
            if (playBtn) {
                event.preventDefault();
                reproducirAudio(
                    playBtn.dataset.consecutivo,
                    playBtn.dataset.empresa,
                    true);
                return;
            }

            const button = event.target.closest('.btn-seleccionar');
            if (!button) {
                return;
            }

            const row = button.closest('tr');
            if (!row) {
                return;
            }

            gridBody.querySelectorAll('tr').forEach(function (r) {
                r.classList.remove('table-active');
            });
            row.classList.add('table-active');

            cargarDetalle(row.dataset.consecutivo, row.dataset.empresa, { activarTabAudio: false });
        });
    }

    if (btnSubirAudio) {
        btnSubirAudio.addEventListener('click', function () {
            if (!inputAudio?.files?.length) {
                mostrarAlerta('Seleccione un archivo de audio.', 'warning');
                return;
            }

            subirAudio(inputAudio.files[0]);
        });
    }

    if (inputAudio) {
        inputAudio.addEventListener('change', function () {
            if (!inputAudio.files?.length) {
                return;
            }

            const error = validarArchivo(inputAudio.files[0]);
            if (error) {
                mostrarAlerta(error, 'danger');
                inputAudio.value = '';
            } else {
                ocultarAlerta();
            }
        });
    }

    if (btnEliminarAudio) {
        btnEliminarAudio.addEventListener('click', eliminarAudio);
    }

    if (btnIniciarGrabacion) {
        btnIniciarGrabacion.addEventListener('click', async function () {
            if (!navigator.mediaDevices?.getUserMedia) {
                mostrarAlerta('Su navegador no soporta grabación de audio.', 'warning');
                return;
            }

            const mimeCandidates = ['audio/ogg;codecs=opus', 'audio/mp4', 'audio/ogg'];
            recordedMimeType = '';
            for (const mime of mimeCandidates) {
                if (MediaRecorder.isTypeSupported(mime)) {
                    recordedMimeType = mime.split(';')[0];
                    break;
                }
            }

            if (!recordedMimeType) {
                mostrarAlerta('Su navegador no soporta un formato de grabación compatible (ogg/m4a).', 'warning');
                return;
            }

            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                mediaRecorder = new MediaRecorder(stream, { mimeType: recordedMimeType });
                recordedChunks = [];

                mediaRecorder.ondataavailable = function (e) {
                    if (e.data.size > 0) {
                        recordedChunks.push(e.data);
                    }
                };

                mediaRecorder.onstop = function () {
                    recordedBlob = new Blob(recordedChunks, { type: recordedMimeType });
                    if (audioPreview) {
                        audioPreview.src = URL.createObjectURL(recordedBlob);
                        audioPreview.hidden = false;
                    }

                    if (btnSubirGrabacion) {
                        btnSubirGrabacion.disabled = false;
                    }

                    if (estadoGrabacion) {
                        estadoGrabacion.textContent = 'Grabación finalizada. Puede subir el audio.';
                    }

                    stream.getTracks().forEach(function (track) {
                        track.stop();
                    });
                };

                mediaRecorder.start();
                btnIniciarGrabacion.disabled = true;
                btnDetenerGrabacion.disabled = false;
                estadoGrabacion.textContent = 'Grabando...';
            } catch {
                mostrarAlerta('No se pudo acceder al micrófono.', 'danger');
            }
        });
    }

    if (btnDetenerGrabacion) {
        btnDetenerGrabacion.addEventListener('click', function () {
            if (mediaRecorder && mediaRecorder.state !== 'inactive') {
                mediaRecorder.stop();
            }

            btnIniciarGrabacion.disabled = false;
            btnDetenerGrabacion.disabled = true;
        });
    }

    if (btnSubirGrabacion) {
        btnSubirGrabacion.addEventListener('click', async function () {
            if (!recordedBlob) {
                return;
            }

            const file = new File(
                [recordedBlob],
                recordedMimeType.includes('ogg') ? 'grabacion.ogg' : 'grabacion.m4a',
                { type: recordedMimeType });
            const subido = await subirAudio(file);

            if (subido && modalGrabar) {
                const modal = bootstrap.Modal.getInstance(modalGrabar);
                modal?.hide();
            }
        });
    }

    if (modalGrabar) {
        modalGrabar.addEventListener('hidden.bs.modal', resetGrabacion);
    }
})();
