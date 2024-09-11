const express = require('express');
const bodyParser = require('body-parser');
const app = express();
const port = 3000;

app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

// Serve static files
app.use(express.static('public'));

// GET endpoint
app.get('/api/data', (req, res) => {
  res.json({ message: 'This is a GET response' });
});

// POST endpoint
app.post('/api/data', (req, res) => {
  console.log('Received POST data:', req.body);
  res.json({ message: 'POST request received', data: req.body });
});

app.listen(port, () => {
  console.log(`Server running at http://localhost:${port}`);
});