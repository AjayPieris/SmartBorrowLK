$(document).ready(function () {
    if (typeof window.chatConfig === 'undefined' || window.chatConfig.currentUserId === 0) return;

    let currentChatPartnerId = null;
    let isChatOpen = false;

    const pusher = new Pusher(window.chatConfig.pusherKey, {
        cluster: window.chatConfig.pusherCluster,
        encrypted: true
    });

    const channel = pusher.subscribe(`chat-${window.chatConfig.currentUserId}`);

    channel.bind('new-message', function (data) {
        if (isChatOpen && currentChatPartnerId == data.senderId) {
            // Chat is open with this user, append message
            appendMessage(data, false);
            scrollToBottom();
            // Optional: Send a read receipt here if implemented
        } else {
            // Show badge
            $('#globalChatBadge').removeClass('d-none');
            // Play sound or show toast notification if desired
        }
    });

    // Check for unread on load
    $.get('/api/chat/unread', function (res) {
        if (res.count > 0) {
            $('#globalChatBadge').removeClass('d-none');
        }
    });

    // Open chat from Contact Seller button
    $('.open-chat-btn').on('click', function () {
        const ownerId = $(this).data('owner-id');
        if (!ownerId) return;
        openChatWindow(ownerId);
    });

    // Navbar toggle behavior (Opens last person or blank)
    $('#navbarChatBtn').on('click', function () {
        if (isChatOpen) {
            closeChatWindow();
        } else {
            // If they click the navbar icon, just open the window.
            // If they haven't selected anyone, it shows the blank state.
            $('#globalChatWidget').addClass('active');
            isChatOpen = true;
            $('#globalChatBadge').addClass('d-none'); // Optimistically clear
        }
    });

    $('#closeChatBtn').on('click', function () {
        closeChatWindow();
    });

    function closeChatWindow() {
        $('#globalChatWidget').removeClass('active');
        isChatOpen = false;
    }

    function openChatWindow(partnerId) {
        if (currentChatPartnerId !== partnerId) {
            currentChatPartnerId = partnerId;
            loadChatHistory(partnerId);
        }
        $('#globalChatWidget').addClass('active');
        isChatOpen = true;
    }

    function loadChatHistory(partnerId) {
        $('#chatBody').html('<div class="text-center mt-4"><div class="spinner-border text-primary spinner-border-sm" role="status"></div></div>');
        $('#chatInputMessage').prop('disabled', true);
        $('#chatSendBtn').prop('disabled', true);

        $.get(`/api/chat/history/${partnerId}`, function (res) {
            const receiver = res.receiver;
            const messages = res.messages;

            // Updated header
            $('#chatPartnerName').text(receiver.name);
            if (receiver.profileImageUrl) {
                $('#chatPartnerAvatar').html(`<img src="${receiver.profileImageUrl}" alt="${receiver.name}"/>`);
            } else {
                $('#chatPartnerAvatar').html((receiver.name ? receiver.name.charAt(0).toUpperCase() : '?'));
            }

            // Render Messages
            const $body = $('#chatBody');
            $body.empty();

            if (messages.length === 0) {
                $body.append('<div class="text-center text-muted mt-5 mb-5" style="font-size: 0.9rem;">Start the conversation</div>');
            } else {
                messages.forEach(msg => {
                    const isSent = msg.senderId == window.chatConfig.currentUserId;
                    appendMessage(msg, isSent);
                });
                scrollToBottom();
            }

            $('#chatInputMessage').prop('disabled', false).focus();
            $('#chatSendBtn').prop('disabled', false);

        }).fail(function () {
            $('#chatBody').html('<div class="text-center text-danger mt-4">Failed to load messages</div>');
        });
    }

    function appendMessage(msg, isSent) {
        const timeStr = new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const bubbleClass = isSent ? 'sent' : 'received';
        const el = `
            <div class="chat-message ${bubbleClass}">
                ${escapeHtml(msg.content)}
                <span class="chat-message-time">${timeStr}</span>
            </div>
        `;
        $('#chatBody').append(el);
    }

    function scrollToBottom() {
        const body = document.getElementById('chatBody');
        body.scrollTop = body.scrollHeight;
    }

    function escapeHtml(unsafe) {
        return (unsafe || '').toString()
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Send Message
    function sendMessage() {
        if (!currentChatPartnerId) return;
        const info = $('#chatInputMessage').val().trim();
        if (!info) return;

        $('#chatInputMessage').val('');
        
        // Optimistic UI append
        const tempMsg = {
            content: info,
            createdAt: new Date().toISOString(),
            senderId: window.chatConfig.currentUserId
        };
        appendMessage(tempMsg, true);
        scrollToBottom();

        $.ajax({
            url: '/api/chat/send',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                receiverId: currentChatPartnerId,
                content: info
            }),
            success: function () {
                // Sent successfully - handled optimistically.
                // Could update status ticks here if implemented.
            },
            error: function () {
                alert("Failed to send message.");
            }
        });
    }

    $('#chatSendBtn').on('click', sendMessage);
    $('#chatInputMessage').on('keypress', function (e) {
        if (e.which === 13) {
            sendMessage();
        }
    });

});
