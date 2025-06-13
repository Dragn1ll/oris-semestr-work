const modalOverlay = document.getElementById('modal-overlay');
const modalTitle = document.getElementById('modal-title');
const modalMessage = document.getElementById('modal-message');
const modalOk = document.getElementById('modal-ok');

function showAlert(title, message) {
    modalTitle.textContent = title;
    modalMessage.textContent = message;
    modalOverlay.classList.add('visible');

    return new Promise(resolve => {
        const handler = () => {
            modalOverlay.classList.remove('visible');
            modalOk.removeEventListener('click', handler);
            resolve();
        };
        modalOk.addEventListener('click', handler);
    });
}

function showNotification(message, type = 'info', duration = 3000) {
    const container = document.getElementById('notification-container');
    if (!container) return;

    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;

    container.appendChild(notification);

    setTimeout(() => {
        notification.style.opacity = '0';
        setTimeout(() => notification.remove(), 300);
    }, duration);
    
    notification.addEventListener('click', () => {
        notification.style.opacity = '0';
        setTimeout(() => notification.remove(), 300);
    });
}

const logoutBtn = document.getElementById('logout-btn');
const profileLink = document.getElementById('profile-link');

function updateProfileLink() {
    const userId = localStorage.getItem('userId');
    if (userId && profileLink) {
        profileLink.href = `/profile/${userId}`;
    }
}

async function logout() {
    try {
        const response = await fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
        });

        if (response.ok) {
            localStorage.removeItem('jwt');
            localStorage.removeItem('userId');
            window.location.href = '/login';
        } else {
            console.error('Logout failed:', response.status);
        }
    } catch (error) {
        console.error('Logout error:', error);
    }
}

function initNavigation() {
    updateProfileLink();

    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }
}

initNavigation();

const habitTypeSelect = document.getElementById('habitType');
const physicalActivityTypeSelect = document.getElementById('physicalActivityType');
const physicalActivitySection = document.getElementById('physicalActivitySection');
const toggleFormBtn = document.getElementById('toggleFormBtn');
const habitForm = document.getElementById('habitForm');
const submitHabit = document.getElementById('submitHabit');
const habitList = document.getElementById('habitList');
const goalInput = document.getElementById('habitGoal');
const typeError = document.getElementById('typeError');
const subtypeError = document.getElementById('subtypeError');
const goalError = document.getElementById('goalError');

let habits = [];
let selectedProgressHabit = null;

const HabitTypeLabels = {
    1: "Физическая активность",
    2: "Здоровое питание",
    3: "Умственные привычки",
    4: "Продуктивность и организация",
    5: "Финансовые привычки",
    6: "Социальные привычки",
    7: "Духовные практики",
    8: "Гигиена и уход за собой",
    9: "Творческие привычки",
    10: "Экологические привычки",
    11: "Привычки сна",
    12: "Вредные привычки",
    13: "Другие привычки"
};

const PhysicalActivityLabels = {
    1: "Ходьба",
    2: "Бег",
    3: "Велоспорт",
    4: "Плавание",
    5: "Лыжи",
    6: "Сноуборд",
    7: "Йога",
    8: "Другая активность"
};

let hasGoogleToken = false;
let googleFitModal = null;

async function checkGoogleToken() {
    try {
        const res = await fetchWithRetry('/api/google/token/contains');
        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            return false;
        }
        return await res.json();
    } catch (e) {
        console.error('Ошибка проверки Google токена', e);
        return false;
    }
}

async function fetchGoogleFitProgress(habitId, fromDate, toDate) {
    const maxRetries = 3;
    const retryDelay = 1000;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            const res = await fetchWithRetry('/api/google/fit/progress/analyze', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    habitId,
                    fromDate: new Date(fromDate).toISOString(),
                    toDate: new Date(toDate).toISOString()
                })
            });

            if (!res || !res.ok) {
                if (res) await parseApiError(res);
                throw new Error(`HTTP error! status: ${res ? res.status : 'no response'}`);
            }

            return await res.json();
        } catch (e) {
            console.error(`Ошибка получения данных Google Fit (попытка ${attempt}/${maxRetries})`, e);

            if (attempt === maxRetries) {
                throw e;
            }

            await new Promise(resolve => setTimeout(resolve, retryDelay));
        }
    }
}

