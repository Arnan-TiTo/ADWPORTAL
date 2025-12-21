window.miniqr = (function () {
    let scanner, videoEl, cameras = [], idx = 0, dotnetRef = null;

    async function ensureLib() {
        if (!window.QrScanner) {
            await new Promise(r => {
                const s = document.createElement('script');
                s.src = "/js/qr-scanner.umd.min.js";   // ใช้ไฟล์โลคัล
                s.onload = r; document.head.appendChild(s);
            });
            window.QrScanner.WORKER_PATH = "/js/qr-scanner-worker.min.js";
        }
    }

    async function open(ref, elOrId) {
        await ensureLib();
        dotnetRef = ref;

        //  รองรับทั้ง ElementReference (.value) และ id/string
        videoEl = elOrId?.tagName ? elOrId :
            (elOrId?.value?.tagName ? elOrId.value :
                document.getElementById(elOrId));

        if (!videoEl) {
            console.error('miniqr.open: video element not found');
            return;
        }

        if (!scanner) {
            scanner = new QrScanner(videoEl, res => {
                const text = typeof res === 'string' ? res : (res?.data || '');
                if (text && dotnetRef) dotnetRef.invokeMethodAsync('QrDecoded', text);
            }, { returnDetailedScanResult: true, highlightScanRegion: true, highlightCodeOutline: true });
            scanner.setInversionMode('both');
        }

        const cams = await QrScanner.listCameras(true);
        cameras = cams || [];
        if (cameras.length) {
            const back = cameras.findIndex(c => /back|rear|environment/i.test(c.label));
            idx = back >= 0 ? back : 0;
            await scanner.setCamera(cameras[idx].id);
        }
        await scanner.start();
    }

    async function switchCam() {
        if (!scanner || !cameras.length) return;
        idx = (idx + 1) % cameras.length;
        await scanner.setCamera(cameras[idx].id);
    }

    async function choose(inputId) {
        await ensureLib();
        const input = document.getElementById(inputId);
        input.onchange = async (e) => {
            const f = e.target.files?.[0]; if (!f) return;
            try {
                const res = await QrScanner.scanImage(f, { returnDetailedScanResult: true, inversionAttempts: 'attemptBoth' });
                const text = typeof res === 'string' ? res : (res?.data || '');
                if (text && dotnetRef) dotnetRef.invokeMethodAsync('QrDecoded', text);
            } finally { input.value = ''; }
        };
        input.click();
    }

    async function close() { try { await scanner?.stop(); } catch { } }

    return { open, switch: switchCam, choose, close };
})();
