import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom } from 'rxjs';

/**
 * Converts a one-shot HTTP Observable into a Promise that auto-unsubscribes
 * when the calling component is destroyed.
 */
export function injectHttpRequest() {
  const destroyRef = inject(DestroyRef);
  return <T>(source: Observable<T>) =>
    firstValueFrom(source.pipe(takeUntilDestroyed(destroyRef)));
}
