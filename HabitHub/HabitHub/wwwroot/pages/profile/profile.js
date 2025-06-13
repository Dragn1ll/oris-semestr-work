const navLogoutBtn = document.getElementById('nav-logout-btn');
const navProfileLink = document.getElementById('nav-profile-link');

function updateNavProfileLink() {
    const userId = localStorage.getItem('userId');
    if (userId && navProfileLink) {
        navProfileLink.href = `/profile/${userId}`;
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
    updateNavProfileLink();

    if (navLogoutBtn) {
        navLogoutBtn.addEventListener('click', logout);
    }
}

initNavigation();

const currentUserId = localStorage.getItem('userId');
const urlParts = window.location.pathname.split('/');
const profileUserId = urlParts[urlParts.length - 1];
const container = document.getElementById('post-feed');
const actionsDiv = document.getElementById('profile-actions');
const profileName = document.getElementById('profile-name');
const profileBirthday = document.getElementById('profile-birthday');
const profileStatus = document.getElementById('profile-status');
const postsCount = document.getElementById('posts-count');
const likesCount = document.getElementById('likes-count');
const commentsCount = document.getElementById('comments-count');
const telegramInfo = document.getElementById('telegram-info');
const telegramUserId = document.getElementById('telegram-user-id');

const isMyProfile = profileUserId === currentUserId;

let telegramInstructionsVisible = false;

const firstMessageModal = document.getElementById('first-message-modal');
const firstMessageForm = document.getElementById('first-message-form');
document.querySelectorAll('.close-modal').forEach(btn => {
    btn.onclick = () => {
        document.querySelectorAll('.modal').forEach(modal => {
            modal.style.display = 'none';
        });
    };
});

async function sendFirstMessage(recipientId) {
    return new Promise((resolve, reject) => {
        firstMessageModal.style.display = 'flex';
        const submitHandler = async (e) => {
            e.preventDefault();

            const text = document.getElementById('first-message-text').value.trim();
            if (!text) return;

            const btnSubmit = firstMessageForm.querySelector('button[type="submit"]');
            const originalText = btnSubmit.textContent;
            btnSubmit.disabled = true;
            btnSubmit.textContent = 'Отправка...';

            try {
                const messageData = {
                    RecipientId: recipientId,
                    SenderId: currentUserId,
                    Text: text
                };

                const res = await fetchWithRetry('/api/users/send/message', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(messageData)
                });

                if (!res.ok) {
                    throw new Error('Ошибка отправки сообщения');
                }

                firstMessageModal.style.display = 'none';
                firstMessageForm.removeEventListener('submit', submitHandler);

                window.location.href = `/chats?companion=${recipientId}`;
                resolve();
            } catch (error) {
                console.error('Ошибка отправки сообщения:', error);
                showNotification('Не удалось отправить сообщение', false);
                btnSubmit.disabled = false;
                btnSubmit.textContent = originalText;
                reject(error);
            }
        };

        firstMessageForm.addEventListener('submit', submitHandler);

        const closeHandler = () => {
            firstMessageModal.style.display = 'none';
            firstMessageForm.removeEventListener('submit', submitHandler);
            reject(new Error('Отменено пользователем'));
        };

        firstMessageModal.querySelectorAll('.close-modal').forEach(btn => {
            btn.addEventListener('click', closeHandler);
        });
    });
}

