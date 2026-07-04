import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ArticleCategory,
  ArticleDetail,
  ArticleListItem,
  FaqItem,
  PagedResult,
  PsychologistDetail,
  PsychologistSummary,
  ServiceCategory,
  Testimonial,
} from './content.models';

/** Read-only public content API (anonymous; served through the SSR/api proxy). */
@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly http = inject(HttpClient);

  getPsychologists(): Observable<PsychologistSummary[]> {
    return this.http.get<PsychologistSummary[]>('/api/psychologists');
  }

  getPsychologist(slug: string): Observable<PsychologistDetail> {
    return this.http.get<PsychologistDetail>(`/api/psychologists/${slug}`);
  }

  getServiceCatalog(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>('/api/services');
  }

  getArticles(options?: {
    category?: string;
    search?: string;
    page?: number;
  }): Observable<PagedResult<ArticleListItem>> {
    let params = new HttpParams();
    if (options?.category) params = params.set('category', options.category);
    if (options?.search) params = params.set('search', options.search);
    if (options?.page) params = params.set('page', options.page);
    return this.http.get<PagedResult<ArticleListItem>>('/api/articles', { params });
  }

  getArticle(slug: string): Observable<ArticleDetail> {
    return this.http.get<ArticleDetail>(`/api/articles/${slug}`);
  }

  getArticleCategories(): Observable<ArticleCategory[]> {
    return this.http.get<ArticleCategory[]>('/api/articles/categories');
  }

  getFaq(): Observable<FaqItem[]> {
    return this.http.get<FaqItem[]>('/api/faq');
  }

  getTestimonials(): Observable<Testimonial[]> {
    return this.http.get<Testimonial[]>('/api/testimonials');
  }
}
