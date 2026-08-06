(function () {
    'use strict';

    const selectOperador = document.getElementById('selectOperador');
    const nombreOperador = document.getElementById('nombreOperador');
    const empresaCheckboxes = document.querySelectorAll('.empresa-checkbox');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-operador');
    const modalElement = document.getElementById('modalEliminarOperador');
    const cedulaEliminar = document.getElementById('cedulaEliminar');
    const mensajeEliminar = document.getElementById('mensajeEliminarOperador');

    function syncNombreOperador() {
        if (!selectOperador || !nombreOperador) {
            return;
        }

        const selectedOption = selectOperador.options[selectOperador.selectedIndex];
        nombreOperador.value = selectedOption?.dataset.nombre ?? '';
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
                mensajeEliminar.textContent = `¿Desea eliminar al operador ${nombre} (${cedula})?`;
                modal.show();
            });
        });
    }

    if (selectOperador) {
        selectOperador.addEventListener('change', syncNombreOperador);
        syncNombreOperador();
    }

    bindDeleteModal();
})();
