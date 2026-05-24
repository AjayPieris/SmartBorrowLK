$(document).ready(function () {
  if (
    typeof window.chatConfig === "undefined" ||
    window.chatConfig.currentUserId === 0
  )
    return;

  let currentChatPartnerId = null;
  let isChatOpen = false;

  const pusher = new Pusher(window.chatConfig.pusherKey, {
    cluster: window.chatConfig.pusherCluster,
    encrypted: true,
  });

  const channel = pusher.subscribe(`chat-${window.chatConfig.currentUserId}`);

  channel.bind("new-message", function (data) {
    if (isChatOpen && currentChatPartnerId == data.senderId) {
      // Active chat is open with sender
      appendMessage(data, false);
      scrollToBottom();

      // Refresh to mark as read and update global count
      refreshConversations();
    } else {
      // Update global badge
      let countSpan = $("#chatUnreadCount");
      let current = parseInt(countSpan.text()) || 0;
      countSpan.text(current + 1);
      $("#globalChatBadge").removeClass("d-none");

      // Play sound or show toast notification like WhatsApp
      let preview =
        data.content && data.content.length > 30
          ? data.content.substring(0, 30) + "..."
          : data.content || "New Message!";
      $("#toastMessageContent").text("New message: " + preview);
      let toastEl = new bootstrap.Toast(
        document.getElementById("chatToneToast"),
      );
      toastEl.show();

      // Refresh conversation list to show new badge locally
      refreshConversations();
    }
  });

  // Initial Load
  refreshConversations();

  function refreshConversations() {
    $.get("/api/chat/conversations", function (res) {
      const list = $("#chatConversationsList");
      list.empty();
      let totalUnread = 0;

      if (!res || res.length === 0) {
        list.append(
          '<li class="p-3 text-center text-muted" style="font-size: 0.9rem;">No conversations yet</li>',
        );
      } else {
        res.forEach((c) => {
          totalUnread += c.unreadCount;
          const isActive = c.partnerId == currentChatPartnerId ? "active" : "";
          let avatarHtml = c.partnerAvatar
            ? `<img src="${c.partnerAvatar}" alt="${escapeHtml(c.partnerName)}">`
            : escapeHtml(c.partnerName.charAt(0).toUpperCase());

          const timeStr = new Date(c.lastMessageTime).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
          });

          const el = `
                    <li class="chat-conversation-item ${isActive}" data-id="${c.partnerId}" data-name="${escapeHtml(c.partnerName)}" data-avatar="${c.partnerAvatar || ""}">
                        <div class="conversation-avatar">${avatarHtml}</div>
                        <div class="conversation-details">
                            <div class="conversation-name-row">
                                <span class="conversation-name">${escapeHtml(c.partnerName)}</span>
                                <span class="conversation-time">${timeStr}</span>
                            </div>
                            <div class="conversation-msg-row">
                                <p class="conversation-last-msg">${escapeHtml(c.lastMessage)}</p>
                                ${c.unreadCount > 0 ? `<span class="conversation-unread-badge">${c.unreadCount}</span>` : ""}
                            </div>
                        </div>
                    </li>
                    `;
          list.append(el);
        });
      }

      if (totalUnread > 0) {
        $("#chatUnreadCount").text(totalUnread);
        $("#globalChatBadge").removeClass("d-none");
      } else {
        $("#globalChatBadge").addClass("d-none");
      }

      // Bind click
      $(".chat-conversation-item")
        .off("click")
        .on("click", function () {
          $(".chat-conversation-item").removeClass("active");
          $(this).addClass("active");
          openActiveChat(
            $(this).data("id"),
            $(this).data("name"),
            $(this).data("avatar"),
          );
        });
    }).fail(function () {
      $("#chatConversationsList").html(
        '<li class="p-3 text-center text-danger" style="font-size: 0.9rem;">Failed to load chats</li>',
      );
    });
  }

  // Open chat from Contact Seller button on Listing Page
  $(".open-chat-btn").on("click", function () {
    const ownerId = $(this).data("owner-id");
    if (!ownerId) return;

    $("#globalChatWidget").addClass("active");
    isChatOpen = true;

    // Optimistically open chat
    openActiveChat(ownerId, "Vendor", null);

    refreshConversations();
  });

  // Navbar toggle behavior
  $("#navbarChatBtn").on("click", function () {
    if (isChatOpen) {
      closeWidget();
    } else {
      $("#globalChatWidget").addClass("active");
      isChatOpen = true;
      refreshConversations();
    }
  });

  $("#closeSidebarBtn").on("click", function () {
    closeWidget();
  });

  $("#closeEmptyStateBtn").on("click", function () {
    closeWidget();
  });

  $("#closeChatBtn").on("click", function () {
    closeActiveChat();
  });

  $("#chatBackBtn").on("click", function () {
    closeActiveChat();
  });

  function closeWidget() {
    $("#globalChatWidget").removeClass("active");
    isChatOpen = false;
  }

  function closeActiveChat() {
    currentChatPartnerId = null;
    $("#chatActiveState").addClass("d-none");
    $("#chatEmptyState").removeClass("d-none");
    $(".chat-conversation-item").removeClass("active");
    $("#globalChatWidget").removeClass("mobile-active-chat");
  }

  function openActiveChat(partnerId, partnerName, partnerAvatar) {
    currentChatPartnerId = partnerId;

    $("#chatEmptyState").addClass("d-none");
    $("#chatActiveState").removeClass("d-none");
    $("#globalChatWidget").addClass("mobile-active-chat");
    $("#chatBody").html(
      '<div class="text-center mt-4"><div class="spinner-border text-primary spinner-border-sm" role="status"></div></div>',
    );
    $("#chatInputMessage").prop("disabled", true);
    $("#chatSendBtn").prop("disabled", true);

    $.get(`/api/chat/history/${partnerId}`, function (res) {
      const receiver = res.receiver;
      const messages = res.messages;

      // Updated header
      $("#chatPartnerName").text(receiver.name || fallbackName);
      if (receiver.profileImageUrl || fallbackAvatar) {
        $("#chatPartnerAvatar").html(
          `<img src="${receiver.profileImageUrl || fallbackAvatar}" alt="${escapeHtml(receiver.name || fallbackName)}"/>`,
        );
      } else {
        $("#chatPartnerAvatar").html(
          escapeHtml(
            (receiver.name || fallbackName || "?").charAt(0).toUpperCase(),
          ),
        );
      }

      // Render Messages
      const $body = $("#chatBody");
      $body.empty();

      if (messages.length === 0) {
        $body.append(
          '<div class="text-center text-muted mt-5 mb-5" style="font-size: 0.9rem;">Start the conversation</div>',
        );
      } else {
        messages.forEach((msg) => {
          const isSent = msg.senderId == window.chatConfig.currentUserId;
          appendMessage(msg, isSent);
        });
        scrollToBottom();
      }

      $("#chatInputMessage").prop("disabled", false).focus();
      $("#chatSendBtn").prop("disabled", false);

      // Refetch conversations just to update badges globally since we read messages
      refreshConversations();
    }).fail(function () {
      $("#chatBody").html(
        '<div class="text-center text-danger mt-4">Failed to load messages</div>',
      );
    });
  }

  function appendMessage(msg, isSent) {
    const timeStr = new Date(msg.createdAt).toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });
    const bubbleClass = isSent ? "sent" : "received";
    const el = `
            <div class="chat-message ${bubbleClass}">
                ${escapeHtml(msg.content)}
                <span class="chat-message-time">${timeStr}</span>
            </div>
        `;
    $("#chatBody").append(el);
  }

  function scrollToBottom() {
    const body = document.getElementById("chatBody");
    body.scrollTop = body.scrollHeight;
  }

  function escapeHtml(unsafe) {
    return (unsafe || "")
      .toString()
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  // Send Message
  function sendMessage() {
    if (!currentChatPartnerId) return;
    const info = $("#chatInputMessage").val().trim();
    if (!info) return;

    $("#chatInputMessage").val("");

    // Optimistic UI append
    const tempMsg = {
      content: info,
      createdAt: new Date().toISOString(),
      senderId: window.chatConfig.currentUserId,
    };
    appendMessage(tempMsg, true);
    scrollToBottom();

    $.ajax({
      url: "/api/chat/send",
      type: "POST",
      contentType: "application/json",
      data: JSON.stringify({
        receiverId: currentChatPartnerId,
        content: info,
      }),
      success: function () {
        // Sent successfully - refresh to push conversation to top
        refreshConversations();
      },
      error: function () {
        alert("Failed to send message.");
      },
    });
  }

  $("#chatSendBtn").on("click", sendMessage);
  $("#chatInputMessage").on("keypress", function (e) {
    if (e.which === 13) {
      sendMessage();
    }
  });
});
