const express = require('express');
const path = require('path');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();
const port = 3000;

// Serve frontend
app.use(express.static(path.join(__dirname, '../public')));

//proxy flask endpoints
app.use('/status', createProxyMiddleware({ target: 'http://localhost:5001', changeOrigin: true }));
app.use('/snapshot', createProxyMiddleware({ target: 'http://localhost:5001', changeOrigin: true }));

app.listen(port, () => {
  console.log(`Web server running at http://localhost:${port}`);
});