function showGoogleFitModal(habit) {
    if (!googleFitModal) {
        googleFitModal = document.createElement('div');
        googleFitModal.style.position = 'fixed';
        googleFitModal.style.top = '0';
        googleFitModal.style.left = '0';
        googleFitModal.style.width = '100%';
        googleFitModal.style.height = '100%';
        googleFitModal.style.backgroundColor = 'rgba(0,0,0,0.7)';
        googleFitModal.style.display = 'flex';
        googleFitModal.style.justifyContent = 'center';
        googleFitModal.style.alignItems = 'center';
        googleFitModal.style.zIndex = '2000';
        document.body.appendChild(googleFitModal);
    }

    googleFitModal.innerHTML = '';
    const modalContent = document.createElement('div');
    modalContent.style.backgroundColor = '#2a2a2a';
    modalContent.style.padding = '20px';
    modalContent.style.borderRadius = '8px';
    modalContent.style.width = '500px';
    modalContent.style.maxWidth = '90%';

    const step1Form = document.createElement('div');
    step1Form.id = 'googleFitStep1';
    step1Form.innerHTML = `
        <h3>Получить данные из Google Fit</h3>
        <p>Укажите период для анализа активности:</p>
        <label>Дата и время начала:
            <input type="datetime-local" id="googleFitStart" required />
        </label>
        <label>Дата и время окончания:
            <input type="datetime-local" id="googleFitEnd" required />
        </label>
        <div class="modal-buttons">
            <button type="button" id="googleFitCancel">Отмена</button>
            <button type="button" id="googleFitSubmit">Получить данные</button>
        </div>
    `;

    const step2Form = document.createElement('div');
    step2Form.id = 'googleFitStep2';
    step2Form.classList.add('hidden');
    step2Form.innerHTML = `
        <h3>Результат анализа Google Fit</h3>
        <div id="googleFitResults" style="margin: 15px 0;"></div>
        <label>Выберите дату для сохранения прогресса:
            <input type="date" id="saveProgressDate" required />
        </label>
        <div class="modal-buttons">
            <button type="button" id="googleFitBack">Назад</button>
            <button type="button" id="googleFitSave">Сохранить прогресс</button>
        </div>
    `;

    modalContent.appendChild(step1Form);
    modalContent.appendChild(step2Form);
    googleFitModal.appendChild(modalContent);
    googleFitModal.style.display = 'flex';

    document.getElementById('googleFitCancel').addEventListener('click', () => {
        googleFitModal.style.display = 'none';
    });

    document.getElementById('googleFitSubmit').addEventListener('click', async () => {
        const start = document.getElementById('googleFitStart').value;
        const end = document.getElementById('googleFitEnd').value;

        if (!start || !end) {
            showNotification('Заполните оба поля даты', 'error');
            return;
        }

        const submitBtn = document.getElementById('googleFitSubmit');
        const cancelBtn = document.getElementById('googleFitCancel');
        const originalText = submitBtn.textContent;

        submitBtn.textContent = 'Загрузка...';
        submitBtn.disabled = true;
        cancelBtn.disabled = true;

        try {
            const result = await fetchGoogleFitProgress(habit.id, start, end);

            document.getElementById('googleFitResults').innerHTML = `
            <p><strong>Процент выполнения:</strong> ${result.completionPercentage}%</p>
            <p><strong>Анализ:</strong> ${result.analysisSummary}</p>
        `;

            step1Form.classList.add('hidden');
            step2Form.classList.remove('hidden');
        } catch (e) {
            showNotification('Ошибка получения данных. Попробуйте позже', 'error');
            console.error('Не удалось получить данные Google Fit', e);
        } finally {
            submitBtn.textContent = originalText;
            submitBtn.disabled = false;
            cancelBtn.disabled = false;
        }
    });

    document.getElementById('googleFitBack').addEventListener('click', () => {
        step2Form.classList.add('hidden');
        step1Form.classList.remove('hidden');
    });

    document.getElementById('googleFitSave').addEventListener('click', async () => {
        const date = document.getElementById('saveProgressDate').value;
        const results = document.getElementById('googleFitResults');
        const percent = results.querySelector('p:first-child')?.textContent?.match(/\d+/)?.[0];

        if (!date || !percent) {
            showNotification('Выберите дату для сохранения', 'error');
            return;
        }

        const success = await addProgress(habit.id, date, percent);
        if (success) {
            showNotification('Прогресс успешно сохранён!', 'success');
            googleFitModal.style.display = 'none';
            loadHabits();
        } else {
            showNotification('Ошибка сохранения прогресса', 'error');
        }
    });
}

