import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { ProductTarget } from '../../core/models/product-target.model';
import {
  PromotionCandidate,
  PromotionCandidateStatus,
} from '../../core/models/promotion-candidate.model';
import {
  StoreMonitoringState,
  StoreMonitoringStatus,
} from '../../core/models/store-monitoring-state.model';
import { MonitoringService } from '../../core/services/monitoring.service';
import { ProductTargetService } from '../../core/services/product-target.service';
import { PromotionService } from '../../core/services/promotion.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  private readonly productService = inject(ProductTargetService);
  private readonly promotionService = inject(PromotionService);
  private readonly monitoringService = inject(MonitoringService);

  readonly products = signal<ProductTarget[]>([]);
  readonly promotions = signal<PromotionCandidate[]>([]);
  readonly storeStates = signal<StoreMonitoringState[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly lastUpdatedAt = signal<Date | null>(null);

  readonly activeProductsCount = computed(
    () => this.products().filter((product) => product.monitoringEnabled).length,
  );

  readonly reachedProductsCount = computed(
    () => this.products().filter((product) => this.isTargetReached(product)).length,
  );

  readonly unconfiguredProductsCount = computed(
    () => this.products().filter((product) => product.targetPrice === null).length,
  );

  readonly reviewPromotionsCount = computed(
    () => this.promotions().filter((promotion) => this.requiresReview(promotion.status)).length,
  );

  readonly operationalStoresCount = computed(
    () => this.storeStates().filter((store) => store.status === 'Supported').length,
  );

  readonly opportunityProducts = computed(() =>
    [...this.products()]
      .filter((product) => this.getProductPrice(product) !== null)
      .sort((first, second) => {
        const targetDifference =
          Number(this.isTargetReached(second)) - Number(this.isTargetReached(first));

        if (targetDifference !== 0) {
          return targetDifference;
        }

        return new Date(second.updatedAt).getTime() - new Date(first.updatedAt).getTime();
      })
      .slice(0, 5),
  );

  readonly recentPromotions = computed(() =>
    [...this.promotions()]
      .sort(
        (first, second) =>
          new Date(second.receivedAt).getTime() - new Date(first.receivedAt).getTime(),
      )
      .slice(0, 5),
  );

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      products: this.productService.getAll(),
      promotions: this.promotionService.getAll(),
      stores: this.monitoringService.getStoreStates(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.products.set(result.products);
          this.promotions.set(result.promotions);
          this.storeStates.set(result.stores);
          this.lastUpdatedAt.set(new Date());
        },
        error: (error) => {
          console.error(error);
          this.error.set(
            'Não foi possível atualizar o painel. Verifique se a API está em execução.',
          );
        },
      });
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

  getProductOpportunity(product: ProductTarget): string {
    const price = this.getProductPrice(product);

    if (price === null) {
      return 'Aguardando o primeiro preço';
    }

    if (product.targetPrice === null) {
      return 'Defina uma meta para este produto';
    }

    const difference = Math.abs(price - product.targetPrice);

    if (price <= product.targetPrice) {
      return this.formatCurrency(difference) + ' abaixo da meta';
    }

    return 'Faltam ' + this.formatCurrency(difference);
  }

  getProductPriceSource(product: ProductTarget): string {
    if (product.currentPrice !== null) {
      return 'Captura do worker';
    }

    if (product.lastObservedPrice !== null) {
      return 'Observação do Telegram';
    }

    return 'Sem preço';
  }

  requiresReview(status: PromotionCandidateStatus): boolean {
    return status === 'Pending' || status === 'NeedsReview' || status === 'UnsupportedStore';
  }

  getPromotionStatusLabel(status: PromotionCandidateStatus): string {
    const labels: Record<PromotionCandidateStatus, string> = {
      Pending: 'Pendente',
      Imported: 'Importada',
      Ignored: 'Ignorada',
      NeedsReview: 'Revisar',
      UnsupportedStore: 'Loja não suportada',
    };

    return labels[status];
  }

  getStoreStatusLabel(status: StoreMonitoringStatus): string {
    const labels: Record<StoreMonitoringStatus, string> = {
      Supported: 'Operacional',
      TemporarilyBlocked: 'Pausa temporária',
      RequiresSession: 'Requer sessão',
      RequiresManualAction: 'Ação necessária',
      Unsupported: 'Não suportada',
    };

    return labels[status];
  }

  getStoreName(host: string): string {
    const names: Record<string, string> = {
      'terabyteshop.com.br': 'Terabyte',
      'pichau.com.br': 'Pichau',
    };

    return names[host] ?? host;
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

  formatRelativeTime(value: string | Date | null): string {
    if (!value) {
      return 'Ainda não atualizado';
    }

    const date = value instanceof Date ? value : new Date(value);
    const difference = Date.now() - date.getTime();

    if (Number.isNaN(difference)) {
      return 'Data indisponível';
    }

    const minutes = Math.max(0, Math.floor(difference / 60_000));

    if (minutes < 1) {
      return 'Agora';
    }

    if (minutes < 60) {
      return 'Há ' + minutes + ' min';
    }

    const hours = Math.floor(minutes / 60);

    if (hours < 24) {
      return 'Há ' + hours + ' h';
    }

    const days = Math.floor(hours / 24);
    return 'Há ' + days + ' dia' + (days === 1 ? '' : 's');
  }
}
