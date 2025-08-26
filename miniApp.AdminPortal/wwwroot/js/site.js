(function () {
    window.showImage = function (src) {
        var img = document.getElementById('image-modal-img');
        var modalEl = document.getElementById('image-modal');
        if (!img || !modalEl || !window.bootstrap) {
            console.warn('Image modal or bootstrap not found');
            return;
        }
        img.src = src;

        currentScale = 1;
        offsetX = 0;
        offsetY = 0;
        img.style.transform = 'translate(0px,0px) scale(1)';

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    };

    var currentScale = 1, offsetX = 0, offsetY = 0, isDragging = false, startX = 0, startY = 0;
    function applyTransform() {
        var img = document.getElementById('image-modal-img');
        if (img) {
            img.style.transform = 'translate(' + offsetX + 'px,' + offsetY + 'px) scale(' + currentScale + ')';
        }
    }
    document.addEventListener('wheel', function (e) {
        var modalEl = document.getElementById('image-modal');
        if (!modalEl || !modalEl.classList.contains('show')) return;
        e.preventDefault();
        currentScale = Math.min(5, Math.max(1, currentScale + (e.deltaY < 0 ? 0.1 : -0.1)));
        applyTransform();
    }, { passive: false });

    document.addEventListener('mousedown', function (e) {
        var modalEl = document.getElementById('image-modal');
        if (!modalEl || !modalEl.classList.contains('show')) return;
        isDragging = true;
        var img = document.getElementById('image-modal-img');
        if (img) img.style.cursor = 'grabbing';
        startX = e.clientX - offsetX;
        startY = e.clientY - offsetY;
    });

    document.addEventListener('mousemove', function (e) {
        if (!isDragging) return;
        offsetX = e.clientX - startX;
        offsetY = e.clientY - startY;
        applyTransform();
    });

    document.addEventListener('mouseup', function () {
        isDragging = false;
        var img = document.getElementById('image-modal-img');
        if (img) img.style.cursor = 'grab';
    });

    window.downloadFile = function (fileName, mimeType, base64Data) {
        try {
            var link = document.createElement('a');
            link.href = 'data:' + (mimeType || 'text/csv') + ';base64,' + base64Data;
            link.download = fileName || 'download.csv';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (e) {
            console.error('downloadFile error', e);
        }
    };

    window.downloadFileFromUrl = async function (url, filename) {
        if (!url) return;
        try {
            const res = await fetch(url, { mode: 'cors' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const blob = await res.blob();
            const blobUrl = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = blobUrl;
            a.download = filename || 'download';
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(blobUrl);
        } catch (err) {
            console.error('downloadFileFromUrl', err);
            // fallback ให้ผู้ใช้เซฟเอง
            window.open(url, '_blank');
        }
    };

})();
