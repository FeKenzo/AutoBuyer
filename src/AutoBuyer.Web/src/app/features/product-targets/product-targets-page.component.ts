import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';

import { ProductTarget } from '../../core/models/product-target.model';
import { ProductTargetService } from '../../core/services/product-target.service';

type ProductStatusFilter = 'all' | 'monitoring' | 'paused' | 'reached' | 'missing-target';

type ProductSort = 'recent' | 'name' | 'price-low' | 'price-high' | 'closest';

interface StoreOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-product-targets-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-targets-page.component.html',
  styleUrl: './product-targets-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductTargetsPageComponent implements OnInit {
  private readonly productService = inject(ProductTargetService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly products = signal<ProductTarget[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly busyProductId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly searchTerm = signal('');
  readonly statusFilter = signal<ProductStatusFilter>('all');
  readonly storeFilter = signal('all');
  readonly sortOrder = signal<ProductSort>('recent');

  readonly createModalOpen = signal(false);
  readonly selectedProduct = signal<ProductTarget | null>(null);
  readonly productToDelete = signal<ProductTarget | null>(null);
  readonly focusProductId = signal<string | null>(null);

  private readonly fallbackStores: StoreOption[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Terabyte',
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Pichau',
    },
  ];

  readonly availableStores = computed(() => {
    const stores = new Map<string, StoreOption>();

    for (const store of this.fallbackStores) {
      stores.set(store.name.toLowerCase(), store);
    }

    for (const product of this.products()) {
      if (product.storeName) {
        stores.set(product.storeName.toLowerCase(), {
          id: product.storeId,
          name: product.storeName,
        });
      }
    }

    return [...stores.values()].sort((first, second) =>
      first.name.localeCompare(second.name, 'pt-BR'),
    );
  });

  readonly productStores = computed(() => {
    const names = new Set(
      this.products()
        .map((product) => product.storeName)
        .filter(Boolean),
    );

    return [...names].sort((first, second) => first.localeCompare(second, 'pt-BR'));
  });

  readonly activeCount = computed(
    () => this.products().filter((product) => product.monitoringEnabled).length,
  );

  readonly reachedCount = computed(
    () => this.products().filter((product) => this.isTargetReached(product)).length,
  );

  readonly missingTargetCount = computed(
    () => this.products().filter((product) => product.targetPrice === null).length,
  );

  readonly filteredProducts = computed(() => {
    const query = this.searchTerm().trim().toLocaleLowerCase('pt-BR');
    const status = this.statusFilter();
    const store = this.storeFilter();

    const filtered = this.products().filter((product) => {
      const matchesSearch =
        !query ||
        product.name.toLocaleLowerCase('pt-BR').includes(query) ||
        product.storeName.toLocaleLowerCase('pt-BR').includes(query) ||
        product.externalProductId?.toLocaleLowerCase('pt-BR').includes(query);

      const matchesStore = store === 'all' || product.storeName === store;

      const matchesStatus =
        status === 'all' ||
        (status === 'monitoring' && product.monitoringEnabled) ||
        (status === 'paused' && !product.monitoringEnabled) ||
        (status === 'reached' && this.isTargetReached(product)) ||
        (status === 'missing-target' && product.targetPrice === null);

      return matchesSearch && matchesStore && matchesStatus;
    });

    return filtered.sort((first, second) => this.compareProducts(first, second));
  });

  readonly createForm = this.formBuilder.nonNullable.group({
    storeId: [this.fallbackStores[0].id, Validators.required],
    name: ['', [Validators.required, Validators.maxLength(250)]],
    productUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/i)]],
    targetPrice: [0, [Validators.required, Validators.min(0.01)]],
    autoBuyEnabled: [false],
  });

  readonly editForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(250)]],
    productUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/i)]],
    targetPrice: [0, [Validators.required, Validators.min(0.01)]],
    autoBuyEnabled: [false],
  });

  ngOnInit(): void {
    const queryParams = this.route.snapshot.queryParamMap;

    if (queryParams.get('novo') === 'true') {
      this.openCreateModal();
    }

    const focusId = queryParams.get('foco');

    if (focusId) {
      this.focusProductId.set(focusId);
    }

    this.loadProducts();
  }

  @HostListener('document:keydown.escape')
  closeDialogsOnEscape(): void {
    this.closeCreateModal();
    this.closeEditModal();
    this.closeDeleteDialog();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (products) => {
          this.products.set(products);

          if (this.createForm.pristine) {
            const firstStore = this.availableStores()[0];

            if (firstStore) {
              this.createForm.controls.storeId.setValue(firstStore.id);
            }
          }

          this.scrollToFocusedProduct();
        },
        error: (error) => {
          console.error(error);
          this.error.set(
            'Não foi possível carregar os monitoramentos. Verifique a conexão com a API.',
          );
        },
      });
  }

  openCreateModal(): void {
    this.clearMessages();
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.createModalOpen.set(false);
  }

  createProduct(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.clearMessages();

    this.productService
      .create(this.createForm.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (product) => {
          this.products.update((products) => [product, ...products]);
          this.successMessage.set('Monitoramento criado com sucesso.');
          this.createModalOpen.set(false);
          this.resetCreateForm();
        },
        error: (error) => {
          console.error(error);
          this.error.set(error?.error?.error ?? 'Não foi possível cadastrar o monitoramento.');
        },
      });
  }

  openEditModal(product: ProductTarget): void {
    this.clearMessages();
    this.selectedProduct.set(product);
    this.editForm.reset({
      name: product.name,
      productUrl: product.productUrl,
      targetPrice: product.targetPrice ?? this.getProductPrice(product) ?? 0,
      autoBuyEnabled: product.autoBuyEnabled,
    });
  }

  closeEditModal(): void {
    this.selectedProduct.set(null);
  }

  saveProduct(): void {
    const product = this.selectedProduct();

    if (!product) {
      return;
    }

    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.clearMessages();

    this.productService
      .update(product.id, this.editForm.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.selectedProduct.set(null);
          this.successMessage.set('Produto e meta atualizados.');
          this.loadProducts();
        },
        error: (error) => {
          console.error(error);
          this.error.set(error?.error?.error ?? 'Não foi possível atualizar o produto.');
        },
      });
  }

  changeMonitoring(product: ProductTarget): void {
    const enabled = !product.monitoringEnabled;
    this.busyProductId.set(product.id);
    this.clearMessages();

    this.productService
      .changeMonitoringStatus(product.id, enabled)
      .pipe(finalize(() => this.busyProductId.set(null)))
      .subscribe({
        next: () => {
          this.products.update((products) =>
            products.map((item) =>
              item.id === product.id
                ? {
                    ...item,
                    monitoringEnabled: enabled,
                  }
                : item,
            ),
          );

          this.successMessage.set(enabled ? 'Monitoramento retomado.' : 'Monitoramento pausado.');
        },
        error: (error) => {
          console.error(error);
          this.error.set('Não foi possível alterar o monitoramento.');
        },
      });
  }

  requestDelete(product: ProductTarget): void {
    this.clearMessages();
    this.productToDelete.set(product);
  }

  closeDeleteDialog(): void {
    this.productToDelete.set(null);
  }

  confirmDelete(): void {
    const product = this.productToDelete();

    if (!product) {
      return;
    }

    this.busyProductId.set(product.id);
    this.clearMessages();

    this.productService
      .delete(product.id)
      .pipe(finalize(() => this.busyProductId.set(null)))
      .subscribe({
        next: () => {
          this.products.update((products) => products.filter((item) => item.id !== product.id));
          this.productToDelete.set(null);
          this.successMessage.set('Monitoramento excluído.');
        },
        error: (error) => {
          console.error(error);
          this.error.set('Não foi possível excluir o monitoramento.');
        },
      });
  }

  changeStatusFilter(value: string): void {
    const allowed: ProductStatusFilter[] = [
      'all',
      'monitoring',
      'paused',
      'reached',
      'missing-target',
    ];

    if (allowed.includes(value as ProductStatusFilter)) {
      this.statusFilter.set(value as ProductStatusFilter);
    }
  }

  changeSortOrder(value: string): void {
    const allowed: ProductSort[] = ['recent', 'name', 'price-low', 'price-high', 'closest'];

    if (allowed.includes(value as ProductSort)) {
      this.sortOrder.set(value as ProductSort);
    }
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.statusFilter.set('all');
    this.storeFilter.set('all');
    this.sortOrder.set('recent');
  }

  hasActiveFilters(): boolean {
    return (
      Boolean(this.searchTerm()) || this.statusFilter() !== 'all' || this.storeFilter() !== 'all'
    );
  }

  getProductPrice(product: ProductTarget): number | null {
    return product.currentPrice ?? product.lastObservedPrice;
  }

  isTargetReached(product: ProductTarget): boolean {
    const price = this.getProductPrice(product);

    return (
      product.targetReached ||
      (price !== null && product.targetPrice !== null && price <= product.targetPrice)
    );
  }

  getPriceProgress(product: ProductTarget): number {
    const price = this.getProductPrice(product);

    if (price === null || product.targetPrice === null) {
      return 0;
    }

    if (price <= product.targetPrice) {
      return 100;
    }

    return Math.max(8, Math.min(99, (product.targetPrice / price) * 100));
  }

  getTargetDifference(product: ProductTarget): string {
    const price = this.getProductPrice(product);

    if (price === null) {
      return 'Aguardando preço';
    }

    if (product.targetPrice === null) {
      return 'Meta ainda não definida';
    }

    const difference = Math.abs(price - product.targetPrice);

    if (price <= product.targetPrice) {
      return this.formatCurrency(difference) + ' abaixo da meta';
    }

    return 'Faltam ' + this.formatCurrency(difference);
  }

  getPriceSource(product: ProductTarget): string {
    if (product.currentPrice !== null) {
      return 'Capturado pelo worker';
    }

    if (product.lastObservedPrice !== null) {
      return 'Extraído do Telegram';
    }

    return 'Sem captura';
  }

  getProductStatus(product: ProductTarget): string {
    if (!product.monitoringEnabled) {
      return 'Pausado';
    }

    if (this.isTargetReached(product)) {
      return 'Meta atingida';
    }

    if (product.targetPrice === null) {
      return 'Configurar meta';
    }

    return 'Monitorando';
  }

  getProductStatusCode(product: ProductTarget): string {
    if (!product.monitoringEnabled) {
      return 'paused';
    }

    if (this.isTargetReached(product)) {
      return 'reached';
    }

    if (product.targetPrice === null) {
      return 'missing-target';
    }

    return 'monitoring';
  }

  formatCurrency(value: number | null): string {
    if (value === null) {
      return '—';
    }

    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  }

  private compareProducts(first: ProductTarget, second: ProductTarget): number {
    switch (this.sortOrder()) {
      case 'name':
        return first.name.localeCompare(second.name, 'pt-BR');
      case 'price-low':
        return this.numericPrice(first) - this.numericPrice(second);
      case 'price-high':
        return this.numericPrice(second) - this.numericPrice(first);
      case 'closest':
        return this.targetGap(first) - this.targetGap(second);
      case 'recent':
        return new Date(second.updatedAt).getTime() - new Date(first.updatedAt).getTime();
    }
  }

  private numericPrice(product: ProductTarget): number {
    return this.getProductPrice(product) ?? Number.MAX_SAFE_INTEGER;
  }

  private targetGap(product: ProductTarget): number {
    const price = this.getProductPrice(product);

    if (price === null || product.targetPrice === null) {
      return Number.MAX_SAFE_INTEGER;
    }

    return Math.abs(price - product.targetPrice);
  }

  private resetCreateForm(): void {
    this.createForm.reset({
      storeId: this.availableStores()[0]?.id ?? this.fallbackStores[0].id,
      name: '',
      productUrl: '',
      targetPrice: 0,
      autoBuyEnabled: false,
    });
  }

  private clearMessages(): void {
    this.error.set(null);
    this.successMessage.set(null);
  }

  private scrollToFocusedProduct(): void {
    const productId = this.focusProductId();

    if (!productId) {
      return;
    }

    setTimeout(() => {
      document.getElementById('product-' + productId)?.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }
}
