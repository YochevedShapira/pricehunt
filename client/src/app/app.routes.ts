import { Routes } from '@angular/router';

/**
 * The two screens of the brief. Each is lazy-loaded so the history screen's
 * table code is not downloaded until it is opened.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'search' },
  {
    path: 'search',
    title: 'PriceHunt · Search',
    loadComponent: () => import('./features/search/search-page').then((m) => m.SearchPage),
  },
  {
    path: 'history',
    title: 'PriceHunt · History',
    loadComponent: () =>
      import('./features/history/history-page').then((m) => m.HistoryPageComponent),
  },
];
