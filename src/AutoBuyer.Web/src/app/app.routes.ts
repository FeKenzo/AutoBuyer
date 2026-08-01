import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import(
        './features/product-targets/product-targets-page.component'
      ).then(component =>
        component.ProductTargetsPageComponent
      )
  },
  {
    path: '**',
    redirectTo: ''
  }
];