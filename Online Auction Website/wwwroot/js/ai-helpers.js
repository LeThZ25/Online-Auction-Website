// Safe JSON helpers cho mọi fetch của AI (chat + gợi ý mô tả)
window.AI = (() => {
    function tryParseJson(s) { try { return JSON.parse(s); } catch { return null; } }

    async function fetchJsonSafe(url, options = {}) {
        const opt = { method: 'GET', ...options };
        opt.headers = { 'X-Requested-With': 'XMLHttpRequest', ...(opt.headers || {}) };

        // Nếu body là object JS thì tự JSON.stringify + header
        if (opt.body && typeof opt.body === 'object' && !(opt.body instanceof FormData)) {
            opt.headers['Content-Type'] = 'application/json';
            opt.body = JSON.stringify(opt.body);
        }

        // Anti-forgery (nếu controller dùng [ValidateAntiForgeryToken])
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token && !opt.headers['RequestVerificationToken']) {
            opt.headers['RequestVerificationToken'] = token;
        }

        const res = await fetch(url, opt);
        const ct = res.headers.get('content-type') || '';

        let data = null, text = '';
        if (ct.includes('application/json')) {
            data = await res.json().catch(() => null);
        } else {
            text = await res.text();
            data = tryParseJson(text); // lỡ server trả text nhưng thực ra là JSON
        }

        if (!res.ok) {
            const msg = (data && (data.error || data.message)) || text || `HTTP ${res.status}`;
            throw new Error(msg);
        }

        // Trả về object JSON (nếu có), nếu không thì bọc text lại
        return data ?? { text };
    }

    return { fetchJsonSafe };
})();