/* =====================================================================
   DekParinya Stock — site.js
   - SweetAlert2 defaults (confirmButtonColor: #E31936)
   - Flash messages (TempData)
   - Confirm forms / buttons ([data-confirm])
   - Approve / Reject dialogs ([data-doc-action])
   - initCharCounters: ตัวนับ "0/200" ในฟอร์ม POST
   - Line tables (เพิ่ม/ลบรายการสินค้า)
   - Global search suggest (/Search/Suggest) + Ctrl+K
   - Sidebar toggle (mobile)
   ===================================================================== */
(function () {
    'use strict';

    var BRAND_RED = '#E31936';

    // ---------- SweetAlert2 ----------
    var AppSwal = window.Swal ? Swal.mixin({
        confirmButtonColor: BRAND_RED,
        showCancelButton: false,     // ไม่มีปุ่ม "ยกเลิก" — ปิดด้วยปุ่ม X / Esc
        showCloseButton: true,
        buttonsStyling: true,
        reverseButtons: false
    }) : null;
    window.AppSwal = AppSwal;

    window.appAlert = function (icon, title, text) {
        if (!AppSwal) { alert(title + (text ? '\n' + text : '')); return Promise.resolve(); }
        return AppSwal.fire({ icon: icon, title: title, text: text || '', confirmButtonText: 'OK' });
    };

    window.appConfirm = function (title, text, confirmText, icon) {
        if (!AppSwal) return Promise.resolve(window.confirm(title));
        return AppSwal.fire({
            icon: icon || 'question',
            title: title,
            text: text || '',
            confirmButtonText: confirmText || 'Confirm',
            focusConfirm: true
        }).then(function (r) { return r.isConfirmed; });
    };

    function showFlash() {
        var el = document.getElementById('flash');
        if (!el || !AppSwal) return;
        var ok = el.getAttribute('data-success');
        var err = el.getAttribute('data-error');
        if (ok) {
            AppSwal.fire({ icon: 'success', title: 'สำเร็จ', text: ok, confirmButtonText: 'OK', timer: 2600, timerProgressBar: true });
        } else if (err) {
            AppSwal.fire({ icon: 'error', title: 'ไม่สามารถทำรายการได้', text: err, confirmButtonText: 'OK' });
        }
    }

    // ---------- Confirm (form[data-confirm] / button[data-confirm]) ----------
    function bindConfirms() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('button[data-confirm], input[type=submit][data-confirm]');
            if (!btn || btn.form == null) return;
            if (btn.dataset.confirmed === '1') { btn.dataset.confirmed = ''; return; }
            e.preventDefault();
            if (!btn.form.checkValidity()) { btn.form.reportValidity(); return; }
            window.appConfirm(btn.dataset.confirm, btn.dataset.confirmDetail, btn.dataset.confirmText || btn.textContent.trim())
                .then(function (ok) {
                    if (!ok) return;
                    btn.dataset.confirmed = '1';
                    if (btn.form.requestSubmit) { btn.form.requestSubmit(btn); btn.dataset.confirmed = ''; }
                    else btn.click();
                });
        });

        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form.matches('form[data-confirm]')) return;
            if (form.dataset.confirmed === '1') { form.dataset.confirmed = ''; return; }
            e.preventDefault();
            window.appConfirm(form.dataset.confirm, form.dataset.confirmDetail, form.dataset.confirmText)
                .then(function (ok) {
                    if (!ok) return;
                    form.dataset.confirmed = '1';
                    form.submit();
                });
        });
    }

    // ---------- Approve / Reject ----------
    function bindDocActions() {
        document.querySelectorAll('[data-doc-action]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var form = document.getElementById(btn.dataset.form);
                if (!form || !AppSwal) return;
                var isReject = btn.dataset.docAction === 'reject';
                AppSwal.fire({
                    icon: isReject ? 'warning' : 'question',
                    title: btn.dataset.title || (isReject ? 'Reject this document?' : 'Approve this document?'),
                    text: btn.dataset.text || '',
                    input: 'textarea',
                    inputLabel: isReject ? 'เหตุผลที่ไม่อนุมัติ *' : 'หมายเหตุ (ถ้ามี)',
                    inputPlaceholder: isReject ? 'ระบุเหตุผล...' : 'ระบุหมายเหตุ...',
                    inputAttributes: { maxlength: 200 },
                    confirmButtonText: isReject ? 'Reject' : 'Approve',
                    confirmButtonColor: isReject ? BRAND_RED : '#005978',
                    inputValidator: function (v) {
                        if (isReject && !String(v || '').trim()) return 'กรุณาระบุเหตุผลที่ไม่อนุมัติ';
                        if (String(v || '').length > 200) return 'ระบุได้ไม่เกิน 200 ตัวอักษร';
                        return null;
                    },
                    didOpen: function () {
                        var ta = Swal.getInput();
                        if (ta) attachCounter(ta, 200);
                    }
                }).then(function (r) {
                    if (!r.isConfirmed) return;
                    var remark = form.querySelector('input[name="remark"]');
                    if (remark) remark.value = r.value || '';
                    form.submit();
                });
            });
        });
    }

    // ---------- Character counters ----------
    function resolveMax(el) {
        var ml = parseInt(el.getAttribute('maxlength'), 10);
        if (ml > 0) return ml;
        var dv = parseInt(el.getAttribute('data-val-length-max'), 10);
        if (dv > 0) return dv;
        return 200;
    }

    function attachCounter(el, max) {
        if (el.dataset.counterReady === '1') return;
        el.dataset.counterReady = '1';
        el.setAttribute('maxlength', max);
        var counter = document.createElement('div');
        counter.className = 'char-counter';
        var anchor = el.closest('.input-group') || el;
        anchor.insertAdjacentElement('afterend', counter);
        var update = function () {
            var len = el.value.length;
            counter.textContent = len + '/' + max;
            counter.classList.toggle('near', len >= max * 0.9 && len < max);
            counter.classList.toggle('full', len >= max);
        };
        el.addEventListener('input', update);
        update();
    }

    function initCharCounters(root) {
        (root || document).querySelectorAll('form').forEach(function (form) {
            var method = (form.getAttribute('method') || 'get').toLowerCase();
            if (method !== 'post') return; // ไม่ใส่ตัวนับในฟอร์มค้นหา (GET)
            form.querySelectorAll('textarea, input[type="text"], input[type="email"], input[type="tel"], input[type="url"], input:not([type])')
                .forEach(function (el) {
                    if (el.type === 'hidden' || el.hasAttribute('data-no-counter') || el.closest('template')) return;
                    attachCounter(el, resolveMax(el));
                });
        });
    }
    window.initCharCounters = initCharCounters;

    // ---------- Line tables ----------
    function initLineTables() {
        document.querySelectorAll('[data-line-table]').forEach(function (table) {
            var tbody = table.querySelector('tbody');
            var tplId = table.getAttribute('data-template');
            var tpl = document.getElementById(tplId);
            var form = table.closest('form');
            var prefix = table.getAttribute('data-prefix') || 'Lines';

            function renumber() {
                var rows = tbody.querySelectorAll('tr.line-row');
                rows.forEach(function (row, idx) {
                    var no = row.querySelector('.line-no');
                    if (no) no.textContent = idx + 1;
                    row.querySelectorAll('[data-field]').forEach(function (input) {
                        input.name = prefix + '[' + idx + '].' + input.getAttribute('data-field');
                    });
                });
                var empty = tbody.querySelector('.line-empty');
                if (empty) empty.hidden = rows.length > 0;
                recalc();
            }

            function syncRow(row) {
                var sel = row.querySelector('select[data-field="ProductId"]');
                if (!sel) return;
                var opt = sel.options[sel.selectedIndex];
                var uom = row.querySelector('[data-field="Uom"]');
                var stock = row.querySelector('[data-stock-cell]');
                var price = row.querySelector('[data-field="UnitPrice"]');
                if (opt && opt.value) {
                    if (uom && !row.dataset.loaded) uom.value = opt.getAttribute('data-uom') || uom.value;
                    if (stock) stock.textContent = Number(opt.getAttribute('data-stock') || 0).toLocaleString('en-US');
                    if (price && !row.dataset.loaded) price.value = opt.getAttribute('data-price') || 0;
                } else if (stock) {
                    stock.textContent = '-';
                }
                row.dataset.loaded = '';
            }

            function recalc() {
                var totalQty = 0, totalAmt = 0;
                tbody.querySelectorAll('tr.line-row').forEach(function (row) {
                    var q = parseFloat((row.querySelector('[data-field="Qty"]') || {}).value) || 0;
                    var p = parseFloat((row.querySelector('[data-field="UnitPrice"]') || {}).value) || 0;
                    totalQty += q;
                    totalAmt += q * p;
                    var amt = row.querySelector('[data-amount]');
                    if (amt) amt.textContent = (q * p).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                });
                var fmt = function (n) { return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };
                var set = function (sel, val) { document.querySelectorAll(sel).forEach(function (e) { e.textContent = val; }); };
                var vat = Math.round(totalAmt * 0.07 * 100) / 100;
                set('[data-total-qty]', totalQty.toLocaleString('en-US'));
                set('[data-total-amount]', fmt(totalAmt));
                set('[data-total-vat]', fmt(vat));
                set('[data-total-grand]', fmt(totalAmt + vat));
                set('[data-line-count]', tbody.querySelectorAll('tr.line-row').length);
            }

            function addRow() {
                var clone = tpl.content.firstElementChild.cloneNode(true);
                var empty = tbody.querySelector('.line-empty');
                tbody.insertBefore(clone, empty || null);
                renumber();
                initCharCounters(form.parentElement);
                var first = clone.querySelector('select, input');
                if (first) first.focus();
            }

            table.addEventListener('change', function (e) {
                var row = e.target.closest('tr.line-row');
                if (row && e.target.matches('select[data-field="ProductId"]')) syncRow(row);
                recalc();
            });
            table.addEventListener('input', recalc);
            table.addEventListener('click', function (e) {
                var rm = e.target.closest('[data-remove-line]');
                if (!rm) return;
                rm.closest('tr').remove();
                renumber();
            });
            document.querySelectorAll('[data-add-line="' + table.id + '"]').forEach(function (b) {
                b.addEventListener('click', addRow);
            });

            tbody.querySelectorAll('tr.line-row').forEach(function (row) {
                row.dataset.loaded = '1';
                syncRow(row);
            });
            renumber();
        });
    }

    // ---------- Global search suggest ----------
    function initSearch() {
        var input = document.getElementById('globalSearch');
        var panel = document.getElementById('suggestPanel');
        if (!input || !panel) return;
        var timer = null, activeIdx = -1, lastQ = '', ctrl = null;

        function esc(s) {
            return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
                return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
            });
        }
        function close() { panel.classList.remove('show'); activeIdx = -1; }
        function items() { return panel.querySelectorAll('.suggest-item'); }
        function highlight(i) {
            var list = items();
            list.forEach(function (el, k) { el.classList.toggle('active', k === i); });
            if (list[i]) list[i].scrollIntoView({ block: 'nearest' });
            activeIdx = i;
        }
        function render(data, q) {
            if (!data.length) {
                panel.innerHTML = '<div class="suggest-empty"><i class="bi bi-search me-1"></i>ไม่พบผลลัพธ์สำหรับ "' + esc(q) + '"</div>';
            } else {
                panel.innerHTML = data.map(function (d) {
                    return '<a class="suggest-item" role="option" href="' + esc(d.url) + '">' +
                        '<span class="s-icon"><i class="bi ' + esc(d.icon) + '"></i></span>' +
                        '<span class="flex-grow-1 min-w-0"><div class="s-title text-truncate">' + esc(d.title) + '</div>' +
                        (d.subtitle ? '<div class="s-sub text-truncate">' + esc(d.subtitle) + '</div>' : '') + '</span>' +
                        '<span class="s-type">' + esc(d.type) + '</span></a>';
                }).join('') +
                    '<div class="suggest-footer"><kbd>↑</kbd> <kbd>↓</kbd> เลื่อน · <kbd>Enter</kbd> เปิด · กด Enter ที่ช่องค้นหาเพื่อดูผลทั้งหมด</div>';
            }
            panel.classList.add('show');
            activeIdx = -1;
        }
        function fetchSuggest(q) {
            if (ctrl) ctrl.abort();
            ctrl = window.AbortController ? new AbortController() : null;
            fetch('/Search/Suggest?q=' + encodeURIComponent(q), { signal: ctrl ? ctrl.signal : undefined, headers: { 'Accept': 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (data) { if (q === lastQ) render(data, q); })
                .catch(function () { /* aborted */ });
        }

        input.addEventListener('input', function () {
            var q = input.value.trim();
            lastQ = q;
            clearTimeout(timer);
            if (q.length < 1) { close(); return; }
            timer = setTimeout(function () { fetchSuggest(q); }, 180);
        });
        input.addEventListener('focus', function () { if (input.value.trim() && panel.innerHTML) panel.classList.add('show'); });
        input.addEventListener('keydown', function (e) {
            var list = items();
            if (e.key === 'ArrowDown' && list.length) { e.preventDefault(); highlight((activeIdx + 1) % list.length); }
            else if (e.key === 'ArrowUp' && list.length) { e.preventDefault(); highlight((activeIdx - 1 + list.length) % list.length); }
            else if (e.key === 'Enter' && activeIdx >= 0 && list[activeIdx]) { e.preventDefault(); window.location.href = list[activeIdx].href; }
            else if (e.key === 'Escape') { close(); input.blur(); }
        });
        document.addEventListener('click', function (e) { if (!e.target.closest('.global-search')) close(); });

        // Ctrl+K / Cmd+K เปิดช่องค้นหา
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                input.focus();
                input.select();
                if (input.value.trim()) fetchSuggest(lastQ = input.value.trim());
            }
        });
    }

    // ---------- Sidebar (mobile) ----------
    function initSidebar() {
        var root = document.documentElement;
        var isDesktop = function () { return window.matchMedia('(min-width: 992px)').matches; };
        var syncIcon = function () {
            var collapsed = isDesktop() ? root.getAttribute('data-sidebar') === 'collapsed' : !document.body.classList.contains('sidebar-open');
            document.querySelectorAll('[data-sidebar-toggle]').forEach(function (b) {
                b.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
                var i = b.querySelector('i');
                if (i) i.className = 'bi bi-list';
            });
        };
        var toggle = function () {
            if (isDesktop()) {
                var collapse = root.getAttribute('data-sidebar') !== 'collapsed';
                if (collapse) root.setAttribute('data-sidebar', 'collapsed'); else root.removeAttribute('data-sidebar');
                try { localStorage.setItem('dekparinya-sidebar', collapse ? 'collapsed' : 'open'); } catch (e) { /* ignore */ }
                window.dispatchEvent(new Event('resize')); // ให้กราฟปรับขนาดตาม
            } else {
                document.body.classList.toggle('sidebar-open');
            }
            syncIcon();
        };
        document.querySelectorAll('[data-sidebar-toggle]').forEach(function (b) { b.addEventListener('click', toggle); });
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && (e.key === 'b' || e.key === 'B') && !e.target.closest('input, textarea, select')) {
                e.preventDefault(); toggle();
            }
        });
        window.addEventListener('resize', syncIcon);
        syncIcon();
        document.querySelectorAll('[data-sidebar-close]').forEach(function (b) {
            b.addEventListener('click', function () { document.body.classList.remove('sidebar-open'); syncIcon(); });
        });
        document.querySelectorAll('.nav-toggle').forEach(function (b) {
            var target = document.querySelector(b.getAttribute('data-bs-target'));
            if (!target) return;
            target.addEventListener('show.bs.collapse', function () { b.classList.remove('collapsed'); b.classList.add('open'); });
            target.addEventListener('hide.bs.collapse', function () { b.classList.add('collapsed'); b.classList.remove('open'); });
        });
    }

    // ---------- Print ----------
    function initPrint() {
        document.querySelectorAll('[data-print]').forEach(function (b) {
            b.addEventListener('click', function () { window.print(); });
        });
    }

    // ---------- Image uploader ([data-image-uploader]) ----------
    function initImageUploaders() {
        document.querySelectorAll('[data-image-uploader]').forEach(function (box) {
            var input = box.querySelector('input[type=file]');
            var zone = box.querySelector('.drop-zone');
            var grid = box.querySelector('[data-preview]');
            var remainEl = box.querySelector('[data-remaining]');
            var maxFiles = parseInt(box.dataset.maxFiles, 10) || 10;
            var maxBytes = (parseFloat(box.dataset.maxMb) || 5) * 1024 * 1024;
            var okTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
            var submit = box.closest('form') ? box.closest('form').querySelector('[data-upload-submit]') : null;
            var files = [];
            if (!window.DataTransfer) return; // เบราว์เซอร์เก่า: ใช้ input ปกติ

            function sync() {
                var dt = new DataTransfer();
                files.forEach(function (f) { dt.items.add(f); });
                input.files = dt.files;
                if (remainEl) remainEl.textContent = Math.max(0, maxFiles - files.length);
                if (submit) submit.disabled = files.length === 0;
                box.classList.toggle('is-full', files.length >= maxFiles);
                render();
            }

            function fmt(b) { return b >= 1048576 ? (b / 1048576).toFixed(1) + ' MB' : Math.max(1, Math.round(b / 1024)) + ' KB'; }

            function render() {
                grid.querySelectorAll('img').forEach(function (i) { URL.revokeObjectURL(i.src); });
                grid.innerHTML = '';
                files.forEach(function (f, i) {
                    var item = document.createElement('div');
                    item.className = 'preview-item';
                    var img = document.createElement('img');
                    img.src = URL.createObjectURL(f);
                    img.alt = f.name;
                    var meta = document.createElement('div');
                    meta.className = 'preview-meta';
                    meta.innerHTML = '<span class="text-truncate"></span><span class="text-muted">' + fmt(f.size) + '</span>';
                    meta.firstChild.textContent = f.name;
                    var rm = document.createElement('button');
                    rm.type = 'button';
                    rm.className = 'preview-remove';
                    rm.title = 'Remove';
                    rm.innerHTML = '<i class="bi bi-x-lg"></i>';
                    rm.addEventListener('click', function () { files.splice(i, 1); sync(); });
                    item.appendChild(img); item.appendChild(meta); item.appendChild(rm);
                    grid.appendChild(item);
                });
            }

            function add(list) {
                var rejected = [];
                Array.prototype.forEach.call(list, function (f) {
                    if (okTypes.indexOf(f.type) < 0) { rejected.push(f.name + ' (ไม่ใช่ไฟล์รูปที่รองรับ)'); return; }
                    if (f.size > maxBytes) { rejected.push(f.name + ' (เกิน ' + box.dataset.maxMb + ' MB)'); return; }
                    if (files.length >= maxFiles) { rejected.push(f.name + ' (เกินจำนวนที่แนบได้)'); return; }
                    if (files.some(function (x) { return x.name === f.name && x.size === f.size; })) return;
                    files.push(f);
                });
                sync();
                if (rejected.length) window.appAlert('warning', 'บางไฟล์แนบไม่ได้', rejected.join('\n'));
            }

            input.addEventListener('change', function () {
                var picked = Array.prototype.slice.call(input.files);
                add(picked);
            });
            ['dragenter', 'dragover'].forEach(function (ev) {
                zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.add('dragging'); });
            });
            ['dragleave', 'drop'].forEach(function (ev) {
                zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.remove('dragging'); });
            });
            zone.addEventListener('drop', function (e) { if (e.dataTransfer) add(e.dataTransfer.files); });
            // วางรูปจาก clipboard (Ctrl+V)
            box.closest('form').addEventListener('paste', function (e) {
                if (!e.clipboardData) return;
                var imgs = Array.prototype.filter.call(e.clipboardData.files || [], function (f) { return f.type.indexOf('image/') === 0; });
                if (imgs.length) { e.preventDefault(); add(imgs); }
            });
            sync();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindConfirms();
        bindDocActions();
        initLineTables();
        initImageUploaders();
        initCharCounters(document);
        initSearch();
        initSidebar();
        initPrint();
        showFlash();
    });
})();
