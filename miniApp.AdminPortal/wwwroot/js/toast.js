function ensureContainer() {
    let c = document.getElementById('toast-container');
    if (!c) {
        c = document.createElement('div');
        c.id = 'toast-container';
        c.style.position = 'fixed';
        c.style.top = '1rem';
        c.style.right = '1rem';
        c.style.zIndex = '1080';
        c.style.display = 'flex';
        c.style.flexDirection = 'column';

        c.style.gap = '.5rem';
        document.body.appendChild(c);
    }
    return c;
}

window.showToast = (message, type = 'info', timeout = 3000) => {
    const container = ensureContainer();
    const el = document.createElement('div');
    el.className = 'vc-toast';
    el.innerHTML = `
    <div class="vc-toast-body">${message}</div>
    <button class="vc-toast-close" aria-label="Close">&times;</button>
  `;

    let bg = '#0d6efd', color = '#fff';
    if (type === 'success') bg = '#198754';
    else if (type === 'error') bg = '#dc3545';
    else if (type === 'warning') { bg = '#ffc107'; color = '#222'; }

    Object.assign(el.style, {
        background: bg, color, padding: '.75rem 1rem',
        borderRadius: '.5rem', boxShadow: '0 6px 20px rgba(0,0,0,0.15)',
        display: 'flex', alignItems: 'center', gap: '.75rem',
        opacity: '0', transform: 'translateY(-6px)',
        transition: 'opacity .18s ease, transform .18s ease'
    });
    const closeBtn = el.querySelector('.vc-toast-close');
    Object.assign(closeBtn.style, {
        background: 'transparent', border: '0', color: 'inherit',
        fontSize: '1.1rem', lineHeight: '1', cursor: 'pointer'
    });

    container.appendChild(el);

    const close = () => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(-6px)';
        el.addEventListener('transitionend', () => el.remove(), { once: true });
    };
    closeBtn.addEventListener('click', close);

    requestAnimationFrame(() => {
        el.style.opacity = '1';
        el.style.transform = 'translateY(0)';
    });

    setTimeout(close, timeout);
};

window.confirmToast = (message, okText = 'OK', cancelText = 'Cancel', type = 'warning') => {
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        Object.assign(overlay.style, {
            position: 'fixed', inset: '0', background: 'rgba(0,0,0,.35)',
            display: 'grid', placeItems: 'center', zIndex: 2000
        });

        const card = document.createElement('div');
        let bg = '#ffc107', color = '#222';
        if (type === 'success') { bg = '#198754'; color = '#fff'; }
        if (type === 'error') { bg = '#dc3545'; color = '#fff'; }
        if (type === 'info') { bg = '#0d6efd'; color = '#fff'; }

        Object.assign(card.style, {
            width: 'min(92vw, 420px)', background: '#fff', color: '#222',
            borderRadius: '12px', boxShadow: '0 18px 60px rgba(0,0,0,.25)',
            overflow: 'hidden', transform: 'translateY(-8px)', opacity: 0,
            transition: 'opacity .18s ease, transform .18s ease'
        });

        const bar = document.createElement('div');
        Object.assign(bar.style, { height: '6px', background: bg });
        card.appendChild(bar);

        const body = document.createElement('div');
        Object.assign(body.style, { padding: '16px 18px', fontSize: '15px' });
        body.textContent = message;
        card.appendChild(body);

        const actions = document.createElement('div');
        Object.assign(actions.style, { display: 'flex', gap: '8px', justifyContent: 'flex-end', padding: '0 18px 16px' });

        const btnCancel = document.createElement('button');
        btnCancel.textContent = cancelText;
        Object.assign(btnCancel.style, {
            padding: '8px 14px', borderRadius: '8px', border: '1px solid #ced4da',
            background: '#fff', cursor: 'pointer'
        });

        const btnOk = document.createElement('button');
        btnOk.textContent = okText;
        Object.assign(btnOk.style, {
            padding: '8px 14px', borderRadius: '8px', border: '0',
            background: bg, color, cursor: 'pointer'
        });

        actions.append(btnCancel, btnOk);
        card.appendChild(actions);
        overlay.appendChild(card);
        document.body.appendChild(overlay);

        requestAnimationFrame(() => { card.style.opacity = 1; card.style.transform = 'translateY(0)'; });

        const close = (val) => {
            card.style.opacity = 0; card.style.transform = 'translateY(-8px)';
            card.addEventListener('transitionend', () => overlay.remove(), { once: true });
            resolve(val);
        };

        btnCancel.addEventListener('click', () => close(false));
        btnOk.addEventListener('click', () => close(true));
        overlay.addEventListener('click', (e) => { if (e.target === overlay) close(false); });
        window.addEventListener('keydown', function onKey(ev) {
            if (ev.key === 'Escape') { window.removeEventListener('keydown', onKey); close(false); }
            if (ev.key === 'Enter') { window.removeEventListener('keydown', onKey); close(true); }
        });
    });
};

