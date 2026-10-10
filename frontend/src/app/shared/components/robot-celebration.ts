import { Component, effect, HostListener, inject, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RobotCelebrationService {
  readonly requestId = signal(0);

  play(): void {
    this.requestId.update((requestId) => requestId + 1);
  }
}

@Component({
  selector: 'app-robot-celebration',
  template: `
    @if (visible()) {
      <div class="robot-cheer" role="status" aria-live="polite">
        <p>Good Job!</p>
        <img [src]="frames[frameIndex()]" alt="Celebration robot" width="304" height="368" />
      </div>
    }
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      z-index: 1200;
      display: grid;
      place-items: center;
      pointer-events: none;
    }

    .robot-cheer {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    p {
      margin: 0;
      color: #20313c;
      font: 36px/1.8 'Press Start 2P', 'Courier New', monospace;
      text-shadow: 2px 2px 0 #e7b83f;
      white-space: nowrap;
    }

    img {
      display: block;
      width: min(304px, calc(100vw - 32px));
      height: auto;
      image-rendering: pixelated;
    }
  `,
})
export class RobotCelebrationView {
  protected readonly frames = [
    '/assets/robot-cheer/frame-0.png',
    '/assets/robot-cheer/frame-1.png',
    '/assets/robot-cheer/frame-2.png',
    '/assets/robot-cheer/frame-3.png',
  ];
  protected readonly frameIndex = signal(0);
  protected readonly visible = signal(false);

  private readonly celebration = inject(RobotCelebrationService);
  private timers: number[] = [];

  constructor() {
    for (const frame of this.frames) {
      const image = new Image();
      image.src = frame;
    }

    effect((onCleanup) => {
      const requestId = this.celebration.requestId();
      if (requestId === 0) return;

      this.visible.set(true);
      this.frameIndex.set(0);
      const totalFrames = this.frames.length * 4;
      this.timers = Array.from({ length: totalFrames }, (_, index) => {
        const nextFrame = index + 1;
        return window.setTimeout(() => {
          if (nextFrame === totalFrames) {
            this.visible.set(false);
            this.frameIndex.set(0);
            this.timers = [];
          } else {
            this.frameIndex.set(nextFrame % this.frames.length);
          }
        }, nextFrame * 300);
      });

      onCleanup(() => this.clearTimers());
    });
  }

  @HostListener('document:keydown')
  protected interruptOnKeydown(): void {
    this.interrupt();
  }

  @HostListener('document:click')
  protected interruptOnClick(): void {
    this.interrupt();
  }

  private interrupt(): void {
    if (!this.visible()) return;
    this.clearTimers();
    this.visible.set(false);
    this.frameIndex.set(0);
  }

  private clearTimers(): void {
    this.timers.forEach((timer) => window.clearTimeout(timer));
    this.timers = [];
  }
}