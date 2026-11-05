/* =====================================================================
   DekParinya — Login: ร้านขายปุ๋ย & ดิน
   - คลิกถุง หรือ พิมพ์รหัส (A–C + 1–5) บน keypad แล้วกด OK → ถุงระเบิด → ~4 วินาทีเติมกลับเข้าช่อง
   - ปุ่ม LOGIN → ถุงทั้งหมดหล่นลงมาก่อน แล้วจึงส่งฟอร์มเข้าสู่ระบบ
   ===================================================================== */
(function () {
    'use strict';

    var RED = '#E31936';
    var REFILL_MS = 4000;
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    var lcd = document.querySelector('.lcd');
    var lcdMain = document.getElementById('lcdMain');
    var lcdSub = document.getElementById('lcdSub');
    var lcdCode = document.getElementById('lcdCode');
    var lcdMoney = document.getElementById('lcdMoney');
    var soldEl = document.getElementById('soldCount');
    var fx = document.getElementById('fxLayer');
    var shop = document.getElementById('shop');
    var form = document.getElementById('loginForm');
    var btnLogin = document.getElementById('btnLogin');

    var busy = {};
    var sold = 0, money = 0;
    var buffer = '';
    var loggingIn = false;
    var idleTimer = null;

    var Alert = window.Swal ? Swal.mixin({ confirmButtonColor: RED, showCloseButton: true, showCancelButton: false }) : null;

    function setLcd(main, sub, blink) {
        if (!lcd) return;
        lcdMain.textContent = main;
        if (sub != null) lcdSub.textContent = sub;
        if (blink) {
            lcd.classList.remove('blink');
            void lcd.offsetWidth;
            lcd.classList.add('blink');
        }
        clearTimeout(idleTimer);
        idleTimer = setTimeout(function () {
            if (loggingIn) return;
            setLcd('WELCOME', 'เลือกสินค้า หรือ เข้าสู่ระบบ');
            if (lcdCode) lcdCode.textContent = buffer || '--';
        }, 5000);
    }

    function slotByCode(code) { return document.querySelector('.slot[data-code="' + code + '"]'); }

    function markSelected(code) {
        document.querySelectorAll('.slot.selected').forEach(function (s) { s.classList.remove('selected'); });
        var s = code && code.length === 2 ? slotByCode(code) : null;
        if (s) s.classList.add('selected');
    }

    // ---------- FX ----------
    function centerOf(el) {
        var r = el.getBoundingClientRect();
        return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
    }

    function spawn(cls, x, y, styles) {
        var el = document.createElement('span');
        el.className = cls;
        el.style.left = x + 'px';
        el.style.top = y + 'px';
        for (var k in styles) el.style.setProperty(k, styles[k]);
        fx.appendChild(el);
        el.addEventListener('animationend', function () { el.remove(); });
        return el;
    }

    function explode(slot) {
        var sack = slot.querySelector('.sack');
        var p = centerOf(sack);
        var color = slot.dataset.color;
        var palette = [color, color, '#333F48', '#333F48', '#C4C6C7', '#FFFFFF', '#0090BA'];
        var count = reduceMotion ? 10 : 46;

        spawn('dust', p.x, p.y, {});
        spawn('shockwave', p.x, p.y, { '--c': color === '#C4C6C7' ? '#333F48' : color });
        for (var i = 0; i < count; i++) {
            var ang = Math.random() * Math.PI * 2;
            var dist = 60 + Math.random() * 160;
            var size = 4 + Math.random() * 9;
            spawn('particle', p.x, p.y, {
                '--dx': Math.cos(ang) * dist + 'px',
                '--dy': (Math.sin(ang) * dist + 40 + Math.random() * 60) + 'px',
                '--r': (Math.random() * 720 - 360) + 'deg',
                '--s': size + 'px',
                '--br': Math.random() > .5 ? '50%' : '2px',
                '--c': palette[i % palette.length],
                '--t': (0.8 + Math.random() * 0.8) + 's'
            });
        }
        var txt = spawn('flash-text', p.x, p.y - 30, {});
        txt.textContent = '+฿' + slot.dataset.price;
    }

    // ---------- Buy ----------
    function buy(code) {
        if (loggingIn) return;
        var slot = slotByCode(code);
        if (!slot) {
            setLcd('ERROR', 'ไม่พบรหัสสินค้า ' + code, true);
            return;
        }
        if (busy[code]) {
            setLcd('REFILLING', code + ' กำลังเติมสินค้า รอสักครู่', true);
            return;
        }
        busy[code] = true;
        var sack = slot.querySelector('.sack');
        if (lcdCode) lcdCode.textContent = code;
        setLcd(slot.dataset.label + ' ฿' + slot.dataset.price, slot.dataset.th + ' · กำลังจ่ายสินค้า...');

        sack.classList.remove('refill');
        sack.classList.add('shake');
        setTimeout(function () {
            sack.classList.remove('shake');
            sack.classList.add('boom');
            explode(slot);
            sold++;
            money += parseInt(slot.dataset.price, 10) || 0;
            if (soldEl) soldEl.textContent = sold;
            if (lcdMoney) lcdMoney.textContent = '฿' + money.toLocaleString('en-US');
            setLcd('THANK YOU!', 'ได้รับ ' + slot.dataset.label + ' แล้ว · กำลังเติมสินค้า', true);

            setTimeout(function () {
                sack.classList.remove('boom');
                sack.classList.add('gone');
                slot.classList.add('sold-out');
            }, 280);
        }, reduceMotion ? 50 : 350);

        // เติมกลับเข้าช่องหลัง ~4 วินาที
        setTimeout(function () {
            slot.classList.remove('sold-out');
            sack.classList.remove('gone', 'boom');
            void sack.offsetWidth;
            sack.classList.add('refill');
            busy[code] = false;
            setTimeout(function () { sack.classList.remove('refill'); }, 800);
        }, REFILL_MS);
    }

    document.querySelectorAll('.slot').forEach(function (slot) {
        slot.addEventListener('click', function () {
            buffer = '';
            markSelected(null);
            buy(slot.dataset.code);
        });
    });

    // ---------- Keypad ----------
    function press(key) {
        if (loggingIn) return;
        var btn = document.querySelector('.key[data-key="' + key + '"]');
        if (btn) {
            btn.classList.add('pressed');
            setTimeout(function () { btn.classList.remove('pressed'); }, 120);
        }
        if (key === 'CLR') {
            buffer = '';
            markSelected(null);
            if (lcdCode) lcdCode.textContent = '--';
            setLcd('CLEAR', 'พิมพ์รหัส เช่น A3 แล้วกด OK');
            return;
        }
        if (key === 'OK') {
            if (buffer.length !== 2) {
                setLcd('ERROR', 'กรุณาพิมพ์รหัสให้ครบ เช่น B2', true);
                return;
            }
            var code = buffer;
            buffer = '';
            markSelected(null);
            buy(code);
            return;
        }
        if (/^[ABC]$/.test(key)) {
            buffer = key;
        } else if (/^[1-5]$/.test(key)) {
            if (buffer.length === 1) buffer += key;
            else { setLcd('ERROR', 'เลือกแถว A, B หรือ C ก่อน', true); return; }
        }
        if (lcdCode) lcdCode.textContent = buffer || '--';
        markSelected(buffer);
        var s = buffer.length === 2 ? slotByCode(buffer) : null;
        if (s) setLcd(buffer + ' ' + s.dataset.label, s.dataset.th + ' · ฿' + s.dataset.price + ' · กด OK เพื่อซื้อ');
        else setLcd('CODE ' + buffer + '_', 'เลือกช่อง 1–5');
    }

    document.querySelectorAll('.key').forEach(function (k) {
        k.addEventListener('click', function () { press(k.dataset.key); });
    });

    // คีย์บอร์ดจริง (เฉพาะตอนไม่ได้พิมพ์ในช่อง input)
    document.addEventListener('keydown', function (e) {
        if (e.target.closest('input, textarea') || e.ctrlKey || e.metaKey || e.altKey) return;
        var k = e.key.toUpperCase();
        if (/^[ABC1-5]$/.test(k)) press(k);
        else if (e.key === 'Enter' && buffer) { e.preventDefault(); press('OK'); }
        else if (e.key === 'Backspace' || e.key === 'Escape') press('CLR');
    });

    // ---------- Password toggle ----------
    var pwToggle = document.getElementById('pwToggle');
    if (pwToggle) {
        pwToggle.addEventListener('click', function () {
            var pw = document.getElementById('Password');
            var show = pw.type === 'password';
            pw.type = show ? 'text' : 'password';
            pwToggle.innerHTML = show ? '<i class="bi bi-eye-slash"></i>' : '<i class="bi bi-eye"></i>';
        });
    }

    // ---------- LOGIN: ถุงหล่นลงมาก่อน แล้วจึงเข้าสู่ระบบ ----------
    function validate() {
        var email = document.getElementById('Email');
        var pw = document.getElementById('Password');
        var msg = null;
        email.classList.remove('input-validation-error');
        pw.classList.remove('input-validation-error');
        if (!email.value.trim()) { msg = 'กรุณาระบุอีเมล'; email.classList.add('input-validation-error'); }
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) { msg = 'รูปแบบอีเมลไม่ถูกต้อง'; email.classList.add('input-validation-error'); }
        else if (!pw.value) { msg = 'กรุณาระบุรหัสผ่าน'; pw.classList.add('input-validation-error'); }
        return msg;
    }

    form.addEventListener('submit', function (e) {
        if (form.dataset.go === '1') return;
        e.preventDefault();
        if (loggingIn) return;

        var err = validate();
        if (err) {
            setLcd('ERROR', err, true);
            if (Alert) Alert.fire({ icon: 'warning', title: 'ข้อมูลไม่ครบถ้วน', text: err, confirmButtonText: 'OK' });
            return;
        }

        loggingIn = true;
        btnLogin.disabled = true;
        btnLogin.innerHTML = '<i class="bi bi-hourglass-split"></i> LOGGING IN...';
        if (lcdCode) lcdCode.textContent = 'ID';
        setLcd('HELLO!', 'กำลังเข้าสู่ระบบ...', true);
        shop.classList.add('rumble');

        var maxEnd = 0;
        var vh = window.innerHeight;
        document.querySelectorAll('.slot .sack').forEach(function (sack) {
            if (sack.classList.contains('gone')) return;
            var r = sack.getBoundingClientRect();
            var delay = reduceMotion ? 0 : Math.random() * 0.55;
            var dur = reduceMotion ? 0.3 : 0.9 + Math.random() * 0.35;
            sack.style.setProperty('--fall', (vh - r.top + 120) + 'px');
            sack.style.setProperty('--rot', (Math.random() * 120 - 60) + 'deg');
            sack.style.setProperty('--delay', delay + 's');
            sack.style.setProperty('--fall-dur', dur + 's');
            sack.classList.remove('refill', 'shake');
            sack.classList.add('falling');
            maxEnd = Math.max(maxEnd, (delay + dur) * 1000);
        });

        setTimeout(function () {
            form.dataset.go = '1';
            form.submit();
        }, maxEnd ? maxEnd + 150 : 300);
    });

    // ---------- Server-side error ----------
    var serverError = (form.getAttribute('data-error') || '').trim();
    if (serverError) {
        if (lcdCode) lcdCode.textContent = 'ERR';
        setLcd('ACCESS ERROR', serverError, true);
        if (Alert) Alert.fire({ icon: 'error', title: 'เข้าสู่ระบบไม่สำเร็จ', text: serverError, confirmButtonText: 'OK' });
    }
})();
