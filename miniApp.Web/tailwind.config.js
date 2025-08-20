/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Pages/**/*.{cshtml,html}",
        "./Pages/Shared/**/*.cshtml",
        "./wwwroot/**/*.js"
    ],
    theme: { extend: {} },
    plugins: [],
    safelist: [
        'bg-green-600', 'bg-red-600', 'bg-yellow-500', 'bg-blue-600', 'bg-gray-800'
    ]
};
