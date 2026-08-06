(function () {
    'use strict';

    const ALLOWED_TYPES = ['audio/mpeg', 'audio/wav', 'audio/ogg', 'audio/mp4', 'audio/x-m4a'];
    const MAX_SIZE = 25 * 1024 * 1024;

    const selectEmpresa = document.getElementById('selectEmpresa');
    const nombreEmpresa = document.getElementById('nombreEmpresa');
    const gridBody = document.getElementById('gridEstudiosBody');
    const sinEstudiosMsg = document.getElementById('sinEstudiosMsg');
    const panelDetalle = document.getElementById('panelDetalle');
    const alertPortal = document.getElementById('alertPortal');

    const detalleNoCuenta = document.getElementById('detalleNoCuenta');
    const detalleNoOrden = document.getElementById('detalleNoOrden');
    const detalleServicio = document.getElementById('detalleServicio');
    const detalleDependencia = document.getElementById('detalleDependencia');
    const listaDiagnosticos = document.getElementById('listaDiagnosticos');
    const listaNotas = document.getElementById('listaNotas');

    const inputAudio = document.getElementById('inputAudio');
    const btnSubirAudio = document.getElementById('btnSubirAudio');
    const btnGrabar = document.getElementById('btnGrabar');
    const btnReproducir = document.getElementById('btnReproducir');
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

    function validarArchivo(file) {
        if (!file) {
            return 'Debe seleccionar un archivo de audio.';
        }

        if (!ALLOWED_TYPES.includes(file.type)) {
            return 'Formato no permitido';
        }

        if (file.size > MAX_SIZE) {
            return 'El archivo supera el límite de 25 MB.';
        }

        return null;
    }

    function renderGrid(estudios) {
        if (!gridBody) {
            return;
        }

        gridBody.innerHTML = '';

        if (!estudios || estudios.length === 0) {
            if (sinEstudiosMsg) {
                sinEstudiosMsg.hidden = false;
            }

            if (panelDetalle) {
                panelDetalle.hidden = true;
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

            tr.innerHTML = `
                <td>${noCuenta}</td>
                <td>${paciente || '—'}</td>
                <td>${servicio}</td>
                <td>${fechaTexto}</td>
                <td>${dependencia}</td>
                <td>${estado}</td>
                <td class="audio-icon-cell">${tieneAudio ? '<span class="text-success" aria-label="Tiene audio">&#128266;</span>' : ''}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-outline-primary btn-seleccionar" aria-label="Seleccionar estudio ${noCuenta}">
                        Seleccionar
                    </button>
                </td>`;

            gridBody.appendChild(tr);
        });
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

    function actualizarControlesAudio(tieneAudioFlag) {
        hasAudio = tieneAudioFlag;

        if (btnReproducir) {
            btnReproducir.disabled = !tieneAudioFlag;
        }

        if (btnEliminarAudio) {
            btnEliminarAudio.disabled = !tieneAudioFlag;
        }

        if (audioPlayer) {
            if (tieneAudioFlag && estudioConsecutivo && estudioEmpresa) {
                audioPlayer.src = `${obtenerAudioUrl}?consecutivo=${estudioConsecutivo.value}&empresa=${encodeURIComponent(estudioEmpresa.value)}&t=${Date.now()}`;
                audioPlayer.hidden = false;
            } else {
                audioPlayer.removeAttribute('src');
                audioPlayer.hidden = true;
            }
        }
    }

    function marcarAudioEnGrid(consecutivo) {
        const row = gridBody?.querySelector(`tr[data-consecutivo="${consecutivo}"]`);
        if (!row) {
            return;
        }

        const cell = row.querySelector('.audio-icon-cell');
        if (cell) {
            cell.innerHTML = '<span class="text-success" aria-label="Tiene audio">&#128266;</span>';
        }
    }

    async function cargarDetalle(consecutivo, empresa) {
        ocultarAlerta();

        try {
            const resp = await fetch(`${detalleUrl}?consecutivo=${consecutivo}&empresa=${encodeURIComponent(empresa)}`);
            if (!resp.ok) {
                throw new Error('Error al cargar detalle');
            }

            const data = await resp.json();
            const estudio = data.estudio ?? data.Estudio;

            if (panelDetalle) {
                panelDetalle.hidden = false;
            }

            if (estudioConsecutivo) {
                estudioConsecutivo.value = consecutivo;
            }

            if (estudioEmpresa) {
                estudioEmpresa.value = empresa;
            }

            if (detalleNoCuenta) {
                detalleNoCuenta.value = estudio.noCuenta ?? estudio.NoCuenta ?? '';
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
            actualizarControlesAudio(data.hasAudio ?? data.HasAudio ?? false);

            if (inputAudio) {
                inputAudio.value = '';
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
            actualizarControlesAudio(true);
            marcarAudioEnGrid(estudioConsecutivo.value);
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
            actualizarControlesAudio(false);

            const row = gridBody?.querySelector(`tr[data-consecutivo="${estudioConsecutivo.value}"]`);
            const cell = row?.querySelector('.audio-icon-cell');
            if (cell) {
                cell.innerHTML = '';
            }
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

            cargarDetalle(row.dataset.consecutivo, row.dataset.empresa);
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

    if (btnReproducir && audioPlayer) {
        btnReproducir.addEventListener('click', function () {
            if (hasAudio) {
                audioPlayer.play();
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
