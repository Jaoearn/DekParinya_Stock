/* DekParinya Stock — Theme switcher (light / dark)
   โหลดใน <head> เพื่อกำหนดธีมก่อน render (ป้องกันจอกระพริบ) และจำค่าที่เลือกไว้ */
(function () {
    'use strict';
    var KEY = 'dekparinya-theme';

    function getStored() {
        try { return localStorage.getItem(KEY); } catch (e) { return null; }
    }
    function store(v) {
        try { localStorage.setItem(KEY, v); } catch (e) { /* ignore */ }
    }
    function preferred() {
        var s = getStored();
        if (s === 'light' || s === 'dark') return s;
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    function apply(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        var icons = document.querySelectorAll('[data-theme-toggle] i');
        icons.forEach(function (i) {
            i.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
        document.dispatchEvent(new CustomEvent('themechange', { detail: { theme: theme } }));
    }

    apply(preferred());

    // สถานะเมนู sidebar (หุบ/เปิด) — ใส่ก่อน render เพื่อไม่ให้จอกระพริบ
    try {
        if (localStorage.getItem('dekparinya-sidebar') === 'collapsed')
            document.documentElement.setAttribute('data-sidebar', 'collapsed');
    } catch (e) { /* ignore */ }

    window.DekTheme = {
        get: function () { return document.documentElement.getAttribute('data-bs-theme') || 'light'; },
        set: function (t) { store(t); apply(t); },
        toggle: function () { this.set(this.get() === 'dark' ? 'light' : 'dark'); }
    };

    document.addEventListener('DOMContentLoaded', function () {
        apply(window.DekTheme.get());
        document.querySelectorAll('[data-theme-toggle]').forEach(function (btn) {
            btn.addEventListener('click', function () { window.DekTheme.toggle(); });
        });
    });
})();
