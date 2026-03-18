import { loadEnv } from 'vite';
const envs = loadEnv('development', process.cwd());
console.log("VITE ENVS CARREGADAS:", envs);
