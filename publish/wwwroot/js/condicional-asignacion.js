(function () {
    'use strict';

    const selectEmpresa = document.getElementById('selectEmpresa');
    const selectMedico = document.getElementById('selectMedico');
    const nombreMedico = document.getElementById('nombreMedico');
    const selectDependencia = document.getElementById('selectDependencia');
    const nombreDependencia = document.getElementById('nombreDependencia');
    const selectServicio = document.getElementById('selectServicio');
    const nombreServicio = document.getElementById('nombreServicio');
    const tablaAsignaciones = document.getElementById('tablaAsignaciones');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-asignacion');
    const modalElement = document.getElementById('modalEliminarAsignacion');
    const empresaEliminar = document.getElementById('empresaEliminar');
    const cedulaMedicoEliminar = document.getElementById('cedulaMedicoEliminar');
    const codDependenciaEliminar = document.getElementById('codDependenciaEliminar');
    const codServicioEliminar = document.getElementById('codServicioEliminar');
    const mensajeEliminar = document.getElementById('mensajeEliminarAsignacion');

    function resetDropdown(selectElement, placeholder) {
        if (!selectElement) {
            return;
        }

        selectElement.innerHTML = `<option value="">${placeholder}</option>`;
    }

    function syncNombreFromSelect(selectElement, targetInput) {
        if (!selectElement || !targetInput) {
            return;
        }

        const selectedOption = selectElement.options[selectElement.selectedIndex];
        targetInput.value = selectedOption?.dataset.nombre ?? '';
    }

    async function fetchJson(url) {
        const response = await fetch(url, {
            headers: { Accept: 'application/json' }
        });

        if (!response.ok) {
            return [];
        }

        return response.json();
    }

    async function loadMedicosPorEmpresa(empresa) {
        if (!selectMedico || !selectEmpresa) {
            return;
        }

        resetDropdown(selectMedico, '-- Seleccione médico --');
        syncNombreFromSelect(selectMedico, nombreMedico);

        const baseUrl = selectEmpresa.dataset.medicosUrl;
        if (!baseUrl || !empresa) {
            return;
        }

        try {
            const medicos = await fetchJson(`${baseUrl}?empresa=${encodeURIComponent(empresa)}`);
            medicos.forEach((medico) => {
                const cedula = medico.cedula ?? medico.Cedula ?? '';
                const nombre = medico.nombre ?? medico.Nombre ?? '';
                const option = document.createElement('option');
                option.value = cedula;
                option.textContent = `${nombre} (${cedula})`;
                option.dataset.nombre = nombre;
                selectMedico.appendChild(option);
            });
        } catch {
            resetDropdown(selectMedico, '-- Seleccione médico --');
        }
    }

    async function loadDependenciasPorEmpresa(empresa) {
        if (!selectDependencia || !selectEmpresa) {
            return;
        }

        resetDropdown(selectDependencia, '-- Seleccione dependencia --');
        syncNombreFromSelect(selectDependencia, nombreDependencia);
        resetServiciosDropdown();

        const baseUrl = selectEmpresa.dataset.dependenciasUrl;
        if (!baseUrl || !empresa) {
            return;
        }

        try {
            const dependencias = await fetchJson(`${baseUrl}?empresa=${encodeURIComponent(empresa)}`);
            dependencias.forEach((dependencia) => {
                const codigo = dependencia.codDependencia ?? dependencia.CodDependencia ?? '';
                const nombre = dependencia.nombreDependencia ?? dependencia.NombreDependencia ?? '';
                const option = document.createElement('option');
                option.value = codigo;
                option.textContent = `${nombre} (${codigo})`;
                option.dataset.nombre = nombre;
                selectDependencia.appendChild(option);
            });
        } catch {
            resetDropdown(selectDependencia, '-- Seleccione dependencia --');
        }
    }

    function resetServiciosDropdown() {
        resetDropdown(selectServicio, '-- Seleccione servicio --');
        syncNombreFromSelect(selectServicio, nombreServicio);
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

        try {
            const servicios = await fetchJson(`${baseUrl}?codDependencia=${encodeURIComponent(codDependencia)}`);
            resetServiciosDropdown();

            servicios.forEach((servicio) => {
                const codigo = servicio.codServicio ?? servicio.CodServicio ?? '';
                const nombre = servicio.nombreServicio ?? servicio.NombreServicio ?? '';
                const option = document.createElement('option');
                option.value = codigo;
                option.textContent = `${nombre} (${codigo})`;
                option.dataset.nombre = nombre;
                selectServicio.appendChild(option);
            });
        } catch {
            resetServiciosDropdown();
        }
    }

    function filterGridByEmpresa(empresa) {
        if (!tablaAsignaciones) {
            return;
        }

        const rows = tablaAsignaciones.querySelectorAll('tbody tr');
        rows.forEach((row) => {
            const rowEmpresa = row.dataset.empresa ?? '';
            if (!empresa) {
                row.style.display = '';
                return;
            }

            row.style.display = rowEmpresa === empresa ? '' : 'none';
        });
    }

    async function onEmpresaChange() {
        if (!selectEmpresa) {
            return;
        }

        const empresa = selectEmpresa.value;
        await Promise.all([
            loadMedicosPorEmpresa(empresa),
            loadDependenciasPorEmpresa(empresa)
        ]);
        filterGridByEmpresa(empresa);
    }

    function bindDeleteModal() {
        if (!modalElement || !empresaEliminar || !cedulaMedicoEliminar
            || !codDependenciaEliminar || !codServicioEliminar || !mensajeEliminar) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

        deleteButtons.forEach((button) => {
            button.addEventListener('click', () => {
                empresaEliminar.value = button.dataset.empresa ?? '';
                cedulaMedicoEliminar.value = button.dataset.cedulaMedico ?? '';
                codDependenciaEliminar.value = button.dataset.codDependencia ?? '';
                codServicioEliminar.value = button.dataset.codServicio ?? '';
                const descripcion = button.dataset.descripcion ?? '';
                mensajeEliminar.textContent = `¿Desea eliminar la asignación de ${descripcion}?`;
                modal.show();
            });
        });
    }

    if (selectEmpresa) {
        selectEmpresa.addEventListener('change', onEmpresaChange);
        if (selectEmpresa.value) {
            filterGridByEmpresa(selectEmpresa.value);
        }
    }

    if (selectMedico) {
        selectMedico.addEventListener('change', () => syncNombreFromSelect(selectMedico, nombreMedico));
        syncNombreFromSelect(selectMedico, nombreMedico);
    }

    if (selectDependencia) {
        selectDependencia.addEventListener('change', async () => {
            syncNombreFromSelect(selectDependencia, nombreDependencia);
            await loadServiciosPorDependencia(selectDependencia.value);
        });
        syncNombreFromSelect(selectDependencia, nombreDependencia);
    }

    if (selectServicio) {
        selectServicio.addEventListener('change', () => syncNombreFromSelect(selectServicio, nombreServicio));
        syncNombreFromSelect(selectServicio, nombreServicio);
    }

    bindDeleteModal();
})();
