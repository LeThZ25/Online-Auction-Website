(function () {
    function update() {
        document.querySelectorAll('[data-countdown]').forEach(el => {
            const end = new Date(el.getAttribute('data-countdown'));
            const now = new Date();
            const diff = end - now;
            if (isNaN(end)) return;
            if (diff <= 0) { el.textContent = 'Hết hạn'; return; }
            const d = Math.floor(diff / 86400000);
            const h = Math.floor((diff % 86400000) / 3600000);
            const m = Math.floor((diff % 3600000) / 60000);
            el.textContent = (d > 0 ? d + ' ngày ' : '') + `${h} giờ ${m} phút`;
        });
    }
    update();
    setInterval(update, 60000);
})();