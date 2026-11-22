
(function ($) {
  'use strict';
  if (!$) return;
  $(document).on('submit', '#cart-content .js-cart-form', function (event) {
    event.preventDefault();
    const form = this;
    const $form = $(form);
    $form.find('button[type="submit"]').prop('disabled', true);
    $.ajax({
      url: form.action,
      type: 'POST',
      data: $form.serialize(),
      headers: { 'X-Requested-With': 'XMLHttpRequest' },
      cache: false
    }).done(function (html, textStatus, xhr) {
      $('#cart-content').html(html);
      const count = xhr.getResponseHeader('X-Cart-Count');
      if (count !== null) $('.cart-badge').text(count);
    }).fail(function () {
      form.submit();
    }).always(function () {
      $form.find('button[type="submit"]').prop('disabled', false);
    });
  });
})(window.jQuery);
