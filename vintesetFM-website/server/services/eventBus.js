import { EventEmitter } from 'events';

class ReiDaMesaEventBus extends EventEmitter {}

// Uma instância global compartilhada
export const reiDaMesaEvents = new ReiDaMesaEventBus();
