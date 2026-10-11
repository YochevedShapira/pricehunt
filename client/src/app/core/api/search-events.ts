import { z } from 'zod';

/**
 * The events of `GET /api/searches/stream`, one schema per SSE event name.
 * The schemas validate the JSON at the boundary and the TypeScript types are
 * inferred from them, so no cast is ever needed to turn a message into a
 * typed event.
 */

const searchStartedSchema = z.object({
  type: z.literal('started'),
  searchId: z.string(),
  supplierCount: z.number().int().nonnegative(),
});

const priceReceivedSchema = z.object({
  type: z.literal('price'),
  supplierName: z.string(),
  amount: z.number().positive(),
  currency: z.string().length(3),
  responseTimeMs: z.number().int().nonnegative(),
});

const supplierFailedSchema = z.object({
  type: z.literal('failed'),
  supplierName: z.string(),
  reason: z.string(),
  responseTimeMs: z.number().int().nonnegative(),
});

export const searchStatusSchema = z.enum(['Completed', 'TimedOut', 'Cancelled']);

const searchEndedSchema = z.object({
  type: z.literal('ended'),
  status: searchStatusSchema,
  respondedCount: z.number().int().nonnegative(),
});

export const searchEventSchema = z.discriminatedUnion('type', [
  searchStartedSchema,
  priceReceivedSchema,
  supplierFailedSchema,
  searchEndedSchema,
]);

export type SearchStarted = z.infer<typeof searchStartedSchema>;
export type PriceReceived = z.infer<typeof priceReceivedSchema>;
export type SupplierFailed = z.infer<typeof supplierFailedSchema>;
export type SearchEnded = z.infer<typeof searchEndedSchema>;
export type SearchStatus = z.infer<typeof searchStatusSchema>;
export type SearchEvent = z.infer<typeof searchEventSchema>;

/** The event names the server sends, used to subscribe to the stream. */
export const SEARCH_EVENT_TYPES: readonly SearchEvent['type'][] = searchEventSchema.options.map(
  (option) => option.shape.type.value,
);

/** What the user asked for; becomes the query string of the stream URL. */
export interface SearchRequest {
  readonly fromLocation: string;
  readonly toLocation: string;
  readonly fromDate: string;
  readonly toDate: string;
  /** Empty means every supplier. */
  readonly suppliers: readonly string[];
}
