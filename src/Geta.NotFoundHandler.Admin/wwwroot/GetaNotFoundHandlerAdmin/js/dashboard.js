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

    function addSortableHeaders() {
        var buttons = document.querySelectorAll('form .sortable-header a');
        buttons.forEach(function (button) {
            button.addEventListener('click',
                function (e) {
                    e.preventDefault();
                    var header = button.closest(".sortable-header");
                    var form = header.closest("form");
                    var sortBy = form.querySelector("input[name='sort-by']");
                    var sortDirection = form.querySelector("input[name='sort-direction']");
                    if (sortBy) sortBy.value = header.dataset.sortBy;
                    if (sortDirection) sortDirection.value = header.dataset.sortDirection;
                    form.submit();
                });
        });
    }

    clearInput();
    confirmSubmit();
    addSortableHeaders();
})()