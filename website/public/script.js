// script.js - Simulated alert generator and system status check

// Simulated alert generator
function displayAlert(alert) {
  const alertList = document.getElementById('alertList');
  const div = document.createElement('div');
  div.className = 'alert';
  div.innerHTML = `<strong>[${alert.timestamp}]</strong> ${alert.message} (${alert.severity})`;
  alertList.prepend(div);
}

// Simulate incoming alerts every 10 seconds
setInterval(() => {
  const alert = {
    timestamp: new Date().toLocaleTimeString(),
    message: 'Motion detected in hallway',
    severity: 'Medium'
  };
  displayAlert(alert);
}, 10000);

// Simulated status check (can replace with fetch later)
document.getElementById('cameraStatus').textContent = 'Simulated Connected';
document.getElementById('networkStatus').textContent = 'Simulated Connected';
