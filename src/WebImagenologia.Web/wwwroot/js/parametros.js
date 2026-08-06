(function () {
    'use strict';

    const selectDependencia = document.getElementById('selectDependencia');
    const nombreDependencia = document.getElementById('nombreDependencia');

    // --- Modal Editar ---
    const editModalElement = document.getElementById('modalEditarEstudio');
    const editButtons = document.querySelectorAll('.btn-editar-estudio');

    function bindEditModal() {
        if (!editModalElement) return;

        const modal = bootstrap.Modal.getOrCreateInstance(editModalElement);

        editButtons.forEach((button) => {
            button.addEventListener('click', () => {
                const empresa = button.dataset.empresa ?? '';
                const nombreEmpresa = button.dataset.nombreEmpresa ?? '';
                const codDep = button.dataset.codDependencia ?? '';
                const nombreDep = button.dataset.nombreDependencia ?? '';
                const cantidad = button.dataset.cantidad ?? '1';
                const esLectura = button.dataset.esLectura === 'true';

                document.getElementById('editEmpresa').value = empresa;
                document.getElementById('editEmpresasSel').value = empresa;
                document.getElementById('editCodDependencia').value = codDep;
                document.getElementById('editNombreDependencia').value = nombreDep;
                document.getElementById('editNombreEmpresa').value = nombreEmpresa;
                document.getElementById('editNombreDependenciaDisplay').value = nombreDep;
                document.getElementById('editCantidad').value = cantidad;
                document.getElementById('editEsLectura').checked = esLectura;

                modal.show();
            });
        });
    }

    // --- Modal Eliminar ---
    const deleteModalElement = document.getElementById('modalEliminarEstudio');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-estudio');
    const empresaEliminar = document.getElementById('empresaEliminar');
    const codDependenciaEliminar = document.getElementById('codDependenciaEliminar');
    const codServicioEliminar = document.getElementById('codServicioEliminar');
    const mensajeEliminar = document.getElementById('mensajeEliminarEstudio');

    function bindDeleteModal() {
        if (!deleteModalElement || !empresaEliminar || !codDependenciaEliminar || !mensajeEliminar) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(deleteModalElement);

        deleteButtons.forEach((button) => {
            button.addEventListener('click', () => {
                empresaEliminar.value = button.dataset.empresa ?? '';
                codDependenciaEliminar.value = button.dataset.codDependencia ?? '';
                if (codServicioEliminar) {
                    codServicioEliminar.value = button.dataset.codServicio ?? button.dataset.codDependencia ?? '';
                }

                const descripcion = button.dataset.descripcion ?? '';
                mensajeEliminar.textContent = `¿Desea eliminar el estudio ${descripcion}?`;
                modal.show();
            });
        });
    }

    // --- Sync nombre dependencia en formulario registro ---
    function syncNombreDependencia() {
        if (!selectDependencia || !nombreDependencia) {
            return;
        }

        const selectedOption = selectDependencia.options[selectDependencia.selectedIndex];
        nombreDependencia.value = selectedOption?.dataset.nombre ?? '';
    }

    if (selectDependencia) {
        selectDependencia.addEventListener('change', syncNombreDependencia);
        syncNombreDependencia();
    }

    bindEditModal();
    bindDeleteModal();
})();
