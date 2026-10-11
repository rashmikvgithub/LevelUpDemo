import { Component, effect, HostListener, inject, Injectable, signal } from '@angular/core';

export type RobotAnimationKind = 'good' | 'sad';

interface RobotAnimationRequest {
  id: number;
  kind: RobotAnimationKind;
}

@Injectable({ providedIn: 'root' })
export class RobotCelebrationService {
  readonly request = signal<RobotAnimationRequest | null>(null);
  private nextRequestId = 0;

  play(kind: RobotAnimationKind): void {
    this.request.set({ id: ++this.nextRequestId, kind });
  }
}

@Component({
  selector: 'app-robot-celebration',
  template: `
    @if (visible()) {
      <div class="robot-cheer" role="status" aria-live="polite">
        <p>{{ message() }}</p>
        <img [src]="frames()[frameIndex()]" [alt]="imageAlt()" width="304" height="368" />
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
      width: min(100%, calc(100vw - 16px));
    }

    p {
      max-width: 100%;
      margin: 0;
      color: #20313c;
      font: 72px/1.4 'Silkscreen', monospace;
      text-shadow: 2px 2px 0 #e7b83f;
      text-align: center;
      white-space: normal;
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
  private readonly goodFrames = [
    '/assets/robot-cheer/frame-0.png',
    '/assets/robot-cheer/frame-1.png',
    '/assets/robot-cheer/frame-2.png',
    '/assets/robot-cheer/frame-3.png',
  ];
  private readonly sadFrames = [
    '/assets/robot-sad/frame-4.png',
    '/assets/robot-sad/frame-3.png',
    '/assets/robot-sad/frame-2.png',
    '/assets/robot-sad/frame-1.png',
    '/assets/robot-sad/frame-0.png',
  ];
  protected readonly frames = signal(this.goodFrames);
  protected readonly message = signal('Good Job!');
  protected readonly imageAlt = signal('Celebration robot');
  protected readonly frameIndex = signal(0);
  protected readonly visible = signal(false);

  private readonly celebration = inject(RobotCelebrationService);
  private timers: number[] = [];

  constructor() {
    for (const frame of [...this.goodFrames, ...this.sadFrames]) {
      const image = new Image();
      image.src = frame;
    }

    effect((onCleanup) => {
      const request = this.celebration.request();
      if (!request) return;

      const isSad = request.kind === 'sad';
      const frames = isSad ? this.sadFrames : this.goodFrames;
      const frameDuration = isSad ? 400 : 300;
      this.frames.set(frames);
      this.message.set(isSad ? 'Oh NO!' : 'Good Job!');
      this.imageAlt.set(isSad ? 'Sad robot' : 'Celebration robot');

      this.visible.set(true);
      this.frameIndex.set(0);
      const totalFrames = frames.length * 4;
      this.timers = Array.from({ length: totalFrames }, (_, index) => {
        const nextFrame = index + 1;
        return window.setTimeout(() => {
          if (nextFrame === totalFrames) {
            this.visible.set(false);
            this.frameIndex.set(0);
            this.timers = [];
          } else {
            this.frameIndex.set(nextFrame % frames.length);
          }
        }, nextFrame * frameDuration);
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