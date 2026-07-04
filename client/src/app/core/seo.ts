import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

const SITE_NAME = 'Ardayasa Wellbeing and Growth Center';

export interface SeoData {
  title: string;
  description?: string;
  /** Absolute or root-relative image URL for social sharing. */
  image?: string;
  type?: 'website' | 'article';
}

/**
 * Per-route titles + meta/Open Graph tags. Runs during SSR too, so crawlers
 * see the real values in the initial HTML (SPEC §4).
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  update(data: SeoData): void {
    const fullTitle = data.title === SITE_NAME ? data.title : `${data.title} — ${SITE_NAME}`;
    this.title.setTitle(fullTitle);
    this.meta.updateTag({ property: 'og:title', content: fullTitle });
    this.meta.updateTag({ property: 'og:site_name', content: SITE_NAME });
    this.meta.updateTag({ property: 'og:type', content: data.type ?? 'website' });
    this.meta.updateTag({ property: 'og:url', content: this.currentUrl() });

    if (data.description) {
      this.meta.updateTag({ name: 'description', content: data.description });
      this.meta.updateTag({ property: 'og:description', content: data.description });
    }

    if (data.image) {
      this.meta.updateTag({ property: 'og:image', content: this.absolute(data.image) });
    } else {
      this.meta.removeTag("property='og:image'");
    }
  }

  private currentUrl(): string {
    const location = this.document.location;
    return location ? `${location.origin}${location.pathname}` : '';
  }

  private absolute(url: string): string {
    if (url.startsWith('http')) {
      return url;
    }

    const origin = this.document.location?.origin ?? '';
    return `${origin}${url}`;
  }
}
