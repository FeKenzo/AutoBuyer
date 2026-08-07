import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize } from 'rxjs';

import {
  PromotionCandidate,
  PromotionCandidateStatus
} from '../../core/models/promotion-candidate.model';
import { PromotionService } from
  '../../core/services/promotion.service';

interface StoreOption {
  id: string;
  name: string;
  hosts: string[];
}

@Component({
  selector: 'app-promotions-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './promotions-page.component.html',
  styleUrl: './promotions-page.component.scss'
})
export class PromotionsPageComponent implements OnInit {
  private readonly promotionService =
    inject(PromotionService);

  private readonly formBuilder =
    inject(FormBuilder);

  readonly promotions = signal<PromotionCandidate[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly selectedStatus =
    signal<PromotionCandidateStatus | null>(null);

  readonly selectedPromotion =
    signal<PromotionCandidate | null>(null);

  readonly stores: StoreOption[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Terabyte',
      hosts: ['terabyteshop.com.br']
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Pichau',
      hosts: ['pichau.com.br']
    },

    /*
     * Substitua pelos IDs reais após inserir essas lojas
     * na tabela stores.
     */
    {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'AliExpress',
      hosts: [
        'aliexpress.com',
        's.click.aliexpress.com'
      ]
    },
    {
      id: '44444444-4444-4444-4444-444444444444',
      name: 'KaBuM!',
      hosts: ['kabum.com.br']
    },
    {
      id: '55555555-5555-5555-5555-555555555555',
      name: 'Mercado Livre',
      hosts: [
        'mercadolivre.com.br',
        'mercadolivre.com'
      ]
    },
    {
      id: '66666666-6666-6666-6666-666666666666',
      name: 'Shopee',
      hosts: ['shopee.com.br']
    }
  ];

  readonly statuses: Array<{
    value: PromotionCandidateStatus | null;
    label: string;
  }> = [
    {
      value: null,
      label: 'Todas'
    },
    {
      value: 'Pending',
      label: 'Pendentes'
    },
    {
      value: 'NeedsReview',
      label: 'Revisão'
    },
    {
      value: 'Imported',
      label: 'Importadas'
    },
    {
      value: 'Ignored',
      label: 'Ignoradas'
    },
    {
      value: 'UnsupportedStore',
      label: 'Loja não suportada'
    }
  ];

  readonly pendingCount = computed(() =>
    this.promotions().filter(
      promotion =>
        promotion.status === 'Pending' ||
        promotion.status === 'NeedsReview'
    ).length
  );

  readonly conversionForm =
    this.formBuilder.nonNullable.group({
      storeId: [
        '',
        Validators.required
      ],
      targetPrice: [
        0,
        [
          Validators.required,
          Validators.min(0.01)
        ]
      ],
      productUrl: [
        '',
        [
          Validators.required,
          Validators.pattern(/^https?:\/\/.+/i)
        ]
      ],
      autoBuyEnabled: [false]
    });

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.promotionService
      .getAll(this.selectedStatus())
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: promotions => {
          this.promotions.set(promotions);
        },
        error: error => {
          console.error(error);

          this.error.set(
            'Não foi possível carregar as promoções.'
          );
        }
      });
  }

  changeStatusFilter(
    status: PromotionCandidateStatus | null
  ): void {
    this.selectedStatus.set(status);
    this.closeConversion();
    this.loadPromotions();
  }

  openConversion(
    promotion: PromotionCandidate
  ): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.selectedPromotion.set(promotion);

    const suggestedStore =
      this.findSuggestedStore(promotion);

    this.conversionForm.reset({
      storeId: promotion.storeId
        ?? suggestedStore?.id
        ?? '',
      targetPrice: promotion.advertisedPrice,
      productUrl:
        promotion.resolvedUrl
        ?? promotion.originalUrl,
      autoBuyEnabled: false
    });
  }

  closeConversion(): void {
    this.selectedPromotion.set(null);

    this.conversionForm.reset({
      storeId: '',
      targetPrice: 0,
      productUrl: '',
      autoBuyEnabled: false
    });
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
    this.error.set(null);
    this.successMessage.set(null);

    const formValue =
      this.conversionForm.getRawValue();

    this.promotionService
      .createProductTarget(
        promotion.id,
        {
          storeId: formValue.storeId,
          targetPrice: formValue.targetPrice,
          productUrl: formValue.productUrl,
          autoBuyEnabled:
            formValue.autoBuyEnabled
        }
      )
      .pipe(
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: result => {
          this.successMessage.set(
            `"${promotion.productName}" foi adicionado aos monitoramentos.`
          );

          this.closeConversion();
          this.loadPromotions();
        },
        error: error => {
          console.error(error);

          this.error.set(
            error?.error?.error
            ?? 'Não foi possível converter a promoção.'
          );
        }
      });
  }

  ignorePromotion(
    promotion: PromotionCandidate
  ): void {
    const confirmed = window.confirm(
      `Ignorar a promoção "${promotion.productName}"?`
    );

    if (!confirmed) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);

    this.promotionService
      .ignore(promotion.id)
      .subscribe({
        next: () => {
          this.promotions.update(promotions =>
            promotions.filter(
              item => item.id !== promotion.id
            )
          );

          this.successMessage.set(
            'Promoção ignorada.'
          );
        },
        error: error => {
          console.error(error);

          this.error.set(
            error?.error?.error
            ?? 'Não foi possível ignorar a promoção.'
          );
        }
      });
  }

  canReview(
    promotion: PromotionCandidate
  ): boolean {
    return promotion.status === 'Pending'
      || promotion.status === 'NeedsReview'
      || promotion.status === 'UnsupportedStore';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat(
      'pt-BR',
      {
        style: 'currency',
        currency: 'BRL'
      }
    ).format(value);
  }

  getStatusLabel(
    status: PromotionCandidateStatus
  ): string {
    const labels: Record<
      PromotionCandidateStatus,
      string
    > = {
      Pending: 'Pendente',
      NeedsReview: 'Precisa de revisão',
      Imported: 'Importada',
      Ignored: 'Ignorada',
      UnsupportedStore: 'Loja não suportada'
    };

    return labels[status];
  }

  private findSuggestedStore(
    promotion: PromotionCandidate
  ): StoreOption | undefined {
    const url =
      promotion.resolvedUrl
      ?? promotion.originalUrl;

    try {
      const host =
        new URL(url).hostname.toLowerCase();

      return this.stores.find(store =>
        store.hosts.some(storeHost =>
          host === storeHost
          || host.endsWith(`.${storeHost}`)
        )
      );
    } catch {
      return undefined;
    }
  }
}