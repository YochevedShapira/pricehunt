import { InjectionToken } from '@angular/core';

/** Creates the browser `EventSource`; replaced by a fake in tests. */
export type EventSourceFactory = (url: string) => EventSource;

export const EVENT_SOURCE_FACTORY = new InjectionToken<EventSourceFactory>('EventSourceFactory', {
  providedIn: 'root',
  factory: () => (url) => new EventSource(url),
});
