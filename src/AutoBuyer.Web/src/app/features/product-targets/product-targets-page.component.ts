import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { ProductTarget } from '../../core/models/product-target.model';
import { ProductTargetService } from '../../core/services/product-target.service';

@Component({
  selector: 'app-product-targets-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './product-targets-page.component.html',
  styleUrl: './product-targets-page.component.scss'
})
export class ProductTargetsPageComponent implements OnInit {
  private readonly productTargetService = inject(ProductTargetService);
  private readonly formBuilder = inject(FormBuilder);

  readonly products = signal<ProductTarget[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  // IDs utilizados no seed inicial do backend.
  readonly stores = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Terabyte'
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Pichau'
    }
  ];

  readonly form = this.formBuilder.nonNullable.group({
    storeId: [
      this.stores[0].id,
      Validators.required
    ],
    name: [
      '',
      [
        Validators.required,
        Validators.maxLength(250)
      ]
    ],
    productUrl: [
      '',
      [
        Validators.required,
        Validators.pattern(/^https?:\/\/.+/i)
      ]
    ],
    targetPrice: [
      0,
      [
        Validators.required,
        Validators.min(0.01)
      ]
    ],
    autoBuyEnabled: [false]
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productTargetService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: products => this.products.set(products),
        error: error => {
          console.error(error);

          this.error.set(
            'Não foi possível carregar os monitoramentos.'
          );
        }
      });
  }

  createProduct(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.productTargetService
      .create(this.form.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.form.reset({
            storeId: this.stores[0].id,
            name: '',
            productUrl: '',
            targetPrice: 0,
            autoBuyEnabled: false
          });

          this.loadProducts();
        },
        error: error => {
          console.error(error);

          this.error.set(
            error?.error?.error ??
            'Não foi possível cadastrar o monitoramento.'
          );
        }
      });
  }

  changeMonitoring(
    product: ProductTarget,
    enabled: boolean
  ): void {
    this.productTargetService
      .changeMonitoringStatus(product.id, enabled)
      .subscribe({
        next: () => {
          this.products.update(products =>
            products.map(item =>
              item.id === product.id
                ? { ...item, monitoringEnabled: enabled }
                : item
            )
          );
        },
        error: error => {
          console.error(error);

          this.error.set(
            'Não foi possível alterar o monitoramento.'
          );
        }
      });
  }

  deleteProduct(product: ProductTarget): void {
    const confirmed = window.confirm(
      `Excluir o monitoramento de "${product.name}"?`
    );

    if (!confirmed) {
      return;
    }

    this.productTargetService
      .delete(product.id)
      .subscribe({
        next: () => {
          this.products.update(products =>
            products.filter(item => item.id !== product.id)
          );
        },
        error: error => {
          console.error(error);

          this.error.set(
            'Não foi possível excluir o monitoramento.'
          );
        }
      });
  }

  formatCurrency(value: number | null): string {
    if (value === null) {
      return 'Aguardando captura';
    }

    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }
}