import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import QRModal from '../components/QRModal';
import.meta.env.VITE_API_BASE_URL;


function LoginPage() {
    const navigate = useNavigate();
    const [form, setForm] = useState({ username: '', password: '' });
    const [error, setError] = useState('');
    const [showQR, setShowQR] = useState(false);

    const handleChange = (e) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        try {
            const res = await axios.post(`${import.meta.env.VITE_API_BASE_URL}/api/auth/login`, form);
            localStorage.setItem('token', res.data.token);
            navigate('/');
        } catch (err) {
            setError('Login failed. Please check your credentials.');
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-green-100">
            <form onSubmit={handleSubmit} className="bg-white p-8 rounded shadow w-96">
                <h2 className="text-2xl font-bold mb-4">Login</h2>
                <input
                    name="username"
                    type="text"
                    placeholder="Username"
                    onChange={handleChange}
                    className="w-full mb-3 p-2 border rounded"
                />
                <input
                    name="password"
                    type="password"
                    placeholder="Password"
                    onChange={handleChange}
                    className="w-full mb-3 p-2 border rounded"
                />
                {error && <p className="text-red-500 text-sm mb-3">{error}</p>}
                <button type="submit" className="w-full bg-green-500 text-white py-2 rounded">
                    Login
                </button>

                <button
                    type="button"
                    className="w-full mt-3 bg-gray-200 py-2 rounded"
                    onClick={() => setShowQR(true)}
                >
                    Login ด้วย QR
                </button>
            </form>

            {showQR && (
                <QRModal
                    onClose={() => setShowQR(false)}
                    onSuccess={() => {
                        setShowQR(false);
                        navigate('/');
                    }}
                />
            )}
        </div>
    );
}

export default LoginPage;
