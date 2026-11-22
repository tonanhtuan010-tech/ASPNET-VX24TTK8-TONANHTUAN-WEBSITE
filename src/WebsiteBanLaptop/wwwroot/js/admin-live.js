(function () {
  'use strict';
  const alert = document.getElementById('admin-live');
  if (!alert || !window.signalR) return;

  let hideTimer;
  function notify(message, variant) {
    clearTimeout(hideTimer);
    alert.textContent = message;
    alert.className = 'alert py-2 alert-' + variant;
    alert.hidden = false;
    hideTimer = setTimeout(() => { alert.hidden = true; }, 8000);
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/adminHub')
    .withAutomaticReconnect()
    .build();

  connection.on('OrderCreated', message => {
    notify('Có đơn hàng mới #' + message.id + '.', 'success');
  });
  connection.on('OrderUpdated', message => {
    notify('Đơn #' + message.id + ': ' + message.status, 'info');
  });
  connection.start().catch(() => {});
})();