async function fetchWithRetry(url, options = {}, retry = true) {
    const token = localStorage.getItem('jwt');
    options.headers = {
        ...(options.headers || {}),
        'Authorization': `Bearer ${token}`
    };
    options.credentials = 'include';

    const response = await fetch(url, options);

    if (response.status === 401 && retry) {
        const refresh = await fetch('/api/auth/refresh', {
            method: 'POST',
            credentials: 'include'
        });

        if (!refresh.ok) {
            localStorage.removeItem('jwt');
            window.location.href = '/login';
            return null;
        }

        const data = await refresh.json();
        if (data.token) {
            localStorage.setItem('jwt', data.token);
        }

        return fetchWithRetry(url, options, false);
    }

    return response;
}

async function parseApiError(response) {
    try {
        const result = await response.json();
        const parsed = JSON.parse(result.message);
        console.error(`API Error ${parsed.status}: ${parsed.title} — ${parsed.detail}`);
    } catch (e) {
        console.error('Не удалось распарсить ошибку от API', e);
    }
}


function initTypeSelects() {
    for (let id in HabitTypeLabels) {
        const opt = document.createElement('option');
        opt.value = id;
        opt.textContent = HabitTypeLabels[id];
        habitTypeSelect.appendChild(opt);
    }
    for (let id in PhysicalActivityLabels) {
        const opt = document.createElement('option');
        opt.value = id;
        opt.textContent = PhysicalActivityLabels[id];
        physicalActivityTypeSelect.appendChild(opt);
    }
}
initTypeSelects();

habitTypeSelect.addEventListener('change', () => {
    const selectedTypeId = parseInt(habitTypeSelect.value);
    physicalActivitySection.classList.toggle('hidden', selectedTypeId !== 1);
});

toggleFormBtn.onclick = () => habitForm.classList.toggle('hidden');

submitHabit.onclick = async () => {
    typeError.textContent = subtypeError.textContent = goalError.textContent = '';
    const typeId = parseInt(habitTypeSelect.value);
    const subTypeId = parseInt(physicalActivityTypeSelect.value);
    const goal = goalInput.value.trim();

    let valid = true;
    if (!typeId) { typeError.textContent = 'Выберите тип'; valid = false; }
    if (typeId === 1 && !subTypeId) { subtypeError.textContent = 'Выберите подтип'; valid = false; }
    if (!goal) { goalError.textContent = 'Введите цель'; valid = false; }
    if (!valid) return;

    const payload = {
        type: typeId,
        physicalActivityType: typeId === 1 ? subTypeId : null,
        goal
    };

    try {
        const res = await fetchWithRetry('/api/habits/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            showNotification('Ошибка при создании привычки', 'error');
            return;
        }

        await res.json();
        loadHabits();
        habitForm.reset();
        habitForm.classList.add('hidden');
    } catch (e) {
        console.error('Ошибка сети или запроса', e);
    }
};

async function loadHabits() {
    try {
        const res = await fetchWithRetry('/api/habits/get/all');
        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            habitList.innerText = 'Ошибка загрузки';
            return;
        }
        habits = await res.json();
        renderHabits();
    } catch (e) {
        console.error('Network error', e);
        habitList.innerText = 'Ошибка загрузки';
    }
}

async function toggleActive(h) {
    const payload = { ...h, isActive: !h.isActive };
    try {
        const res = await fetchWithRetry('/api/habits/put', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            return;
        }
        loadHabits();
    } catch (e) {
        console.error('Network error', e);
    }
}

async function deleteHabit(id) {
    try {
        const res = await fetchWithRetry(`/api/habits/delete/${id}`, { method: 'DELETE' });
        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            return;
        }
        loadHabits();
    } catch (e) {
        console.error('Network error', e);
    }
}

async function addProgress(habitId, date, percent) {
    try {
        const res = await fetchWithRetry('/api/habits/progress/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                habitId,
                date,
                percentageCompletion: parseFloat(percent)
            })
        });

        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            return false;
        }
        return true;
    } catch (e) {
        console.error('Ошибка сохранения прогресса', e);
        return false;
    }
}

async function loadProgress(habitId) {
    try {
        const res = await fetchWithRetry(`/api/habits/${habitId}/progress/get/all`);
        if (!res || !res.ok) {
            if (res) await parseApiError(res);
            return;
        }
        const all = await res.json();
        habits = habits.map(h => h.id === habitId ? { ...h, progress: all } : h);
        renderHabits();
    } catch (e) {
        console.error('Network error', e);
    }
}

