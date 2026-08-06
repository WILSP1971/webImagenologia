'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const selectDependencia = document.getElementById('selectDependencia');
    const selectServicio = document.getElementById('selectServicio');
    const selectMedico = document.getElementById('selectMedico');
    const tipoReporteInput = document.getElementById('tipoReporte');
    const reporteTabs = document.getElementById('reporteTabs');
    const btnExportarExcel = document.getElementById('btnExportarExcel');
    const empresaCheckboxes = document.querySelectorAll('.empresa-checkbox');

    if (reporteTabs && tipoReporteInput) {
        reporteTabs.addEventListener('click', function (event) {
            const tabButton = event.target.closest('[data-tipo-reporte]');
            if (!tabButton) {
                return;
            }

            reporteTabs.querySelectorAll('.nav-link').forEach(function (link) {
                link.classList.remove('active');
                link.setAttribute('aria-selected', 'false');
            });

            tabButton.classList.add('active');
            tabButton.setAttribute('aria-selected', 'true');
            tipoReporteInput.value = tabButton.dataset.tipoReporte;
        });
    }

    if (selectDependencia && selectServicio) {
        selectDependencia.addEventListener('change', function () {
            const codDependencia = selectDependencia.value;
            const serviciosUrl = selectDependencia.dataset.serviciosUrl;

            resetServiciosSelect();

            if (!codDependencia || !serviciosUrl) {
                return;
            }

            fetch(`${serviciosUrl}?codDependencia=${encodeURIComponent(codDependencia)}`)
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Error al cargar servicios');
                    }

                    return response.json();
                })
                .then(function (servicios) {
                    servicios.forEach(function (servicio) {
                        const option = document.createElement('option');
                        option.value = servicio.codServicio;
                        option.textContent = servicio.nombreServicio;
                        selectServicio.appendChild(option);
                    });
                })
                .catch(function () {
                    resetServiciosSelect();
                });
        });
    }

    if (empresaCheckboxes.length > 0 && selectMedico) {
        empresaCheckboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', reloadMedicos);
        });

        reloadMedicos();
    }

    if (btnExportarExcel) {
        btnExportarExcel.addEventListener('click', function () {
            const exportUrl = btnExportarExcel.dataset.exportUrl;
            if (!exportUrl) {
                return;
            }

            const params = new URLSearchParams();
            const empresasSeleccionadas = getEmpresasSeleccionadas();

            if (empresasSeleccionadas.length === 0) {
                alert('Debe seleccionar al menos una empresa.');
                return;
            }

            empresasSeleccionadas.forEach(function (empresa) {
                params.append('empresas', empresa);
            });

            const fechaInicial = document.getElementById('fechaInicial');
            const fechaFinal = document.getElementById('fechaFinal');

            if (fechaInicial && fechaInicial.value) {
                params.set('fechaInicial', fechaInicial.value);
            }

            if (fechaFinal && fechaFinal.value) {
                params.set('fechaFinal', fechaFinal.value);
            }

            if (selectMedico && selectMedico.value) {
                params.set('cedulaMedico', selectMedico.value);
            }

            if (selectServicio && selectServicio.value) {
                params.set('codServicio', selectServicio.value);
            }

            if (selectDependencia && selectDependencia.value) {
                params.set('codDependencia', selectDependencia.value);
            }

            const estadoSelect = document.getElementById('Estado');
            if (estadoSelect && estadoSelect.value) {
                params.set('estado', estadoSelect.value);
            }

            if (tipoReporteInput && tipoReporteInput.value) {
                params.set('tipoReporte', tipoReporteInput.value);
            }

            window.location.href = `${exportUrl}?${params.toString()}`;
        });
    }

    function reloadMedicos() {
        if (!selectMedico) {
            return;
        }

        const medicosUrl = selectMedico.dataset.medicosUrl;
        const empresas = getEmpresasSeleccionadas();

        resetMedicosSelect();

        if (empresas.length === 0 || !medicosUrl) {
            return;
        }

        const requests = empresas.map(function (empresa) {
            return fetch(`${medicosUrl}?empresa=${encodeURIComponent(empresa)}`)
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Error al cargar médicos');
                    }

                    return response.json();
                });
        });

        Promise.all(requests)
            .then(function (results) {
                const medicosMap = new Map();

                results.flat().forEach(function (medico) {
                    if (!medicosMap.has(medico.cedula)) {
                        medicosMap.set(medico.cedula, medico);
                    }
                });

                medicosMap.forEach(function (medico) {
                    const option = document.createElement('option');
                    option.value = medico.cedula;
                    option.textContent = `${medico.nombre} (${medico.cedula})`;
                    selectMedico.appendChild(option);
                });
            })
            .catch(function () {
                resetMedicosSelect();
            });
    }

    function getEmpresasSeleccionadas() {
        return Array.from(empresaCheckboxes)
            .filter(function (checkbox) { return checkbox.checked; })
            .map(function (checkbox) { return checkbox.value; });
    }

    function resetServiciosSelect() {
        if (!selectServicio) {
            return;
        }

        selectServicio.innerHTML = '';
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '-- Todos --';
        selectServicio.appendChild(defaultOption);
    }

    function resetMedicosSelect() {
        if (!selectMedico) {
            return;
        }

        selectMedico.innerHTML = '';
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '-- Todos --';
        selectMedico.appendChild(defaultOption);
    }
});
