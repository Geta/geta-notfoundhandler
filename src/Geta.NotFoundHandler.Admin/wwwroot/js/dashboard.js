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

    clearInput();
})()