const runtimeApiBase = (globalThis as any)?.__env?.apiBase as string | undefined;

export const environment = {
  production: true,
  // Update this to your production API, e.g. behind API Gateway / ALB (can be overridden by window.__env.apiBase)
  apiBase: runtimeApiBase || 'https://mkxrivn8wy.eu-central-1.awsapprunner.com/api/storage',
};
