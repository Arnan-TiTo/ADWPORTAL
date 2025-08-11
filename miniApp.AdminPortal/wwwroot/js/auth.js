window.miniapp = window.miniapp || {};

window.miniapp.setSession = async function (payload) {
    try {
        const res = await fetch('/auth/set-session', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
            credentials: 'same-origin'
        });
        return res.ok;
    } catch { return false; }
};

window.miniapp.logout = async function () {
    try {
        await fetch('/auth/logout', {
            method: 'POST',
            credentials: 'same-origin'   // สำคัญ: ให้ส่ง/รับคุกกี้
        });
    } catch (e) {
        console.error('logout failed', e);
    }
    // redirect หลังคุกกี้ถูกลบแล้ว
    location.replace('/login');
};
