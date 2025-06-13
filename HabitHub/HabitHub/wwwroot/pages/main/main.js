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

document.getElementById('refresh-btn').onclick = () => renderPosts();
const currentUserId = localStorage.getItem('userId');

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
        console.error(result.message);
    } catch (e) {
        console.error('Не удалось распарсить ошибку от API', e);
    }
}

async function renderPosts() {
    const container = document.getElementById('post-feed');
    container.innerHTML = '';

    let posts;
    try {
        const res = await fetchWithRetry('http://localhost:5000/api/posts/get/all');
        if (!res.ok) {
            await parseApiError(res);
            return;
        }
        posts = await res.json();
    } catch (e) {
        console.error('Ошибка загрузки постов', e);
        return;
    }

    for (const post of posts) {
        const postDiv = document.createElement('div');
        postDiv.className = 'post-item';

        let user, habit;
        try {
            const [userRes, habitRes] = await Promise.all([
                fetchWithRetry(`http://localhost:5000/api/users/get/${post.userId}`),
                fetchWithRetry(`http://localhost:5000/api/habits/get/${post.habitId}`)
            ]);

            if (!userRes.ok || !habitRes.ok) {
                if (!userRes.ok) await parseApiError(userRes);
                if (!habitRes.ok) await parseApiError(habitRes);
                continue;
            }

            user = await userRes.json();
            habit = await habitRes.json();
        } catch (e) {
            console.error('Ошибка загрузки пользователя или привычки', e);
            continue;
        }
        const habitName = habit.goal;

        const header = document.createElement('div');
        header.className = 'post-header';
        const userLink = document.createElement('a');
        userLink.textContent = `${user.surname} ${user.name}`;
        userLink.href = `/profile/${post.userId}`;
        userLink.className = 'user-profile-link';
        userLink.style.cursor = 'pointer';
        userLink.style.marginRight = '4px';

        const postInfo = document.createTextNode(` • ${habitName} • ${new Date(post.dateTime).toLocaleString()}`);

        header.appendChild(userLink);
        header.appendChild(postInfo);

        const text = document.createElement('div');
        text.className = 'post-text';
        text.textContent = post.text;

        const likeBtn = document.createElement('button');
        likeBtn.textContent = `${post.didUserLiked ? '❤️' : '🤍'} ${post.likesCount}`;
        likeBtn.onclick = async () => {
            try {
                const method = post.didUserLiked ? 'DELETE' : 'POST';
                const res = await fetchWithRetry(`http://localhost:5000/api/posts/${post.didUserLiked ? 'delete' : 'add'}/${post.id}/like`, {
                    method
                });

                if (!res.ok) {
                    await parseApiError(res);
                    return;
                }

                post.didUserLiked = !post.didUserLiked;
                post.likesCount += post.didUserLiked ? 1 : -1;
                likeBtn.textContent = `${post.didUserLiked ? '❤️' : '🤍'} ${post.likesCount}`;
            } catch (e) {
                console.error('Ошибка при работе с лайком', e);
            }
        };

        const commentToggleBtn = document.createElement('button');
        commentToggleBtn.textContent = `Комментарии (${post.commentsCount})`;
        let commentsVisible = false;
        const commentSection = document.createElement('div');

        commentToggleBtn.onclick = async () => {
            commentsVisible = !commentsVisible;
            commentSection.style.display = commentsVisible ? 'block' : 'none';

            if (commentsVisible && commentSection.innerHTML === '') {
                await loadComments(post.id, commentSection, post);
            }
        };

        const mediaDiv = document.createElement('div');
        mediaDiv.className = 'post-media';
        for (const url of post.mediaFilesUrl) {
            const img = document.createElement('img');
            img.src = url;
            mediaDiv.appendChild(img);
        }

        postDiv.append(header, text, mediaDiv, likeBtn, commentToggleBtn, commentSection);
        container.appendChild(postDiv);
    }
}

async function loadComments(postId, commentSection, post) {
    try {
        const res = await fetchWithRetry(`http://localhost:5000/api/posts/get/${postId}/comments`);
        if (!res.ok) {
            await parseApiError(res);
            return;
        }

        const comments = await res.json();

        let currentUser = null;
        if (currentUserId) {
            const userRes = await fetchWithRetry(`http://localhost:5000/api/users/get/${currentUserId}`);
            if (userRes.ok) {
                currentUser = await userRes.json();
            }
        }

        const ul = document.createElement('ul');
        for (const comment of comments) {
            const userRes = await fetchWithRetry(`http://localhost:5000/api/users/get/${comment.userId}`);
            const user = userRes.ok ? await userRes.json() : null;
            const userName = user ? `${user.surname} ${user.name}` : 'Неизвестный пользователь';

            const li = createCommentElement(comment, userName);
            ul.appendChild(li);
        }

        commentSection.innerHTML = '';
        commentSection.appendChild(ul);

        const commentInputDiv = document.createElement('div');
        commentInputDiv.className = 'comment-section';

        const input = document.createElement('input');
        input.type = 'text';
        input.placeholder = 'Оставить комментарий...';

        const addBtn = document.createElement('button');
        addBtn.textContent = 'Отправить';
        addBtn.onclick = async () => {
            try {
                const commentText = input.value.trim();
                if (!commentText) return;

                const res = await fetchWithRetry(`http://localhost:5000/api/posts/add/${postId}/comment`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ comment: commentText })
                });

                if (!res.ok) {
                    await parseApiError(res);
                    return;
                }

                const { id: newCommentId } = await res.json();

                const newComment = {
                    id: newCommentId,
                    text: commentText,
                    userId: currentUserId,
                    dateTime: new Date().toISOString()
                };

                const userName = currentUser ? `${currentUser.surname} ${currentUser.name}` : 'Вы';

                const li = createCommentElement(newComment, userName);
                ul.prepend(li);

                input.value = '';
                post.commentsCount++;
                commentToggleBtn.textContent = `Комментарии (${post.commentsCount})`;
            } catch (e) {
                console.error('Ошибка добавления комментария', e);
            }
        };

        commentInputDiv.append(input, addBtn);
        commentSection.appendChild(commentInputDiv);

    } catch (e) {
        console.error('Ошибка загрузки комментариев', e);
        commentSection.textContent = 'Ошибка загрузки комментариев';
    }
}

function createCommentElement(comment, userName) {
    const li = document.createElement('li');
    li.dataset.commentId = comment.id;
    li.textContent = `${new Date(comment.dateTime).toLocaleString()} — ${userName}: ${comment.text}`;

    if (comment.userId === currentUserId) {
        const delBtn = document.createElement('button');
        delBtn.textContent = 'Удалить';
        delBtn.className = 'delete-comment';
        delBtn.onclick = async () => {
            try {
                const res = await fetchWithRetry(`http://localhost:5000/api/posts/delete/comments/${comment.id}`, {
                    method: 'DELETE'
                });
                if (!res.ok) {
                    await parseApiError(res);
                    return;
                }

                li.remove();
                post.commentsCount--;
                commentToggleBtn.textContent = `Комментарии (${post.commentsCount})`;
            } catch (e) {
                console.error('Ошибка удаления комментария', e);
            }
        };
        li.appendChild(delBtn);
    }

    return li;
}

renderPosts();