async function fetchWithRetry(url, options = {}, retry = true) {
    const token = localStorage.getItem('jwt');
    options.headers = {
        ...(options.headers || {}),
        'Authorization': `Bearer ${token}`
    };
    options.credentials = 'include';

    if (!(options.body instanceof FormData)) {
        if (options.headers) {
            options.headers['Content-Type'] = 'application/json';
        }
    }

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

function showNotification(message, isSuccess = true) {
    const notification = document.getElementById('notification');
    notification.textContent = message;
    notification.style.backgroundColor = isSuccess ? '#4CAF50' : '#f44336';
    notification.style.display = 'block';

    setTimeout(() => {
        notification.style.display = 'none';
    }, 3000);
}

function showConfirm(title, message, callback) {
    const confirmModal = document.getElementById('confirm-modal');
    const confirmTitle = document.getElementById('confirm-title');
    const confirmMessage = document.getElementById('confirm-message');
    const confirmAction = document.getElementById('confirm-action');

    confirmTitle.textContent = title;
    confirmMessage.textContent = message;
    confirmModal.style.display = 'flex';

    const newConfirmAction = confirmAction.cloneNode(true);
    confirmAction.parentNode.replaceChild(newConfirmAction, confirmAction);

    newConfirmAction.onclick = () => {
        callback();
        confirmModal.style.display = 'none';
    };
}

async function loadProfile() {
    try {
        const userRes = await fetchWithRetry(`/api/users/get/${profileUserId}`);
        if (!userRes.ok) {
            throw new Error('Не удалось загрузить данные пользователя');
        }

        const user = await userRes.json();

        profileName.textContent = `${user.surname} ${user.name}`;
        if (user.patronymic) {
            profileName.textContent += ` ${user.patronymic}`;
        }

        profileBirthday.textContent = `Дата рождения: ${new Date(user.birthDay).toLocaleDateString()}`;

        if (user.status) {
            profileStatus.textContent = `Статус: ${user.status}`;
        }

        if (isMyProfile) {
            actionsDiv.innerHTML = `
                    <button id="btn-add-post">Добавить пост</button>
                    <button id="btn-edit-profile">Редактировать профиль</button>
                    <button id="btn-logout">Выйти из аккаунта</button>
                    <button id="btn-delete-profile" class="delete">Удалить профиль</button>
                    <button id="btn-connect-google">Подключить Google</button>
                    <button id="btn-connect-telegram">Включить уведомления</button>
                `;

            document.getElementById('btn-add-post').onclick = async () => {
                const habitsRes = await fetchWithRetry('/api/habits/get/all');
                if (!habitsRes.ok) {
                    showNotification('Ошибка загрузки привычек', false);
                    return;
                }

                const habits = await habitsRes.json();
                const habitSelect = document.getElementById('post-habit');
                habitSelect.innerHTML = '';

                habits.forEach(habit => {
                    const option = document.createElement('option');
                    option.value = habit.id;
                    option.textContent = habit.goal;
                    habitSelect.appendChild(option);
                });

                document.getElementById('add-post-modal').style.display = 'flex';
            };

            document.getElementById('btn-edit-profile').onclick = () => {
                document.getElementById('edit-profile-modal').style.display = 'flex';
                document.getElementById('edit-name').value = user.name;
                document.getElementById('edit-surname').value = user.surname;
                document.getElementById('edit-patronymic').value = user.patronymic || '';
                document.getElementById('edit-status').value = user.status || '';
                document.getElementById('edit-birthday').value = user.birthDay;
            };

            document.getElementById('btn-delete-profile').onclick = () => {
                showConfirm(
                    'Удаление профиля',
                    'Вы уверены, что хотите удалить свой профиль? Это действие нельзя отменить.',
                    async () => {
                        const res = await fetchWithRetry('/api/users/delete', {
                            method: 'DELETE'
                        });

                        if (res.ok) {
                            localStorage.clear();
                            window.location.href = '/';
                        } else {
                            showNotification('Ошибка при удалении профиля', false);
                        }
                    }
                );
            };

            document.getElementById('btn-connect-telegram').onclick = () => {
                telegramInstructionsVisible = !telegramInstructionsVisible;
                telegramInfo.style.display = telegramInstructionsVisible ? 'block' : 'none';
                telegramUserId.textContent = currentUserId;
            };

            document.getElementById('btn-logout').onclick = async () => {
                try {
                    const res = await fetchWithRetry('/api/auth/logout', {
                        method: 'POST'
                    });

                    if (res.ok) {
                        localStorage.clear();
                        window.location.href = '/login';
                    } else {
                        showNotification('Ошибка при выходе из аккаунта', false);
                    }
                } catch (error) {
                    console.error('Ошибка при выходе из аккаунта:', error);
                    showNotification('Ошибка при выходе из аккаунта', false);
                }
            };

            const client = google.accounts.oauth2.initCodeClient({
                client_id: '319850806378-8oith89dlmvcvths4mk276q9si92rmcl.apps.googleusercontent.com',
                scope: 'https://www.googleapis.com/auth/fitness.activity.read',
                ux_mode: 'popup',
                redirect_uri: 'http://localhost:5000',
                callback: async (response) => {
                    const res = await fetchWithRetry('/api/google/token/add', {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({
                            code: response.code
                        }),
                    });

                    if (res.ok) {
                        showNotification('Google аккаунт успешно подключен!');
                        document.getElementById('btn-connect-google').textContent = 'Google подключен ✅';
                        document.getElementById('btn-connect-google').disabled = true;
                    } else {
                        const error = await res.text();
                        console.error('Ошибка подключения Google:', error);
                        showNotification('Ошибка подключения Google: ' + error, false);
                    }
                }
            });

            const btnGoogle = document.getElementById('btn-connect-google');
            btnGoogle.onclick = () => client.requestCode();

            try {
                const googleRes = await fetchWithRetry('/api/google/token/contains');
                if (googleRes.ok) {
                    const isConnected = await googleRes.json();
                    if (isConnected) {
                        btnGoogle.textContent = 'Google подключен ✅';
                        btnGoogle.disabled = true;
                    }
                }
            } catch (e) {
                console.error('Ошибка проверки Google подключения:', e);
            }
        } else {
            actionsDiv.innerHTML = `<button id="btn-send-msg">Отправить сообщение</button>`;

            document.getElementById('btn-send-msg').onclick = async () => {
                const btn = document.getElementById('btn-send-msg');
                const originalText = btn.textContent;
                btn.disabled = true;
                btn.textContent = 'Отправка...';

                try {
                    await sendFirstMessage(profileUserId);
                } catch (error) {
                    console.error('Ошибка отправки сообщения:', error);
                    btn.disabled = false;
                    btn.textContent = originalText;
                    showNotification('Не удалось отправить сообщение', false);
                }
            };
        }

        await renderPosts();

    } catch (error) {
        console.error('Ошибка загрузки профиля:', error);
        showNotification('Ошибка загрузки профиля: ' + error.message, false);
    }
}

async function renderPosts() {
    container.innerHTML = '';
    let res = await fetchWithRetry(`/api/posts/get/user/${profileUserId}`);
    if (!res?.ok) return;

    const posts = await res.json();
    postsCount.textContent = posts.length;

    let totalLikes = 0;
    let totalComments = 0;

    for (let post of posts) {
        totalLikes += post.likesCount;
        totalComments += post.commentsCount;

        const postDiv = document.createElement('div');
        postDiv.className = 'post-item';

        const [userRes, habitRes] = await Promise.all([
            fetchWithRetry(`/api/users/get/${post.userId}`),
            fetchWithRetry(`/api/habits/get/${post.habitId}`)
        ]);

        if (!userRes.ok || !habitRes.ok) continue;

        const user = await userRes.json();
        const habit = await habitRes.json();

        const header = document.createElement('div');
        header.className = 'post-header';

        const userLink = document.createElement('a');
        userLink.textContent = `${user.surname} ${user.name}`;
        userLink.href = `/profile/${post.userId}`;
        userLink.className = 'user-profile-link';
        userLink.style.cursor = 'pointer';
        userLink.style.marginRight = '4px';

        const postInfo = document.createTextNode(` • ${habit.goal} • ${new Date(post.dateTime).toLocaleString()}`);

        header.appendChild(userLink);
        header.appendChild(postInfo);

        const text = document.createElement('div');
        text.className = 'post-text';
        text.textContent = post.text;

        const mediaDiv = document.createElement('div');
        mediaDiv.className = 'post-media';
        post.mediaFilesUrl.forEach(url => {
            const img = document.createElement('img');
            img.src = url;
            mediaDiv.appendChild(img);
        });

        const actionsDiv = document.createElement('div');
        actionsDiv.className = 'post-actions';

        const likeBtn = document.createElement('button');
        likeBtn.textContent = `${post.didUserLiked ? '❤️' : '🤍'} ${post.likesCount}`;
        likeBtn.onclick = async () => {
            const method = post.didUserLiked ? 'DELETE' : 'POST';
            const endpoint = `/api/posts/${post.didUserLiked ? 'delete' : 'add'}/${post.id}/like`;
            const res = await fetchWithRetry(endpoint, { method });

            if (res.ok) {
                post.didUserLiked = !post.didUserLiked;
                post.likesCount += post.didUserLiked ? 1 : -1;
                likeBtn.textContent = `${post.didUserLiked ? '❤️' : '🤍'} ${post.likesCount}`;
                likesCount.textContent = totalLikes + (post.didUserLiked ? 1 : -1);
            }
        };

        const commentToggleBtn = document.createElement('button');
        commentToggleBtn.textContent = `💬 ${post.commentsCount}`;
        const commentSection = document.createElement('div');
        commentSection.className = 'comments-container';

        commentToggleBtn.onclick = async () => {
            commentSection.style.display = commentSection.style.display === 'block' ? 'none' : 'block';
            if (commentSection.style.display === 'block' && commentSection.innerHTML === '') {
                await loadComments(post.id, commentSection, post);
            }
        };

        if (isMyProfile) {
            const deleteBtn = document.createElement('button');
            deleteBtn.textContent = '🗑️ Удалить';
            deleteBtn.className = 'delete';
            deleteBtn.onclick = () => {
                showConfirm(
                    'Удаление поста',
                    'Вы уверены, что хотите удалить этот пост?',
                    async () => {
                        const res = await fetchWithRetry(`/api/posts/delete/${post.id}`, {
                            method: 'DELETE'
                        });

                        if (res.ok) {
                            postDiv.remove();
                            postsCount.textContent = parseInt(postsCount.textContent) - 1;
                            likesCount.textContent = parseInt(likesCount.textContent) - post.likesCount;
                            commentsCount.textContent = parseInt(commentsCount.textContent) - post.commentsCount;
                        }
                    }
                );
            };
            actionsDiv.appendChild(deleteBtn);
        }

        actionsDiv.append(likeBtn, commentToggleBtn);
        postDiv.append(header, text, mediaDiv, actionsDiv, commentSection);
        container.appendChild(postDiv);
    }

    likesCount.textContent = totalLikes;
    commentsCount.textContent = totalComments;
}

async function loadComments(postId, container, post) {
    try {
        const res = await fetchWithRetry(`/api/posts/get/${postId}/comments`);
        if (!res.ok) {
            container.innerHTML = '<p>Не удалось загрузить комментарии</p>';
            return;
        }

        const comments = await res.json();
        container.innerHTML = '';

        let currentUser = null;
        if (currentUserId) {
            const userRes = await fetchWithRetry(`/api/users/get/${currentUserId}`);
            if (userRes.ok) {
                currentUser = await userRes.json();
            }
        }

        const ul = document.createElement('ul');
        for (const comment of comments) {
            const userRes = await fetchWithRetry(`/api/users/get/${comment.userId}`);
            const user = userRes.ok ? await userRes.json() : null;
            const userName = user ? `${user.surname} ${user.name}` : 'Неизвестный пользователь';

            const li = createCommentElement(comment, userName, post);
            ul.appendChild(li);
        }

        container.appendChild(ul);

        if (currentUserId) {
            const commentInputDiv = document.createElement('div');
            commentInputDiv.className = 'comment-section';

            const input = document.createElement('input');
            input.type = 'text';
            input.placeholder = 'Оставить комментарий...';

            const sendBtn = document.createElement('button');
            sendBtn.textContent = 'Отправить';
            sendBtn.className = 'send-comment-btn';
            sendBtn.onclick = async () => {
                try {
                    const commentText = input.value.trim();
                    if (!commentText) return;

                    const res = await fetchWithRetry(`/api/posts/add/${postId}/comment`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ comment: commentText })
                    });

                    if (!res.ok) {
                        showNotification('Ошибка добавления комментария', false);
                        return;
                    }

                    const { id: commentId } = await res.json();

                    const newComment = {
                        id: commentId,
                        text: commentText,
                        userId: currentUserId,
                        dateTime: new Date().toISOString()
                    };

                    const userName = currentUser ? `${currentUser.surname} ${currentUser.name}` : 'Вы';
                    const li = createCommentElement(newComment, userName, post);
                    ul.appendChild(li);

                    input.value = '';
                    post.commentsCount++;
                    document.querySelector(`.post-item[data-post-id="${postId}"] .comments-btn`).textContent = `💬 ${post.commentsCount}`;
                    commentsCount.textContent = parseInt(commentsCount.textContent) + 1;
                } catch (error) {
                    console.error('Ошибка добавления комментария:', error);
                    showNotification('Ошибка добавления комментария', false);
                }
            };

            commentInputDiv.append(input, sendBtn);
            container.appendChild(commentInputDiv);
        }

    } catch (error) {
        console.error('Ошибка загрузки комментариев:', error);
        container.innerHTML = '<p>Ошибка загрузки комментариев</p>';
    }
}

