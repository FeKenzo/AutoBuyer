import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'monitoramentos'
  },
  {
    path: 'monitoramentos',
    loadComponent: () =>
      import(
        './features/product-targets/product-targets-page.component'
      ).then(component =>
        component.ProductTargetsPageComponent
      )
  },
  {
    path: 'promocoes',
    loadComponent: () =>
      import(
        './features/promotions/promotions-page.component'
      ).then(component =>
        component.PromotionsPageComponent
      )
  },
  {
    path: '**',
    redirectTo: 'monitoramentos'
  }
];