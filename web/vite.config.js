import { existsSync, readFileSync } from 'node:fs';
import { defineConfig } from 'vite';

const certificatePath = process.env.VITE_DEV_TLS_CERT;
const keyPath = process.env.VITE_DEV_TLS_KEY;
const hasTls = certificatePath && keyPath && existsSync(certificatePath) && existsSync(keyPath);

export default defineConfig({
  server: {
    host: '127.0.0.1',
    https: hasTls ? {
      cert: readFileSync(certificatePath),
      key: readFileSync(keyPath)
    } : undefined
  }
});
