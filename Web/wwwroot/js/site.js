(function () {
    'use strict';

    function initTopbar() {
        var bar = document.querySelector('.bp-topbar');
        if (!bar) return;

        function onScroll() {
            bar.classList.toggle('is-scrolled', window.scrollY > 4);
        }

        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    function hideToast(el) {
        if (!el || !el.parentNode) return;
        el.classList.add('is-hiding');
        window.setTimeout(function () {
            if (el.parentNode) el.parentNode.removeChild(el);
        }, 300);
    }

    function initFlash() {
        var auto = document.querySelectorAll('[data-bp-autohide]');
        Array.prototype.forEach.call(auto, function (el) {
            window.setTimeout(function () {
                hideToast(el);
            }, 6000);
        });

        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-bp-dismiss]');
            if (!btn) return;
            hideToast(btn.closest('.bp-toast'));
        });
    }

    function initConfirm() {
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form || !form.getAttribute) return;

            var message = form.getAttribute('data-confirm');
            if (message && !window.confirm(message)) {
                e.preventDefault();
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initTopbar();
        initFlash();
        initConfirm();
    });
})();
