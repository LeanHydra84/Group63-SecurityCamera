const express = require('express');
const path = require('path');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();
const port = 3000;

// Serve frontend
app.use(express.static(path.join(__dirname, 'public')));

// Proxy API routes to C# backend (ASP.NET Core on localhost:5000)
app.use('/account', createProxyMiddleware({ target: 'http://localhost:5000', changeOrigin: true }));
app.use('/camera', createProxyMiddleware({ target: 'http://localhost:5000', changeOrigin: true }));
app.use('/session', createProxyMiddleware({ target: 'http://localhost:5000', changeOrigin: true }));

app.listen(port, () => {
  console.log(`Frontend running at http://localhost:${port}`);
});

app.get('/list', (req, res) => {
  res.redirect('/list_page.html')
})

app.get('/view/:someString', (req, res) => {
  const someString = req.params.someString
  res.redirect(`/viewer.html?cam=${encodeURIComponent(someString)}`)
})