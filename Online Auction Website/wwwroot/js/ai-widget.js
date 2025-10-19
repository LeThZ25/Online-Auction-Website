// wwwroot/js/ai-widget.js
(function () {
    function ready(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    function getAntiXsrf() {
        return (
            document.querySelector('meta[name="RequestVerificationToken"]')?.content ||
            document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        );
    }

    function escapeHtml(s) { return (s || '').replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c])); }

    function appendBubble(text, who = 'ai') {
        const box = document.getElementById('aiChatBody');
        if (!box) return;
        const div = document.createElement('div');
        div.className = `bubble bubble-${who}`;
        // Giữ xuống dòng, chống XSS
        div.innerHTML = escapeHtml(text).replace(/\n/g, '<br>');
        box.appendChild(div);
        box.scrollTop = box.scrollHeight;
    }

    async function sendAiMessage(msg) {
        const res = await fetch('/ai/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiXsrf()
            },
            body: JSON.stringify({ message: msg })
        });

        // Đọc body CHỈ 1 LẦN an toàn
        let data;
        const ct = res.headers.get('content-type') || '';
        if (ct.includes('application/json')) {
            data = await res.json();
        } else {
            const txt = await res.text();
            try { data = JSON.parse(txt); }
            catch { data = { ok: false, reply: txt || 'Lỗi không xác định.' }; }
        }
        appendBubble(data.reply || '(không có câu trả lời)', 'ai');
    }

    ready(function () {
        const fab = document.getElementById('aiFab');
        const panel = document.getElementById('aiPanel');
        const overlay = document.getElementById('aiOverlay');
        const close = document.getElementById('aiClose');
        const input = document.getElementById('aiInput');
        const send = document.getElementById('aiSendBtn');

        function openPanel() {
            if (!panel) return;
            panel.hidden = false;
            overlay.hidden = false;
            panel.classList.add('open');
            fab?.setAttribute('aria-expanded', 'true');
            // focus input
            setTimeout(() => input?.focus(), 50);
        }
        function closePanel() {
            if (!panel) return;
            panel.classList.remove('open');
            fab?.setAttribute('aria-expanded', 'false');
            // dùng timeout khớp transition để ẩn hẳn -> tránh overlay ăn click
            setTimeout(() => {
                panel.hidden = true;
                overlay.hidden = true;
            }, 180);
        }
        function togglePanel() {
            if (panel?.classList.contains('open')) closePanel(); else openPanel();
        }

        // Event binding an toàn (nếu phần tử chưa có, also no-op)
        fab?.addEventListener('click', (e) => { e.stopPropagation(); togglePanel(); });
        close?.addEventListener('click', (e) => { e.stopPropagation(); closePanel(); });
        overlay?.addEventListener('click', () => closePanel());

        // ESC để đóng
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && panel?.classList.contains('open')) closePanel();
        });

        // Gửi tin
        send?.addEventListener('click', async () => {
            const msg = (input?.value || '').trim();
            if (!msg) return;
            appendBubble(msg, 'me');
            input.value = '';
            try { await sendAiMessage(msg); }
            catch (err) { appendBubble('Lỗi: ' + (err?.message || err), 'sys'); }
        });

        // Enter để gửi, Shift+Enter xuống dòng
        input?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                send?.click();
            }
        });

        // Chips gợi ý
        document.querySelectorAll('.ai-suggest .chip').forEach(btn => {
            btn.addEventListener('click', () => {
                const q = btn.getAttribute('data-q') || btn.textContent.trim();
                if (!q) return;
                appendBubble(q, 'me');
                sendAiMessage(q).catch(err => appendBubble('Lỗi: ' + (err?.message || err), 'sys'));
            });
        });
    });
})();