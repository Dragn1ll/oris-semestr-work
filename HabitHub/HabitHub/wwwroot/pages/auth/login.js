document.getElementById('loginForm').addEventListener('submit', async function (e) {
    e.preventDefault();

    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value.trim();
    const error = document.getElementById('error');
    error.innerText = '';

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        error.innerText = 'Некорректный email';
        return;
    }
    if (password.length < 8) {
        error.innerText = 'Пароль должен содержать минимум 8 символов';
        return;
    }

    try {
        const res = await fetch('http://localhost:5000/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
            credentials: 'include'
        });

        const contentType = res.headers.get('Content-Type') || '';
        let result = {};

        if (contentType.includes('application/json')) {
            result = await res.json();
        } else {
            result = { message: await res.text() };
        }

        if (!res.ok) {
            let detail = 'Ошибка при входе';

            if (result.message) {
                try {
                    const inner = JSON.parse(result.message);
                    detail = inner.detail || inner.title || detail;
                } catch {
                    detail = result.message;
                }
            }

            throw new Error(detail);
        }

        if (result.token) {
            localStorage.setItem('jwt', result.token);
        }

        localStorage.setItem('userId', result.id || '');
        window.location.href = 'http://localhost:5000/main';
    } catch (err) {
        error.innerText = err.message || 'Произошла неизвестная ошибка';
    }
});
