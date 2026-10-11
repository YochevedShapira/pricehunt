import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the application title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const toolbar = fixture.nativeElement as HTMLElement;
    expect(toolbar.querySelector('.app-title')?.textContent).toContain('PriceHunt');
  });

  it('links to both screens', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a'));
    expect(links.map((a) => a.textContent?.trim())).toEqual(['Search', 'History']);
  });
});
