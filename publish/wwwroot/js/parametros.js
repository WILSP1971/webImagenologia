(function () {
    'use strict';

    const selectDependencia = document.getElementById('selectDependencia');
    const nombreDependencia = document.getElementById('nombreDependencia');
    const selectServicio = document.getElementById('selectServicio');
    const nombreServicio = document.getElementById('nombreServicio');
    const codEsquema = document.getElementById('codEsquema');
    const nombreEmpresa = document.getElementById('nombreEmpresa');
    const empresaCheckboxes = document.querySelectorAll('.empresa-checkbox');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-estudio');
    const modalElement = document.getElementById('modalEliminarEstudio');
    const empresaEliminar = document.getElementById('empresaEliminar');
    const codDependenciaEliminar = document.getElementById('codDependenciaEliminar');
    const codServicioEliminar = document.getElementById('codServicioEliminar');
    const mensajeEliminar = document.getElementById('mensajeEliminarEstudio');

    function syncNombreDependencia() {
        if (!selectDependencia || !nombreDependencia) {
            return;
        }

        const selectedOption = selectDependencia.options[selectDependencia.selectedIndex];
        nombreDependencia.value = selectedOption?.dataset.nombre ?? '';
    }

    function syncNombreServicio() {
        if (!selectServicio || !nombreServicio) {
            return;
        }

        const selectedOption = selectServicio.options[selectServicio.selectedIndex];
        nombreServicio.value = selectedOption?.dataset.nombre ?? '';

        if (codEsquema) {
            codEsquema.value = selectedOption?.dataset.esquema ?? '';
        }
    }

    function resetServiciosDropdown() {
        if (!selectServicio) {
            return;
        }

        selectServicio.innerHTML = '<option value="">-- Seleccione servicio --</option>';
        syncNombreServicio();
    }

    async function loadServiciosPorDependencia(codDependencia) {
        if (!selectServicio || !codDependencia) {
            resetServiciosDropdown();
            return;
        }

        const baseUrl = selectServicio.dataset.serviciosUrl;
        if (!baseUrl) {
            return;
        }

        const url = `${baseUrl}?codDependencia=${encodeURIComponent(codDependencia)}`;

        try {
            const response = await fetch(url, {
                headers: { Accept: 'application/json' }
            });

            if (!response.ok) {
                resetServiciosDropdown();
                return;
            }

            const servicios = await response.json();
            resetServiciosDropdown();

            servicios.forEach((servicio) => {
                const option = document.createElement('option');
                option.value = servicio.codServicio ?? servicio.CodServicio ?? '';
                option.textContent = `${servicio.nombreServicio ?? servicio.NombreServicio} (${option.value})`;
                option.dataset.nombre = servicio.nombreServicio ?? servicio.NombreServicio ?? '';
                option.dataset.esquema = servicio.codEsquema ?? servicio.CodEsquema ?? '';
                selectServicio.appendChild(option);
            });
        } catch {
            resetServiciosDropdown();
        }
    }

    async function onDependenciaChange() {
        syncNombreDependencia();

        if (!selectDependencia) {
            return;
        }

        await loadServiciosPorDependencia(selectDependencia.value);
    }

    function syncNombreEmpresa(event) {
        if (!nombreEmpresa) {
            return;
        }

        const checkbox = event?.target;
        if (checkbox?.checked && checkbox.dataset.nombre) {
            nombreEmpresa.value = checkbox.dataset.nombre;
            return;
        }

        const checked = Array.from(empresaCheckboxes).filter((item) => item.checked);
        const lastChecked = checked[checked.length - 1];
        nombreEmpresa.value = lastChecked?.dataset.nombre ?? '';
    }

    function bindDeleteModal() {
        if (!modalElement || !empresaEliminar || !codDependenciaEliminar || !codServicioEliminar || !mensajeEliminar) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

        deleteButtons.forEach((button) => {
            button.addEventListener('click', () => {
                empresaEliminar.value = button.dataset.empresa ?? '';
                codDependenciaEliminar.value = button.dataset.codDependencia ?? '';
                codServicioEliminar.value = button.dataset.codServicio ?? '';
                const descripcion = button.dataset.descripcion ?? '';
                mensajeEliminar.textContent = `¿Desea eliminar el estudio ${descripcion}?`;
                modal.show();
            });
        });
    }

    if (selectDependencia) {
        selectDependencia.addEventListener('change', onDependenciaChange);
        syncNombreDependencia();
    }

    if (selectServicio) {
        selectServicio.addEventListener('change', syncNombreServicio);
        syncNombreServicio();
    }

    empresaCheckboxes.forEach((checkbox) => {
        checkbox.addEventListener('change', syncNombreEmpresa);
    });

    syncNombreEmpresa();
    bindDeleteModal();
})();
