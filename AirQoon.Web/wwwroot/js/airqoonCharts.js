window.airqoonCharts = (() => {
  const charts = new Map();

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
  }

  return { renderLineChart, destroy };
})();
