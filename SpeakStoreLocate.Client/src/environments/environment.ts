const runtimeApiBase = (globalThis as any)?.__env?.apiBase as string | undefined;

export const environment = {
  production: false,
  // Local API base for development (can be overridden by window.__env.apiBase)
  apiBase: runtimeApiBase || 'http://localhost:5471/api/storage',
};
