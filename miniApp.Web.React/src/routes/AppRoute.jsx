import { Routes, Route } from 'react-router-dom';
import LoginPage from '../pages/LoginPage';

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            {/* เพิ่ม route อื่น ๆ ได้ที่นี่ */}
        </Routes>
    );
}
