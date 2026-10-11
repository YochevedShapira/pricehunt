import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { EVENT_SOURCE_FACTORY } from './event-source-factory';
import { SEARCH_EVENT_TYPES, SearchEvent, SearchRequest } from './search-events';

/**
 * Turns the browser's built-in `EventSource` into an RxJS stream of typed
 * search events. Unsubscribing closes the connection, which is how the
 * server learns that the search was cancelled. The stream completes after
 * the `ended` event; closing there matters because `EventSource` would
 * otherwise reconnect and start a new search.
 */
@Injectable({ providedIn: 'root' })
export class SearchStreamClient {
  private readonly createEventSource = inject(EVENT_SOURCE_FACTORY);

  search(request: SearchRequest): Observable<SearchEvent> {
    return new Observable<SearchEvent>((subscriber) => {
      const source = this.createEventSource(toStreamUrl(request));

      for (const type of SEARCH_EVENT_TYPES) {
        source.addEventListener(type, (message) => {
          const event = {
            type,
            ...JSON.parse((message as MessageEvent<string>).data),
          } as SearchEvent;
          subscriber.next(event);
          if (event.type === 'ended') {
            subscriber.complete();
          }
        });
      }

      source.onerror = () => subscriber.error(new Error('The price stream was interrupted.'));

      return () => source.close();
    });
  }
}

function toStreamUrl(request: SearchRequest): string {
  const query = new URLSearchParams({
    fromLocation: request.fromLocation,
    toLocation: request.toLocation,
    fromDate: request.fromDate,
    toDate: request.toDate,
  });
  for (const supplier of request.suppliers) {
    query.append('suppliers', supplier);
  }

  return `/api/searches/stream?${query}`;
}
