window.mdwOauth = (function () {
    let _dotnetRef = null;


    function centerSpecs(w, h) {
        const dualScreenLeft = window.screenLeft !== undefined ? window.screenLeft : screen.left;
        const dualScreenTop = window.screenTop !== undefined ? window.screenTop : screen.top;
        const width = window.innerWidth || document.documentElement.clientWidth || screen.width;
        const height = window.innerHeight || document.documentElement.clientHeight || screen.height;
        const left = ((width / 2) - (w / 2)) + dualScreenLeft;
        const top = ((height / 2) - (h / 2)) + dualScreenTop;
        return `scrollbars=yes, width=${w}, height=${h}, top=${top}, left=${left}`;
    }


    function openPopup(url, name, w, h) {
        const specs = centerSpecs(w || 680, h || 760);
        const win = window.open(url, name || 'mdw-oauth', specs);
        if (!win) return false; // blocked
        try { win.focus(); } catch { }
        return true;
    }


    function registerReceiver(dotnetRef) {
        _dotnetRef = dotnetRef;
        window.addEventListener('message', (ev) => {
            // Only accept from same origin callback page
            if (ev.origin !== window.location.origin) return;
            const data = ev.data || {};
            if (data && data.type === 'mdw-oauth-callback' && _dotnetRef) {
                try {
                    _dotnetRef.invokeMethodAsync('OnAuthCallback', JSON.stringify(data));
                } catch (e) { console.error(e); }
            }
        });
    }


    return { openPopup, registerReceiver };
})();