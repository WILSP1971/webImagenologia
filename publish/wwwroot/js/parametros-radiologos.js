(function () {
    'use strict';

    const selectMedico = document.getElementById('selectMedico');
    const nombreMedico = document.getElementById('nombreMedico');
    const nombreEmpresa = document.getElementById('nombreEmpresa');
    const empresaCheckboxes = document.querySelectorAll('.empresa-checkbox');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-radiologo');
    const modalElement = document.getElementById('modalEliminarRadiologo');
    const cedulaEliminar = document.getElementById('cedulaEliminar');
    const mensajeEliminar = document.getElementById('mensajeEliminarRadiologo');

    function syncNombreMedico() {
        if (!selectMedico || !nombreMedico) {
            return;
        }

        const selectedOption = selectMedico.options[selectMedico.selectedIndex];
        nombreMedico.value = selectedOption?.dataset.nombre ?? '';
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
        if (!modalElement || !cedulaEliminar || !mensajeEliminar) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

        deleteButtons.forEach((button) => {
            button.addEventListener('click', () => {
                const cedula = button.dataset.cedula ?? '';
                const nombre = button.dataset.nombre ?? '';
                cedulaEliminar.value = cedula;
                mensajeEliminar.textContent = `¿Desea eliminar al radiólogo ${nombre} (${cedula})?`;
                modal.show();
            });
        });
    }

    if (selectMedico) {
        selectMedico.addEventListener('change', syncNombreMedico);
        syncNombreMedico();
    }

    empresaCheckboxes.forEach((checkbox) => {
        checkbox.addEventListener('change', syncNombreEmpresa);
    });

    syncNombreEmpresa();
    bindDeleteModal();
})();
