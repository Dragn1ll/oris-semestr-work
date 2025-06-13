document.getElementById('registerForm').addEventListener('submit', async function (e) {
    e.preventDefault();

    const name = document.getElementById('name').value.trim();
    const surname = document.getElementById('surname').value.trim();
    const patronymic = document.getElementById('patronymic').value.trim();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value.trim();
    const status = document.getElementById('status').value.trim();
    const birthDay = document.getElementById('birthDay').value;
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
    if (!name || !surname || !birthDay) {
        error.innerText = 'Пожалуйста, заполните все обязательные поля';
        return;
    }

    try {
        const res = await fetch('http://localhost:5000/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, surname, patronymic, email, password, status, birthDay })
        });

        const contentType = res.headers.get('Content-Type') || '';
        let result = {};
        if (contentType.includes('application/json')) {
            result = await res.json();
        }

        if (!res.ok) {
            throw new Error(result.detail || result.title || 'Ошибка при регистрации');
        }

        window.location.href = 'http://localhost:5000/login';
    } catch (err) {
        error.innerText = err.message || 'Произошла неизвестная ошибка';
    }
});