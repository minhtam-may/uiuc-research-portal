import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./portal/portal.routes').then((m) => m.PORTAL_ROUTES)
  }
];
