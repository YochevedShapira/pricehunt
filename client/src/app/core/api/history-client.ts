import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { HistoryPage, HistoryQuery, historyPageSchema } from './history';

/** Reads one page of price history. */
@Injectable({ providedIn: 'root' })
export class HistoryClient {
  private readonly http = inject(HttpClient);

  read(query: HistoryQuery): Observable<HistoryPage> {
    return this.http
      .get<unknown>('/api/history', { params: toParams(query) })
      .pipe(map((body) => historyPageSchema.parse(body)));
  }
}

function toParams(query: HistoryQuery): HttpParams {
  let params = new HttpParams()
    .set('sortBy', query.column)
    .set('sortDirection', query.direction)
    .set('page', query.page)
    .set('pageSize', query.pageSize);

  if (query.startDate) {
    params = params.set('startDate', query.startDate);
  }
  if (query.endDate) {
    params = params.set('endDate', query.endDate);
  }
  if (query.fromLocation.trim()) {
    params = params.set('fromLocation', query.fromLocation.trim());
  }
  if (query.toLocation.trim()) {
    params = params.set('toLocation', query.toLocation.trim());
  }
  for (const supplier of query.suppliers) {
    params = params.append('suppliers', supplier);
  }

  return params;
}
