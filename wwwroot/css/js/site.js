function toggleLike(activityId) {
    fetch('/Activity/Like', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value },
        body: `activityId=${activityId}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            const btn = document.getElementById(`like-btn-${activityId}`);
            btn.textContent = `Thích (${data.likesCount})`;
        } else {
            alert(data.message);
        }
    });
}

function toggleComments(activityId) {
    const section = document.getElementById(`comment-section-${activityId}`);
    section.style.display = section.style.display === 'none' ? 'block' : 'none';
}

function submitComment(activityId) {
    const content = document.getElementById(`comment-content-${activityId}`).value;
    fetch('/Activity/Comment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value },
        body: `activityId=${activityId}&content=${encodeURIComponent(content)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            const commentsDiv = document.getElementById(`comments-${activityId}`);
            commentsDiv.innerHTML += `<p>${data.comment.userName} (${data.comment.createdAt}): ${data.comment.content}</p>`;
            document.getElementById(`comment-content-${activityId}`).value = '';
        } else {
            alert(data.message);
        }
    });
}