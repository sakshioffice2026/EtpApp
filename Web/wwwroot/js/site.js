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

    function reindexStreams(body) {
        var rows = body.querySelectorAll('[data-stream-row]');
        Array.prototype.forEach.call(rows, function (row, i) {
            var inputs = row.querySelectorAll('input[name]');
            Array.prototype.forEach.call(inputs, function (input) {
                input.name = input.name.replace(/Streams\[\d+\]/, 'Streams[' + i + ']');
            });
        });
    }

    function initStreamRows() {
        document.addEventListener('click', function (e) {
            var add = e.target.closest('[data-stream-add]');
            if (add) {
                var body = document.querySelector('[data-stream-body]');
                if (!body) return;

                var index = body.querySelectorAll('[data-stream-row]').length;
                var url = add.getAttribute('data-url') + '?index=' + index;

                fetch(url, { credentials: 'same-origin' })
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        body.insertAdjacentHTML('beforeend', html);
                    });
                return;
            }

            var remove = e.target.closest('[data-stream-remove]');
            if (remove) {
                var tbody = remove.closest('[data-stream-body]');
                var row = remove.closest('[data-stream-row]');
                if (!tbody || !row) return;

                row.parentNode.removeChild(row);
                reindexStreams(tbody);
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initTopbar();
        initFlash();
        initConfirm();
        initStreamRows();
    });
})();
