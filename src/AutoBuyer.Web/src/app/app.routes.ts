import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'painel',
  },
  {
    path: 'painel',
    title: 'Visão geral | AutoBuyer',
    loadComponent: () =>
      import('./features/dashboard/dashboard-page.component').then(
        (component) => component.DashboardPageComponent,
      ),
  },
  {
    path: 'monitoramentos',
    title: 'Monitoramentos | AutoBuyer',
    loadComponent: () =>
      import('./features/product-targets/product-targets-page.component').then(
        (component) => component.ProductTargetsPageComponent,
      ),
  },
  {
    path: 'promocoes',
    title: 'Promoções | AutoBuyer',
    loadComponent: () =>
      import('./features/promotions/promotions-page.component').then(
        (component) => component.PromotionsPageComponent,
      ),
  },
  {
    path: '**',
    redirectTo: 'painel',
  },
];