function createCommentElement(comment, userName, post) {
    const li = document.createElement('li');
    li.innerHTML = `
        <strong>${userName}:</strong> 
        <span>${comment.text}</span>
        <small>${new Date(comment.dateTime).toLocaleString()}</small>
    `;

    if (comment.userId === currentUserId) {
        const delBtn = document.createElement('button');
        delBtn.textContent = 'Удалить';
        delBtn.className = 'delete-comment';
        delBtn.onclick = async () => {
            const res = await fetchWithRetry(`/api/posts/delete/comments/${comment.id}`, {
                method: 'DELETE'
            });

            if (res.ok) {
                li.remove();
                const commentsBtn = document.querySelector(`.post-item[data-post-id="${post.id}"] .comments-btn`);
                const count = parseInt(commentsBtn.textContent.match(/\d+/)[0]) - 1;
                commentsBtn.textContent = `💬 ${count}`;
                commentsCount.textContent = parseInt(commentsCount.textContent) - 1;
            }
        };
        li.appendChild(delBtn);
    }

    return li;
}

document.querySelectorAll('.close-modal').forEach(btn => {
    btn.onclick = () => {
        document.querySelectorAll('.modal').forEach(modal => {
            modal.style.display = 'none';
        });
    };
});

document.getElementById('edit-profile-form').onsubmit = async (e) => {
    e.preventDefault();

    const updatedUser = {
        id: profileUserId,
        name: document.getElementById('edit-name').value,
        surname: document.getElementById('edit-surname').value,
        patronymic: document.getElementById('edit-patronymic').value || null,
        status: document.getElementById('edit-status').value || null,
        birthDay: document.getElementById('edit-birthday').value
    };

    const res = await fetchWithRetry('/api/users/put', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updatedUser)
    });

    if (res.ok) {
        showNotification('Профиль успешно обновлен!');
        loadProfile();
        document.getElementById('edit-profile-modal').style.display = 'none';
    } else {
        showNotification('Ошибка при обновлении профиля', false);
    }
};

