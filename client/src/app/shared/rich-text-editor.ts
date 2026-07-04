import {
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  forwardRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ChainedCommands, Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import Image from '@tiptap/extension-image';

/**
 * TipTap-based rich text editor (SPEC §7) usable as a reactive-forms control.
 * Emits HTML; the API re-sanitizes everything server-side before persisting.
 * Browser-only: initialized after first render (admin routes are client-rendered).
 */
@Component({
  selector: 'app-rich-text-editor',
  imports: [MatIconModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditor),
      multi: true,
    },
  ],
  template: `
    <div class="toolbar">
      @for (button of buttons; track button.icon) {
        <button
          type="button"
          [class.active]="activeMarks().includes(button.name)"
          (click)="button.action()"
          [title]="button.title"
        ><mat-icon>{{ button.icon }}</mat-icon></button>
      }
      <label class="image-upload" title="Sisipkan gambar">
        <mat-icon>image</mat-icon>
        <input type="file" accept="image/*" (change)="uploadImage($event)" hidden />
      </label>
    </div>
    <div #editorHost class="editor"></div>
  `,
  styles: `
    :host {
      display: block; border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 8px; overflow: hidden; background: var(--mat-sys-surface-container-low);
    }
    .toolbar {
      display: flex; flex-wrap: wrap; gap: 2px; padding: 6px;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .toolbar button, .image-upload {
      background: transparent; border: none; color: var(--mat-sys-on-surface-variant);
      border-radius: 6px; padding: 6px; cursor: pointer; display: grid; place-items: center;
    }
    .toolbar button:hover, .image-upload:hover { background: var(--mat-sys-surface-container-high); }
    .toolbar button.active { color: var(--accent-gold); background: var(--mat-sys-surface-container-high); }
    .toolbar mat-icon { font-size: 20px; width: 20px; height: 20px; }
    .editor { padding: 12px 16px; min-height: 260px; }
    .editor ::ng-deep .tiptap { outline: none; min-height: 240px; }
  `,
})
export class RichTextEditor implements ControlValueAccessor, OnDestroy {
  private readonly host = viewChild.required<ElementRef<HTMLElement>>('editorHost');
  private readonly http = inject(HttpClient);

  private editor: Editor | null = null;
  private pendingContent = '';
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  protected readonly activeMarks = signal<string[]>([]);

  protected readonly buttons = [
    { name: 'bold', icon: 'format_bold', title: 'Tebal', action: () => this.chain((c) => c.toggleBold()) },
    { name: 'italic', icon: 'format_italic', title: 'Miring', action: () => this.chain((c) => c.toggleItalic()) },
    { name: 'heading-2', icon: 'title', title: 'Judul', action: () => this.chain((c) => c.toggleHeading({ level: 2 })) },
    { name: 'heading-3', icon: 'text_fields', title: 'Subjudul', action: () => this.chain((c) => c.toggleHeading({ level: 3 })) },
    { name: 'bulletList', icon: 'format_list_bulleted', title: 'Daftar', action: () => this.chain((c) => c.toggleBulletList()) },
    { name: 'orderedList', icon: 'format_list_numbered', title: 'Daftar bernomor', action: () => this.chain((c) => c.toggleOrderedList()) },
    { name: 'blockquote', icon: 'format_quote', title: 'Kutipan', action: () => this.chain((c) => c.toggleBlockquote()) },
    { name: 'link', icon: 'link', title: 'Tautan', action: () => this.setLink() },
    { name: 'undo', icon: 'undo', title: 'Urungkan', action: () => this.chain((c) => c.undo()) },
    { name: 'redo', icon: 'redo', title: 'Ulangi', action: () => this.chain((c) => c.redo()) },
  ];

  constructor() {
    afterNextRender(() => {
      this.editor = new Editor({
        element: this.host().nativeElement,
        extensions: [
          StarterKit.configure({ link: { openOnClick: false } }),
          Image,
        ],
        content: this.pendingContent,
        onUpdate: ({ editor }) => this.onChange(editor.isEmpty ? '' : editor.getHTML()),
        onBlur: () => this.onTouched(),
        onSelectionUpdate: ({ editor }) => this.refreshActiveMarks(editor),
        onTransaction: ({ editor }) => this.refreshActiveMarks(editor),
      });
    });
  }

  writeValue(value: string | null): void {
    this.pendingContent = value ?? '';
    if (this.editor && this.editor.getHTML() !== this.pendingContent) {
      this.editor.commands.setContent(this.pendingContent);
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.editor?.setEditable(!isDisabled);
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
  }

  protected uploadImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.editor) {
      return;
    }

    const form = new FormData();
    form.append('file', file);
    this.http.post<{ url: string }>('/api/admin/uploads', form).subscribe((result) => {
      this.editor?.chain().focus().setImage({ src: result.url }).run();
    });
  }

  private setLink(): void {
    if (!this.editor) {
      return;
    }

    const previous = this.editor.getAttributes('link')['href'] as string | undefined;
    const url = window.prompt('URL tautan (kosongkan untuk menghapus):', previous ?? 'https://');
    if (url === null) {
      return;
    }

    if (url === '' || url === 'https://') {
      this.editor.chain().focus().unsetLink().run();
      return;
    }

    this.editor.chain().focus().setLink({ href: url }).run();
  }

  private chain(command: (chain: ChainedCommands) => ChainedCommands): void {
    if (this.editor) {
      command(this.editor.chain().focus()).run();
      this.refreshActiveMarks(this.editor);
    }
  }

  private refreshActiveMarks(editor: Editor): void {
    const active: string[] = [];
    if (editor.isActive('bold')) active.push('bold');
    if (editor.isActive('italic')) active.push('italic');
    if (editor.isActive('heading', { level: 2 })) active.push('heading-2');
    if (editor.isActive('heading', { level: 3 })) active.push('heading-3');
    if (editor.isActive('bulletList')) active.push('bulletList');
    if (editor.isActive('orderedList')) active.push('orderedList');
    if (editor.isActive('blockquote')) active.push('blockquote');
    if (editor.isActive('link')) active.push('link');
    this.activeMarks.set(active);
  }
}
