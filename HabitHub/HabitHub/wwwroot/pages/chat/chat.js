const modal = {
    overlay: null,
    title: null,
    message: null,
    input: null,
    cancelBtn: null,
    confirmBtn: null,
    resolve: null,
    reject: null,

    init: function() {
        this.overlay = document.getElementById('modal-overlay');
        this.title = document.getElementById('modal-title');
        this.message = document.getElementById('modal-message');
        this.input = document.getElementById('modal-input');
        this.cancelBtn = document.getElementById('modal-cancel');
        this.confirmBtn = document.getElementById('modal-confirm');

        this.cancelBtn.addEventListener('click', () => this.hide(false));
        this.confirmBtn.addEventListener('click', () => this.hide(true));

        this.input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.hide(true);
        });
    },

    show: function({title, message, showInput = false, defaultValue = ''}) {
        this.title.textContent = title;
        this.message.textContent = message;

        if (showInput) {
            this.input.classList.remove('hidden');
            this.input.value = defaultValue;
            this.input.focus();
        } else {
            this.input.classList.add('hidden');
        }

        this.overlay.classList.add('visible');
        return new Promise((resolve, reject) => {
            this.resolve = resolve;
            this.reject = reject;
        });
    },

    hide: function(confirmed) {
        this.overlay.classList.remove('visible');
        if (this.resolve) {
            if (confirmed && this.input.classList.contains('hidden')) {
                this.resolve(true);
            } else if (confirmed) {
                this.resolve(this.input.value);
            } else {
                this.resolve(null);
            }
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    const elements = {
        companionsList: document.getElementById('companions'),
        messagesContainer: document.getElementById('messages-container'),
        messageInput: document.getElementById('message-input'),
        btnSend: document.getElementById('btn-send'),
        companionInfo: document.getElementById('companion-info'),
        searchInput: document.getElementById('search-companion'),
        btnSearch: document.getElementById('btn-search'),
        logoutBtn: document.getElementById('logout-btn'),
        profileLink: document.getElementById('profile-link')
    };

    const state = {
        companionsMap: {},
        currentUserId: localStorage.getItem('userId'),
        selectedCompanionId: null,
        connection: null,
        isInitialized: false
    };

    function init() {
        modal.init();
        
        if (!state.currentUserId) {
            window.location.href = '/login';
            return;
        }

        updateProfileLink();
        setupNavigationHandlers();

        initSignalR();
        setupEventListeners();
    }

    function updateProfileLink() {
        if (state.currentUserId && elements.profileLink) {
            elements.profileLink.href = `/profile/${state.currentUserId}`;
        }
    }

    function setupNavigationHandlers() {
        if (elements.logoutBtn) {
            elements.logoutBtn.addEventListener('click', logout);
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

    function initSignalR() {
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub', {
                accessTokenFactory: () => localStorage.getItem('jwt')
            })
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.elapsedMilliseconds < 30000) {
                        return 2000;
                    }
                    return 5000;
                }
            })
            .build();

        setupHubHandlers();

        state.connection.start()
            .then(() => {
                console.log('SignalR connected');
                return invokeWithAuth("GetAllCompanions");
            })
            .then(() => {
                return checkUrlForCompanion();
            })
            .then(() => {
                state.isInitialized = true;
            })
            .catch(handleConnectionError);
    }

    async function checkUrlForCompanion() {
        const companionId = getCompanionIdFromUrl();
        if (!companionId) return;

        if (state.companionsMap[companionId]) {
            selectCompanion(companionId, state.companionsMap[companionId]);
            cleanUrlFromCompanionParam();
            return;
        }

        const user = await fetchCompanionInfo(companionId);
        if (user) {
            state.companionsMap[companionId] = user;
            createCompanionListItem(companionId, user);
            selectCompanion(companionId, user);
            cleanUrlFromCompanionParam();
        }
    }

    async function invokeWithAuth(method, ...args) {
        try {
            return await state.connection.invoke(method, ...args);
        } catch (err) {
            if (err.statusCode === 401) {
                const refreshed = await handleUnauthorized();
                if (refreshed) {
                    return await state.connection.invoke(method, ...args);
                }
            }
            throw err;
        }
    }

    async function handleUnauthorized() {
        try {
            const refresh = await fetch('/api/auth/refresh', {
                method: 'POST',
                credentials: 'include'
            });

            if (refresh.ok) {
                const data = await refresh.json();
                if (data.token) {
                    localStorage.setItem('jwt', data.token);
                    await state.connection.stop();
                    initSignalR();
                    return true;
                }
            }

            localStorage.removeItem('jwt');
            localStorage.removeItem('userId');
            window.location.href = '/login';
            return false;
        } catch (error) {
            console.error('Token refresh error:', error);
            window.location.href = '/login';
            return false;
        }
    }

    function setupHubHandlers() {
        state.connection.on("ReceiveMessage", onReceiveMessage);
        state.connection.on("UpdateMessage", onUpdateMessage);
        state.connection.on("DeleteMessage", onDeleteMessage);
        state.connection.on("ChatHistory", onChatHistory);
        state.connection.on("CompanionsList", onCompanionsList);
        state.connection.on("Error", onError);
    }

    function onReceiveMessage(message) {
        if (!state.isInitialized) return;

        if (state.selectedCompanionId === message.senderId ||
            state.selectedCompanionId === message.recipientId) {
            addMessageToUI(message);
            scrollToBottom();
        }
        else if (message.senderId !== state.currentUserId) {
            updateUnreadCount(message.senderId);
        }
    }

    function onUpdateMessage(updatedMessage) {
        const messageElement = document.querySelector(`.message[data-id="${updatedMessage.id}"]`);
        if (messageElement) {
            messageElement.querySelector('.message-text').textContent = updatedMessage.text;
        }
    }

    function onDeleteMessage(messageId) {
        const messageElement = document.querySelector(`.message[data-id="${messageId}"]`);
        if (messageElement) {
            messageElement.remove();
        }
    }

    function onChatHistory(companionId, messages) {
        if (companionId !== state.selectedCompanionId) return;

        elements.messagesContainer.innerHTML = '';

        if (!messages || messages.length === 0) {
            elements.messagesContainer.innerHTML = '<div class="no-messages">Нет сообщений</div>';
            return;
        }

        messages.forEach(message => addMessageToUI(message));
        scrollToBottom();
    }

    function onCompanionsList(companionIds) {
        elements.companionsList.innerHTML = '';

        if (!companionIds || companionIds.length === 0) {
            elements.companionsList.innerHTML = '<li class="no-companions">Нет активных чатов</li>';
            return;
        }

        companionIds.forEach(companionId => {
            fetchCompanionInfo(companionId)
                .then(user => {
                    if (user) {
                        state.companionsMap[companionId] = user;
                        createCompanionListItem(companionId, user);
                    }
                })
                .catch(error => {
                    console.error('Ошибка загрузки компаньона:', error);
                });
        });
    }

    function onError(errorMessage) {
        modal.show({
            title: 'Ошибка',
            message: errorMessage
        }).then(() => {
            console.error('ChatHub Error:', errorMessage);
        });
    }

    function setupEventListeners() {
        elements.btnSend.addEventListener('click', sendMessage);
        elements.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage();
        });

        elements.btnSearch.addEventListener('click', searchCompanions);
        elements.searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') searchCompanions();
        });
    }

    function getCompanionIdFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('companion');
    }

    function cleanUrlFromCompanionParam() {
        const url = new URL(window.location);
        url.searchParams.delete('companion');
        window.history.replaceState({}, '', url);
    }

    function handleConnectionError(err) {
        console.error('SignalR connection error:', err);
        if (err.statusCode === 401) {
            modal.show({
                title: 'Ошибка авторизации',
                message: 'Ваша сессия истекла. Вы будете перенаправлены на страницу входа.'
            }).then(() => {
                handleUnauthorized();
            });
        } else {
            modal.show({
                title: 'Ошибка соединения',
                message: 'Не удалось подключиться к серверу. Повторная попытка через 5 секунд.'
            }).then(() => {
                setTimeout(initSignalR, 5000);
            });
        }
    }

    function createCompanionListItem(companionId, user) {
        const companionItem = document.createElement('li');
        companionItem.className = 'companion-item';
        companionItem.dataset.id = companionId;

        companionItem.innerHTML = `
            <div class="companion-name">${user.surname} ${user.name}</div>
            <div class="unread-count">0</div>
        `;

        companionItem.addEventListener('click', () => selectCompanion(companionId, user));
        elements.companionsList.appendChild(companionItem);
    }

    async function fetchCompanionInfo(companionId) {
        try {
            const res = await fetchWithRetry(`/api/users/get/${companionId}`);
            if (!res.ok) return null;
            return await res.json();
        } catch (error) {
            console.error('Ошибка загрузки информации о компаньоне:', error);
            return null;
        }
    }

    function selectCompanion(companionId, userInfo = null) {
        if (state.selectedCompanionId === companionId) return;

        state.selectedCompanionId = companionId;
        updateCompanionSelectionUI(companionId);

        elements.messagesContainer.innerHTML = '<div class="loading">Загрузка сообщений...</div>';

        if (userInfo) {
            displayCompanionInfo(userInfo);
        } else {
            fetchCompanionInfo(companionId)
                .then(user => {
                    if (user) displayCompanionInfo(user);
                });
        }

        loadChatHistoryForCompanion(companionId);
    }

    function updateCompanionSelectionUI(companionId) {
        document.querySelectorAll('.companion-item').forEach(item => {
            item.classList.toggle('active', item.dataset.id === companionId);
        });
    }

    function displayCompanionInfo(user) {
        const userLink = document.createElement('a');
        userLink.textContent = `${user.surname} ${user.name}`;
        userLink.href = `/profile/${user.id}`;
        userLink.className = 'user-profile-link';
        userLink.style.cursor = 'pointer';
        userLink.style.marginRight = '4px';
        elements.companionInfo.append(userLink);
    }

    function loadChatHistoryForCompanion(companionId) {
        invokeWithAuth("GetChatHistory", companionId)
            .catch(err => {
                console.error('Ошибка загрузки истории:', err);
                elements.messagesContainer.innerHTML = '<div class="error">Ошибка загрузки истории чата</div>';
            });
    }

    function addMessageToUI(message) {
        const isSent = message.senderId === state.currentUserId;
        const messageElement = document.createElement('div');
        messageElement.className = `message ${isSent ? 'sent' : 'received'}`;
        messageElement.dataset.id = message.id;

        messageElement.innerHTML = `
            <div class="message-text">${message.text}</div>
            <div class="message-time">${formatDateTime(message.dateTime)}</div>
            ${isSent ? `
                <div class="message-actions">
                    <button class="btn-edit" title="Редактировать">✏️</button>
                    <button class="btn-delete" title="Удалить">🗑️</button>
                </div>
            ` : ''}
        `;

        elements.messagesContainer.appendChild(messageElement);

        if (isSent) {
            setupMessageActions(messageElement, message);
        }
    }

    function formatDateTime(dateTime) {
        const date = new Date(dateTime);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    function setupMessageActions(messageElement, message) {
        const btnEdit = messageElement.querySelector('.btn-edit');
        const btnDelete = messageElement.querySelector('.btn-delete');

        btnEdit.addEventListener('click', () => requestEditMessage(message.id, message.text));
        btnDelete.addEventListener('click', () => requestDeleteMessage(message.id));
    }

    function requestEditMessage(messageId, currentText) {
        modal.show({
            title: 'Редактирование сообщения',
            message: 'Введите новый текст сообщения:',
            showInput: true,
            defaultValue: currentText
        }).then(newText => {
            if (newText && newText.trim() && newText !== currentText) {
                invokeWithAuth("EditMessage", messageId, newText.trim())
                    .catch(err => console.error('Ошибка редактирования:', err));
            }
        });
    }

    function requestDeleteMessage(messageId) {
        modal.show({
            title: 'Удаление сообщения',
            message: 'Вы уверены, что хотите удалить это сообщение?'
        }).then(confirmed => {
            if (confirmed) {
                invokeWithAuth("DeleteMessage", messageId)
                    .catch(err => console.error('Ошибка удаления:', err));
            }
        });
    }

    function sendMessage() {
        const text = elements.messageInput.value.trim();
        if (!text || !state.selectedCompanionId) return;

        invokeWithAuth("SendMessage", state.selectedCompanionId, text)
            .then(() => {
                elements.messageInput.value = '';
            })
            .catch(err => {
                console.error('Ошибка отправки сообщения:', err);
            });
    }

    function searchCompanions() {
        const searchTerm = elements.searchInput.value.toLowerCase().trim();
        if (!searchTerm) return;

        document.querySelectorAll('.companion-item').forEach(item => {
            const name = item.querySelector('.companion-name').textContent.toLowerCase();
            item.style.display = name.includes(searchTerm) ? '' : 'none';
        });
    }

    function updateUnreadCount(companionId) {
        const companionItem = document.querySelector(`.companion-item[data-id="${companionId}"]`);
        if (companionItem) {
            const unreadElement = companionItem.querySelector('.unread-count');
            const currentCount = parseInt(unreadElement.textContent) || 0;
            unreadElement.textContent = currentCount + 1;
        }
    }

    function scrollToBottom() {
        elements.messagesContainer.scrollTop = elements.messagesContainer.scrollHeight;
    }

    async function fetchWithRetry(url, options = {}, retry = true) {
        const token = localStorage.getItem('jwt');
        options.headers = {
            ...(options.headers || {}),
            'Authorization': `Bearer ${token}`
        };
        options.credentials = 'include';

        try {
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
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    }

    init();
});