export type PromotionCandidateStatus =
  | 'Pending'
  | 'Imported'
  | 'Ignored'
  | 'NeedsReview'
  | 'UnsupportedStore';

export interface PromotionCandidate {
  id: string;
  telegramChatId: number;
  telegramMessageId: number;

  storeId: string | null;
  storeName: string | null;

  productName: string;
  advertisedPrice: number;

  originalUrl: string;
  resolvedUrl: string | null;

  coupon: string | null;
  conditions: string | null;

  status: PromotionCandidateStatus;

  productTargetId: string | null;

  receivedAt: string;
  processedAt: string | null;
}