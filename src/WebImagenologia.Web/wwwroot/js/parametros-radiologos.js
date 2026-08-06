(function () {
    'use strict';

    const selectMedico = document.getElementById('selectMedico');
    const nombreMedico = document.getElementById('nombreMedico');
    const selectDependencia = document.getElementById('selectDependencia');
    const nombreDependencia = document.getElementById('nombreDependencia');
    const nombreEmpresa = document.getElementById('nombreEmpresa');
    const empresaCheckboxes = document.querySelectorAll('.empresa-checkbox');

    // --- Modal Editar ---
    const editModalElement = document.getElementById('modalEditarRadiologo');
    const editButtons = document.querySelectorAll('.btn-editar-radiologo');

    function bindEditModal() {
        if (!editModalElement) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(editModalElement);

        editButtons.forEach((button) => {
            button.addEventListener('click', () => {
                const empresa = button.dataset.empresa ?? '';
                const cedula = button.dataset.cedula ?? '';
                const nombre = button.dataset.nombre ?? '';
                const usuario = button.dataset.usuario ?? '';
                const codDep = button.dataset.codDependencia ?? '';
                const nombreDep = button.dataset.nombreDependencia ?? '';
                const cantidad = button.dataset.cantidad ?? '1';

                document.getElementById('editRadEmpresa').value = empresa;
                document.getElementById('editRadEmpresasSel').value = empresa;
                document.getElementById('editRadCedula').value = cedula;
                document.getElementById('editRadNombreMedico').value = nombre;
                document.getElementById('editRadCodDependencia').value = codDep;
                document.getElementById('editRadNombreDependencia').value = nombreDep;
                document.getElementById('editRadNombreMedicoDisplay').value =
                    nombre ? `${nombre} (${cedula})` : cedula;
                document.getElementById('editRadNombreDependenciaDisplay').value = nombreDep;
                document.getElementById('editRadUsuario').value = usuario;
                document.getElementById('editRadCantidad').value = cantidad;

                modal.show();
            });
        });
    }

    // --- Modal Eliminar ---
    const deleteModalElement = document.getElementById('modalEliminarRadiologo');
    const deleteButtons = document.querySelectorAll('.btn-eliminar-radiologo');
    const empresaEliminar = document.getElementById('empresaEliminarRad');
    const cedulaEliminar = document.getElementById('cedulaEliminarRad');
    const codDependenciaEliminar = document.getElementById('codDependenciaEliminarRad');
    const usuarioEliminar = document.getElementById('usuarioEliminarRad');
    const cantidadEliminar = document.getElementById('cantidadEliminarRad');
    const mensajeEliminar = document.getElementById('mensajeEliminarRadiologo');

    function bindDeleteModal() {
        if (!deleteModalElement || !cedulaEliminar || !mensajeEliminar) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(deleteModalElement);

        deleteButtons.forEach((button) => {
            button.addEventListener('click', () => {
                if (empresaEliminar) {
                    empresaEliminar.value = button.dataset.empresa ?? '';
                }
                cedulaEliminar.value = button.dataset.cedula ?? '';
                if (codDependenciaEliminar) {
                    codDependenciaEliminar.value = button.dataset.codDependencia ?? '';
                }
                if (usuarioEliminar) {
                    usuarioEliminar.value = button.dataset.usuario ?? '';
                }
                if (cantidadEliminar) {
                    cantidadEliminar.value = button.dataset.cantidad ?? '';
                }

                const nombre = button.dataset.nombre ?? '';
                const cedula = button.dataset.cedula ?? '';
                mensajeEliminar.textContent = `¿Desea eliminar al radiólogo ${nombre} (${cedula})?`;
                modal.show();
            });
        });
    }

    function syncNombreMedico() {
        if (!selectMedico || !nombreMedico) {
            return;
        }

        const selectedOption = selectMedico.options[selectMedico.selectedIndex];
        nombreMedico.value = selectedOption?.dataset.nombre ?? '';
    }

    function syncNombreDependencia() {
        if (!selectDependencia || !nombreDependencia) {
            return;
        }

        const selectedOption = selectDependencia.options[selectDependencia.selectedIndex];
        nombreDependencia.value = selectedOption?.dataset.nombre ?? '';
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

    if (selectMedico) {
        selectMedico.addEventListener('change', syncNombreMedico);
        syncNombreMedico();
    }

    if (selectDependencia) {
        selectDependencia.addEventListener('change', syncNombreDependencia);
        syncNombreDependencia();
    }

    empresaCheckboxes.forEach((checkbox) => {
        checkbox.addEventListener('change', syncNombreEmpresa);
    });

    syncNombreEmpresa();
    bindEditModal();
    bindDeleteModal();
})();
