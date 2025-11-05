export async function downloadWithAuth(url, fileName, token) {
    const res = await fetch(url, {
        headers: {
            'Authorization': 'Bearer ' + token,
            'Accept': '*/*'
        },
        credentials: 'include' // เผื่อมี cookie อื่น ๆ
    });
    if (!res.ok) {
        const txt = await res.text().catch(() => '');
        throw new Error(`Download failed: ${res.status} ${res.statusText} — ${txt}`);
    }

    // ใช้ชื่อไฟล์จาก header ถ้ามี
    let name = fileName || '';
    const cd = res.headers.get('content-disposition');
    if (!name && cd) {
        const m = /filename\*?=(?:UTF-8'')?["']?([^;"']+)/i.exec(cd);
        if (m) name = decodeURIComponent(m[1].replace(/["']/g, ''));
    }

    const blob = await res.blob();
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = name || 'download';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 3000);
}
