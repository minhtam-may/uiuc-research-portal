export const environment = {
  production: true,
  // Relative path: works when the frontend is served from behind the same
  // reverse proxy/origin as the API. For a one-off check (e.g. Codespaces
  // port forwarding), run `ng serve` with `development` config instead, or
  // override this value with the forwarded API URL before building.
  apiBaseUrl: '/api'
};
