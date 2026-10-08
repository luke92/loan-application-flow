const express = require('express');

const app = express();
app.use(express.json());

// Keyed by SSN so a PUT (returning customer) naturally overwrites the previous record,
// mirroring the "one customer per SSN" invariant the backend enforces.
const customers = new Map();

app.post('/customers', (req, res) => {
  const payload = req.body;

  if (!payload?.ssn) {
    return res.status(400).json({ error: 'ssn is required.' });
  }

  customers.set(payload.ssn, {
    ...payload,
    lastAction: 'created',
    receivedAt: new Date().toISOString(),
  });

  console.log(`[mock-external-service] POST /customers -> created ${payload.ssn}`);
  res.status(200).json({ received: true });
});

app.put('/customers/:ssn', (req, res) => {
  const { ssn } = req.params;
  const payload = req.body;

  customers.set(ssn, {
    ...payload,
    lastAction: 'updated',
    receivedAt: new Date().toISOString(),
  });

  console.log(`[mock-external-service] PUT /customers/${ssn} -> updated`);
  res.status(200).json({ received: true });
});

app.get('/customers', (_req, res) => {
  res.status(200).json(Array.from(customers.values()));
});

const port = process.env.PORT || 4000;
app.listen(port, () => {
  console.log(`[mock-external-service] listening on port ${port}`);
});
