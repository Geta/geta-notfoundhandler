/* globals feather:false */

(function () {
    'use strict';

    feather.replace();

    function clearInput() {
        var initiators = document.querySelectorAll('[data-clear]');
        initiators.forEach(function (initiator) {
            initiator.addEventListener('click',
                function (e) {
                    var target = e.currentTarget;
                    var selector = target.getAttribute('data-clear');
                    var input = document.querySelector(selector);
                    input.value = '';
                });
        });
    }

    function confirmSubmit() {
        var initiators = document.querySelectorAll('[data-confirm]');
        initiators.forEach(function (initiator) {
            var form = initiator.form;
            form.addEventListener('submit',
                function (e) {
                    e.preventDefault();

                    var message = initiator.getAttribute('data-confirm');
                    if (confirm(message)) {
                        form.action = initiator.formAction;
                        form.submit();
                    }
                });
        });
    }

    function adjustModalPosition() {
        var modalTriggers = document.querySelectorAll('.modal-trigger[data-bs-target]');
        modalTriggers.forEach(function (modalTrigger) {
            modalTrigger.addEventListener('click', function () {
                var modalDialog = document.querySelector(modalTrigger.dataset.bsTarget + " .modal-dialog");
                if (!modalDialog) { return; }

                modalDialog.style = "position: fixed;" +
                    "top: " + modalTrigger.getBoundingClientRect().top + "px;" +
                    "left: 50%;" +
                    "min-width: 500px;" +
                    "transform: translate(-50%, -50%);";
            });
        });
    }

    clearInput();
    confirmSubmit();
    adjustModalPosition();
})()
