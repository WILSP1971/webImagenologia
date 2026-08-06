'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const selectEmpresa = document.getElementById('selectEmpresa');
    const selectMedico = document.getElementById('selectMedico');

    if (!selectEmpresa || !selectMedico) {
        return;
    }

    selectEmpresa.addEventListener('change', function () {
        const empresa = selectEmpresa.value;
        const medicosUrl = selectEmpresa.dataset.medicosUrl;

        if (!empresa || !medicosUrl) {
            resetMedicosSelect();
            return;
        }

        fetch(`${medicosUrl}?empresa=${encodeURIComponent(empresa)}`)
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Error al cargar médicos');
                }

                return response.json();
            })
            .then(function (medicos) {
                resetMedicosSelect();

                medicos.forEach(function (medico) {
                    const option = document.createElement('option');
                    option.value = medico.cedula;
                    option.textContent = `${medico.nombre} (${medico.cedula})`;
                    selectMedico.appendChild(option);
                });
            })
            .catch(function () {
                resetMedicosSelect();
            });
    });

    function resetMedicosSelect() {
        selectMedico.innerHTML = '';
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '-- Todos los radiólogos --';
        selectMedico.appendChild(defaultOption);
    }
});
