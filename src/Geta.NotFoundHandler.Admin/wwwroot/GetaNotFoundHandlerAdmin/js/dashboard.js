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

    function addFormTriggers() {
        var form = document.getElementById("tableQueryState");
        if (!form) { return; }

        var buttons = form.parentElement.querySelectorAll('.sortable-header button');
        buttons.forEach(function (button) {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                var header = button.closest(".sortable-header");
                var sortBy = form.querySelector("input[name='sort-by']");
                var sortDirection = form.querySelector("input[name='sort-direction']");
                if (sortBy) sortBy.value = header.dataset.sortBy;
                if (sortDirection) sortDirection.value = header.dataset.sortDirection;
                form.submit();
            });
        });

        var pageLinks = form.parentElement.querySelectorAll('.page-link[name="page"]');
        pageLinks.forEach(function (pageLink) {
            pageLink.addEventListener('click', function (e) {
                e.preventDefault();
                var page = form.querySelector("input[name='page']");
                if (page) page.value = pageLink.value;
                form.submit();
            });
        });

        var searchButton = form.querySelector(".search-button");
        if (searchButton) {
            var page = form.querySelector("input[name='page']");
            if (page) {
                page.value = 1;
            }
        }
    }

    clearInput();
    confirmSubmit();
    addFormTriggers();
})()