function renderHabits() {
    habitList.innerHTML = '';
    habits.forEach(h => {
        const div = document.createElement('div');
        div.className = 'habit-item';

        const header = document.createElement('div');
        header.className = 'habit-header';

        const habitTypeNum = Number(h.type);
        const habitTypeLabel = HabitTypeLabels[habitTypeNum] || 'Неизвестный тип';
        let subTypeLabel = '';
        if (habitTypeNum === 1) {
            const subTypeNum = Number(h.physicalActivityType);
            subTypeLabel = ` (${PhysicalActivityLabels[subTypeNum] || 'Неизвестный подтип'})`;
        }

        header.textContent = `${h.goal} — ${habitTypeLabel}${subTypeLabel}`;

        const actions = document.createElement('div');

        ['Прогресс', h.isActive ? 'Деактивировать' : 'Активировать', 'Удалить'].forEach(text => {
            const btn = document.createElement('button');
            btn.textContent = text;
            btn.onclick = async () => {
                if (text === 'Прогресс') {
                    selectedProgressHabit = selectedProgressHabit === h.id ? null : h.id;
                    if (selectedProgressHabit) await loadProgress(h.id);
                    else renderHabits();
                }
                if (text === 'Деактивировать' || text === 'Активировать') await toggleActive(h);
                if (text === 'Удалить') await deleteHabit(h.id);
            };
            actions.appendChild(btn);
        });

        if (habitTypeNum === 1 && hasGoogleToken) {
            const googleFitBtn = document.createElement('button');
            googleFitBtn.textContent = 'Получить данные из GoogleFit';
            googleFitBtn.onclick = () => showGoogleFitModal(h);
            actions.appendChild(googleFitBtn);
        }

        div.appendChild(header);
        div.appendChild(actions);

        if (selectedProgressHabit === h.id) {
            const sec = document.createElement('div');
            sec.className = 'progress-section';

            const dateInput = document.createElement('input');
            dateInput.type = 'date';
            dateInput.style.width = '35%';
            dateInput.style.padding = '12px';
            dateInput.style.fontSize = '16px';
            dateInput.style.marginRight = '15px';
            dateInput.style.marginBottom = '10px';
            dateInput.style.borderRadius = '4px';
            dateInput.style.border = '1px solid #444';
            dateInput.style.backgroundColor = '#2a2a2a';
            dateInput.style.color = '#fff';

            const percentInput = document.createElement('input');
            percentInput.type = 'number';
            percentInput.min = 0;
            percentInput.max = 100;
            percentInput.placeholder = '%';
            percentInput.style.width = '35%';
            percentInput.style.padding = '12px';
            percentInput.style.fontSize = '16px';
            percentInput.style.marginRight = '15px';
            percentInput.style.marginBottom = '10px';
            percentInput.style.borderRadius = '4px';
            percentInput.style.border = '1px solid #444';
            percentInput.style.backgroundColor = '#2a2a2a';
            percentInput.style.color = '#fff';

            const addBtn = document.createElement('button');
            addBtn.textContent = 'Добавить';
            addBtn.style.padding = '12px 18px';
            addBtn.style.fontSize = '15px';
            addBtn.onclick = () => {
                const date = dateInput.value;
                const percent = percentInput.value;

                if (!date || percent === '') {
                    showNotification('Введите дату и процент', 'error');
                    return;
                }

                const existing = (h.progress || []).find(p => p.date === date);

                if (existing) {
                    existing.percentageCompletion = parseFloat(percent);
                } else {
                    h.progress = [...(h.progress || []), {
                        date,
                        percentageCompletion: parseFloat(percent)
                    }];
                }

                addProgress(h.id, date, percent);
            };

            sec.append(dateInput, percentInput, addBtn);

            const ul = document.createElement('ul');
            ul.style.marginTop = '10px';
            ul.style.paddingLeft = '20px';

            (h.progress || []).forEach(p => {
                const li = document.createElement('li');
                li.textContent = `${p.date} — ${p.percentageCompletion}%`;
                ul.appendChild(li);
            });

            sec.appendChild(ul);
            div.appendChild(sec);
        }

        habitList.appendChild(div);
    });
}

async function initPage() {
    await loadHabits();
    hasGoogleToken = await checkGoogleToken();
    renderHabits();
}

initPage();