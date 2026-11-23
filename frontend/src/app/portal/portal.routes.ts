import { Route } from '@angular/router';

export const PORTAL_ROUTES: Route[] = [
  {
    path: '',
    loadComponent: () => import('./features/home/portal-home.component').then((c) => c.PortalHomeComponent)
  }
];
