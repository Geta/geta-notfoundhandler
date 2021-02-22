/* globals feather:false */

(function() {
    'use strict';

    feather.replace();


    function clearInput() {
        var initiators = document.querySelectorAll('[data-clear]');
        initiators.forEach(function(initiator) {
            initiator.addEventListener('click',
                function(e) {
                    var target = e.currentTarget;
                    var selector = target.getAttribute('data-clear');
                    var input = document.querySelector(selector);
                    input.value = '';
                });
        });
    }

    function confirmSubmit() {
        var initiators = document.querySelectorAll('[data-confirm]');
        initiators.forEach(function(initiator) {
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

    clearInput();
    confirmSubmit();
})()