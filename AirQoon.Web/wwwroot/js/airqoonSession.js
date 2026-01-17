window.airqoonSession = {
  get: function (key) {
    try {
      return window.localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set: function (key, value) {
    try {
      window.localStorage.setItem(key, value);
      return true;
    } catch {
      return false;
    }
  },
  remove: function (key) {
    try {
      window.localStorage.removeItem(key);
      return true;
    } catch {
      return false;
    }
  },
  downloadTextFile: function (filename, content) {
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
};
