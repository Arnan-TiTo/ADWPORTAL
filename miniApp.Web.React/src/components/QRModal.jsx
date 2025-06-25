import React, { useEffect, useState } from 'react';
import axios from 'axios';
import.meta.env.VITE_API_BASE_URL;

function QRModal({ onClose, onSuccess }) {
    const [qrToken, setQrToken] = useState('');
    const [error, setError] = useState('');

    useEffect(() => {
        const fetchToken = async () => {
            try {
                const res = await axios.get(`${import.meta.env.VITE_API_BASE_URL}/api/qrlogin/generate`);
                setQrToken(res.data.qrToken);
            } catch (err) {
                setError('ไม่สามารถสร้าง QR Token ได้');
            }
        };

        fetchToken();
    }, []);

    useEffect(() => {
        if (!qrToken) return;
        const interval = setInterval(async () => {
            try {
                const res = await axios.post(`${import.meta.env.VITE_API_BASE_URL}/api/qrlogin/scan`, { token: qrToken });
                if (res.data.token) {
                    localStorage.setItem('token', res.data.token);
                    onSuccess();
                    clearInterval(interval);
                }
            } catch { }
        }, 3000);
        return () => clearInterval(interval);
    }, [qrToken, onSuccess]);

    return (
        <div className="fixed inset-0 bg-black bg-opacity-30 flex items-center justify-center z-50">
            <div className="bg-white p-6 rounded shadow w-80 text-center">
                <h3 className="text-xl font-bold mb-4">สแกน QR เพื่อเข้าสู่ระบบ</h3>
                {error && <p className="text-red-500">{error}</p>}
                {qrToken && (
                    <img
                        src={`https://api.qrserver.com/v1/create-qr-code/?data=${qrToken}&size=200x200`}
                        alt="QR Login"
                        className="mx-auto"
                    />
                )}
                <button className="mt-4 text-blue-500 underline" onClick={onClose}>
                    ยกเลิก
                </button>
            </div>
        </div>
    );
}

export default QRModal;
