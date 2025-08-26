window.dashboard = (function () {
    const charts = {};

    function ensureChartJs(cb) {
        if (window.Chart) { cb(); return; }
        const s = document.createElement("script");
        s.src = "https://cdn.jsdelivr.net/npm/chart.js";
        s.onload = cb;
        document.head.appendChild(s);
    }

    function destroy(id) {
        if (charts[id]) { charts[id].destroy(); delete charts[id]; }
    }

    function fmtMoney(v) { try { return Number(v).toLocaleString(); } catch { return v; } }

    // height/size เป็นพิกเซล (ออปชัน)
    function renderBar(id, labels, data, title, height) {
        ensureChartJs(() => {
            const el = document.getElementById(id);
            if (!el) return;
            if (height) el.style.height = height + "px";         // สำคัญ!
            const ctx = el.getContext("2d");
            destroy(id);
            charts[id] = new Chart(ctx, {
                type: "bar",
                data: { labels, datasets: [{ data }] },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,                       // ใช้ความสูงที่เราตั้ง
                    indexAxis: "y",
                    plugins: {
                        legend: { display: false },
                        title: { display: !!title, text: title }
                    },
                    scales: {
                        x: { beginAtZero: true, ticks: { callback: v => fmtMoney(v) } }
                    }
                }
            });
        });
    }

    function renderPie(id, labels, data, title, size) {
        ensureChartJs(() => {
            const el = document.getElementById(id);
            if (!el) return;
            if (size) { el.style.width = size + "px"; el.style.height = size + "px"; } // จัตุรัส
            const ctx = el.getContext("2d");
            destroy(id);
            charts[id] = new Chart(ctx, {
                type: "pie",
                data: { labels, datasets: [{ data }] },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    aspectRatio: 1,
                    plugins: {
                        legend: { position: "bottom", align: "center" },
                        title: { display: !!title, text: title },
                        tooltip: { callbacks: { label: c => `${c.label}: ${fmtMoney(c.parsed)}` } }
                    }
                }
            });
        });
    }

    return { renderBar, renderPie };
})();