document.getElementById('add-post-form').onsubmit = async (e) => {
    e.preventDefault();

    const formData = new FormData();
    formData.append('UserId', currentUserId);
    formData.append('Text', document.getElementById('post-text').value);
    formData.append('HabitId', document.getElementById('post-habit').value);

    const files = document.getElementById('post-media').files;
    for (let i = 0; i < files.length; i++) {
        formData.append('MediaFiles', files[i]);
    }

    const res = await fetchWithRetry('/api/posts/add', {
        method: 'POST',
        body: formData
    });

    if (res.ok) {
        const data = await res.json();
        showNotification('Пост успешно добавлен!');
        document.getElementById('add-post-modal').style.display = 'none';
        document.getElementById('add-post-form').reset();

        const postRes = await fetchWithRetry(`/api/posts/get/${data.id}`);
        if (!postRes.ok) return;

        const post = await postRes.json();
        const [userRes, habitRes] = await Promise.all([
            fetchWithRetry(`/api/users/get/${post.userId}`),
            fetchWithRetry(`/api/habits/get/${post.habitId}`)
        ]);

        if (!userRes.ok || !habitRes.ok) return;

        const user = await userRes.json();
        const habit = await habitRes.json();

        const postDiv = document.createElement('div');
        postDiv.className = 'post-item';
        postDiv.dataset.postId = post.id;

        const header = document.createElement('div');
        header.className = 'post-header';

        const userLink = document.createElement('a');
        userLink.textContent = `${user.surname} ${user.name}`;
        userLink.href = `/profile/${post.userId}`;
        userLink.className = 'user-profile-link';
        userLink.style.marginRight = '4px';

        const postInfo = document.createTextNode(` • ${habit.goal} • ${new Date(post.dateTime).toLocaleString()}`);

        header.appendChild(userLink);
        header.appendChild(postInfo);

        const text = document.createElement('div');
        text.className = 'post-text';
        text.textContent = post.text;

        const mediaDiv = document.createElement('div');
        mediaDiv.className = 'post-media';
        post.mediaFilesUrl.forEach(url => {
            const img = document.createElement('img');
            img.src = url;
            mediaDiv.appendChild(img);
        });

        const actionsDiv = document.createElement('div');
        actionsDiv.className = 'post-actions';

        const likeBtn = document.createElement('button');
        likeBtn.textContent = `🤍 ${post.likesCount}`;
        likeBtn.onclick = async () => {
            const res = await fetchWithRetry(`/api/posts/add/${post.id}/like`, {
                method: 'POST'
            });

            if (res.ok) {
                likeBtn.textContent = `❤️ ${post.likesCount + 1}`;
                likesCount.textContent = parseInt(likesCount.textContent) + 1;
            }
        };

        const commentToggleBtn = document.createElement('button');
        commentToggleBtn.textContent = `💬 ${post.commentsCount}`;
        commentToggleBtn.className = 'comments-btn';
        const commentSection = document.createElement('div');
        commentSection.className = 'comments-container';

        commentToggleBtn.onclick = async () => {
            commentSection.style.display = commentSection.style.display === 'block' ? 'none' : 'block';
            if (commentSection.style.display === 'block' && commentSection.innerHTML === '') {
                await loadComments(post.id, commentSection, post);
            }
        };

        const deleteBtn = document.createElement('button');
        deleteBtn.textContent = '🗑️ Удалить';
        deleteBtn.className = 'delete';
        deleteBtn.onclick = () => {
            showConfirm(
                'Удаление поста',
                'Вы уверены, что хотите удалить этот пост?',
                async () => {
                    const res = await fetchWithRetry(`/api/posts/delete/${post.id}`, {
                        method: 'DELETE'
                    });

                    if (res.ok) {
                        postDiv.remove();
                        postsCount.textContent = parseInt(postsCount.textContent) - 1;
                        likesCount.textContent = parseInt(likesCount.textContent) - post.likesCount;
                        commentsCount.textContent = parseInt(commentsCount.textContent) - post.commentsCount;
                    }
                }
            );
        };

        actionsDiv.append(likeBtn, commentToggleBtn, deleteBtn);
        postDiv.append(header, text, mediaDiv, actionsDiv, commentSection);

        container.prepend(postDiv);
        postsCount.textContent = parseInt(postsCount.textContent) + 1;
    } else {
        showNotification('Ошибка при добавлении поста', false);
    }
};

document.addEventListener('DOMContentLoaded', () => {
    if (window.location.pathname.startsWith('/profile/')) {
        loadProfile();
    }

    window.onclick = (e) => {
        if (e.target.classList.contains('modal')) {
            e.target.style.display = 'none';
        }
    };
});