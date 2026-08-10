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
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  PromotionCandidate,
  PromotionCandidateStatus,
} from '../../core/models/promotion-candidate.model';
import { PromotionService } from '../../core/services/promotion.service';

type PromotionFilter = PromotionCandidateStatus | 'all';

interface StoreOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-promotions-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './promotions-page.component.html',
  styleUrl: './promotions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionsPageComponent implements OnInit {
  private readonly promotionService = inject(PromotionService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly promotions = signal<PromotionCandidate[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly busyPromotionId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly copiedCouponId = signal<string | null>(null);

  readonly searchTerm = signal('');
  readonly selectedStatus = signal<PromotionFilter>('all');
  readonly selectedPromotion = signal<PromotionCandidate | null>(null);
  readonly promotionToIgnore = signal<PromotionCandidate | null>(null);

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

  readonly statuses: Array<{
    value: PromotionFilter;
    label: string;
  }> = [
    { value: 'all', label: 'Todas' },
    { value: 'NeedsReview', label: 'Revisar' },
    { value: 'Pending', label: 'Pendentes' },
    { value: 'Imported', label: 'Importadas' },
    {
      value: 'UnsupportedStore',
      label: 'Loja não suportada',
    },
    { value: 'Ignored', label: 'Ignoradas' },
  ];

  readonly availableStores = computed(() => {
    const stores = new Map<string, StoreOption>();

    for (const store of this.fallbackStores) {
      stores.set(store.name.toLowerCase(), store);
    }

    for (const promotion of this.promotions()) {
      if (promotion.storeId && promotion.storeName) {
        stores.set(promotion.storeName.toLowerCase(), {
          id: promotion.storeId,
          name: promotion.storeName,
        });
      }
    }

    return [...stores.values()].sort((first, second) =>
      first.name.localeCompare(second.name, 'pt-BR'),
    );
  });

  readonly attentionCount = computed(
    () => this.promotions().filter((promotion) => this.canReview(promotion)).length,
  );

  readonly importedCount = computed(
    () => this.promotions().filter((promotion) => promotion.status === 'Imported').length,
  );

  readonly couponCount = computed(
    () => this.promotions().filter((promotion) => Boolean(promotion.coupon)).length,
  );

  readonly filteredPromotions = computed(() => {
    const status = this.selectedStatus();
    const query = this.searchTerm().trim().toLocaleLowerCase('pt-BR');

    return this.promotions()
      .filter((promotion) => {
        const matchesStatus = status === 'all' || promotion.status === status;

        const searchableText = [
          promotion.productName,
          promotion.storeName ?? '',
          promotion.coupon ?? '',
          String(promotion.telegramMessageId),
        ]
          .join(' ')
          .toLocaleLowerCase('pt-BR');

        return matchesStatus && (!query || searchableText.includes(query));
      })
      .sort(
        (first, second) =>
          new Date(second.receivedAt).getTime() - new Date(first.receivedAt).getTime(),
      );
  });

  readonly conversionForm = this.formBuilder.nonNullable.group({
    storeId: ['', Validators.required],
    targetPrice: [0, [Validators.required, Validators.min(0.01)]],
    productUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/i)]],
    autoBuyEnabled: [false],
  });

  ngOnInit(): void {
    const requestedStatus = this.route.snapshot.queryParamMap.get('status');

    if (requestedStatus && this.isPromotionFilter(requestedStatus)) {
      this.selectedStatus.set(requestedStatus);
    }

    this.loadPromotions();
  }

  @HostListener('document:keydown.escape')
  closeDialogsOnEscape(): void {
    this.closeConversion();
    this.closeIgnoreDialog();
  }

  loadPromotions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.promotionService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (promotions) => this.promotions.set(promotions),
        error: (error) => {
          console.error(error);
          this.error.set(
            'Não foi possível carregar as promoções. Verifique se a API está em execução.',
          );
        },
      });
  }

  changeStatusFilter(value: PromotionFilter): void {
    this.selectedStatus.set(value);
    this.closeConversion();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set('all');
  }

  openConversion(promotion: PromotionCandidate): void {
    this.clearMessages();
    this.selectedPromotion.set(promotion);

    this.conversionForm.reset({
      storeId: promotion.storeId ?? this.availableStores()[0]?.id ?? '',
      targetPrice: promotion.advertisedPrice,
      productUrl: promotion.resolvedUrl ?? promotion.originalUrl,
      autoBuyEnabled: false,
    });
  }

  closeConversion(): void {
    this.selectedPromotion.set(null);
  }

  convertPromotion(): void {
    const promotion = this.selectedPromotion();

    if (!promotion) {
      return;
    }

    if (this.conversionForm.invalid) {
      this.conversionForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.clearMessages();

    this.promotionService
      .createProductTarget(promotion.id, this.conversionForm.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.selectedPromotion.set(null);
          this.successMessage.set('Promoção transformada em monitoramento.');
          this.loadPromotions();
        },
        error: (error) => {
          console.error(error);
          this.error.set(error?.error?.error ?? 'Não foi possível criar o monitoramento.');
        },
      });
  }

  requestIgnore(promotion: PromotionCandidate): void {
    this.clearMessages();
    this.promotionToIgnore.set(promotion);
  }

  closeIgnoreDialog(): void {
    this.promotionToIgnore.set(null);
  }

  confirmIgnore(): void {
    const promotion = this.promotionToIgnore();

    if (!promotion) {
      return;
    }

    this.busyPromotionId.set(promotion.id);
    this.clearMessages();

    this.promotionService
      .ignore(promotion.id)
      .pipe(finalize(() => this.busyPromotionId.set(null)))
      .subscribe({
        next: () => {
          this.promotions.update((promotions) =>
            promotions.map((item) =>
              item.id === promotion.id ? { ...item, status: 'Ignored' } : item,
            ),
          );
          this.promotionToIgnore.set(null);
          this.successMessage.set('Promoção removida da fila de revisão.');
        },
        error: (error) => {
          console.error(error);
          this.error.set(error?.error?.error ?? 'Não foi possível ignorar a promoção.');
        },
      });
  }

  async copyCoupon(promotion: PromotionCandidate): Promise<void> {
    if (!promotion.coupon) {
      return;
    }

    try {
      await navigator.clipboard.writeText(promotion.coupon);
      this.copiedCouponId.set(promotion.id);

      setTimeout(() => {
        if (this.copiedCouponId() === promotion.id) {
          this.copiedCouponId.set(null);
        }
      }, 1800);
    } catch (error) {
      console.error(error);
      this.error.set('Não foi possível copiar o cupom.');
    }
  }

  canReview(promotion: PromotionCandidate): boolean {
    return (
      promotion.status === 'Pending' ||
      promotion.status === 'NeedsReview' ||
      promotion.status === 'UnsupportedStore'
    );
  }

  getStatusCount(status: PromotionFilter): number {
    if (status === 'all') {
      return this.promotions().length;
    }

    return this.promotions().filter((promotion) => promotion.status === status).length;
  }

  getStatusLabel(status: PromotionCandidateStatus): string {
    const labels: Record<PromotionCandidateStatus, string> = {
      Pending: 'Pendente',
      NeedsReview: 'Precisa de revisão',
      Imported: 'Importada',
      Ignored: 'Ignorada',
      UnsupportedStore: 'Loja não suportada',
    };

    return labels[status];
  }

  getStatusDescription(promotion: PromotionCandidate): string {
    if (promotion.reviewReason) {
      return promotion.reviewReason;
    }

    const descriptions: Record<PromotionCandidateStatus, string> = {
      Pending: 'Aguardando uma decisão.',
      NeedsReview: 'Confira loja, link e preço.',
      Imported: 'Produto consolidado no catálogo.',
      Ignored: 'Removida da fila de trabalho.',
      UnsupportedStore: 'A loja ainda não possui suporte completo.',
    };

    return descriptions[promotion.status];
  }

  getPromotionUrl(promotion: PromotionCandidate): string {
    return promotion.resolvedUrl ?? promotion.originalUrl;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  }

  private isPromotionFilter(value: string): value is PromotionFilter {
    return ['all', 'Pending', 'Imported', 'Ignored', 'NeedsReview', 'UnsupportedStore'].includes(
      value,
    );
  }

  private clearMessages(): void {
    this.error.set(null);
    this.successMessage.set(null);
  }
}
