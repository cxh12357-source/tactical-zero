mergeInto(LibraryManager.library, {
  TZ18MenuState: function(ready) {
    window.tz18AtMenu = !!ready;
    window.dispatchEvent(new Event('tz18-phase'));
  }
});
