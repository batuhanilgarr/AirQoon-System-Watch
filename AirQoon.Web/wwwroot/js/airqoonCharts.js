window.airqoonCharts = (() => {
  const charts = new Map();

  function has(canvasId) {
    return charts.has(canvasId);
  }

  function count() {
    return charts.size;
  }

  function getInfo(canvasId) {
    const canvas = document.getElementById(canvasId);
    const rect = canvas ? canvas.getBoundingClientRect() : null;
    return {
      canvasId,
      exists: !!canvas,
      chart: charts.has(canvasId),
      width: rect ? rect.width : null,
      height: rect ? rect.height : null,
      renderedFlag: canvas ? canvas.dataset.aqRendered : null
    };
  }

  function ensureCanvas(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
      throw new Error(`Canvas not found: ${canvasId}`);
    }
    return canvas;
  }

  function destroy(canvasId) {
    const existing = charts.get(canvasId);
    if (existing) {
      existing.destroy();
      charts.delete(canvasId);
    }
  }

  function renderLineChart(canvasId, labels, values, label) {
    const canvas = ensureCanvas(canvasId);
    destroy(canvasId);

    canvas.style.width = '100%';
    canvas.style.height = '100%';

    const ctx = canvas.getContext('2d');
    const chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: label,
          data: values,
          borderWidth: 2,
          tension: 0.25
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: true }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    charts.set(canvasId, chart);
    canvas.dataset.aqRendered = '1';

    requestAnimationFrame(() => {
      try {
        chart.resize();
        chart.update('none');
      } catch (_) {
      }
    });
  }

  function renderPieChart(canvasId, labels, values, title) {
    const canvas = ensureCanvas(canvasId);
    destroy(canvasId);

    canvas.style.width = '100%';
    canvas.style.height = '100%';

    const ctx = canvas.getContext('2d');
    const palette = [
      '#3B82F6', // blue
      '#10B981', // green
      '#F59E0B', // amber
      '#06B6D4', // cyan
      '#EF4444', // red
      '#8B5CF6', // purple
      '#64748B'  // slate
    ];

    const bg = labels.map((_, idx) => palette[idx % palette.length]);

    const chart = new Chart(ctx, {
      type: 'pie',
      data: {
        labels: labels,
        datasets: [{
          label: title || '',
          data: values,
          backgroundColor: bg,
          borderColor: 'rgba(255,255,255,0.9)',
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: true },
          title: title ? { display: false, text: title } : undefined
        }
      }
    });

    charts.set(canvasId, chart);
    canvas.dataset.aqRendered = '1';

    requestAnimationFrame(() => {
      try {
        chart.resize();
        chart.update('none');
      } catch (_) {
      }
    });
  }

  function renderMultiLineChart(canvasId, labels, series, title) {
    const canvas = ensureCanvas(canvasId);
    destroy(canvasId);

    canvas.style.width = '100%';
    canvas.style.height = '100%';

    const ctx = canvas.getContext('2d');

    const palette = [
      '#3B82F6', // blue
      '#10B981', // green
      '#F59E0B', // amber
      '#06B6D4', // cyan
      '#EF4444', // red
      '#8B5CF6'  // purple
    ];

    const keys = Object.keys(series || {});
    const datasets = keys.map((k, idx) => ({
      label: k,
      data: series[k] || [],
      borderWidth: 2,
      tension: 0.25,
      borderColor: palette[idx % palette.length],
      backgroundColor: palette[idx % palette.length],
      pointRadius: 2
    }));

    const chart = new Chart(ctx, {
      type: 'line',
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: true },
          title: title ? { display: false, text: title } : undefined
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    charts.set(canvasId, chart);

    canvas.dataset.aqRendered = '1';

    requestAnimationFrame(() => {
      try {
        chart.resize();
        chart.update('none');
      } catch (_) {
      }
    });
  }

  function renderBarChart(canvasId, labels, values, title) {
    const canvas = ensureCanvas(canvasId);
    destroy(canvasId);

    canvas.style.width = '100%';
    canvas.style.height = '100%';

    const ctx = canvas.getContext('2d');
    const chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [{
          label: title || '',
          data: values,
          borderWidth: 1,
          backgroundColor: 'rgba(59, 130, 246, 0.25)',
          borderColor: 'rgba(59, 130, 246, 0.9)'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    charts.set(canvasId, chart);

    canvas.dataset.aqRendered = '1';

    requestAnimationFrame(() => {
      try {
        chart.resize();
        chart.update('none');
      } catch (_) {
      }
    });
  }

  return { renderLineChart, renderMultiLineChart, renderBarChart, renderPieChart, destroy, has, count, getInfo };
})();
