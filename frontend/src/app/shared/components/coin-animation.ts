import { Component, effect, HostListener, inject, Injectable, signal } from '@angular/core';

interface CoinBurstRequest {
  id: number;
  count: number;
}

interface CoinParticle {
  id: number;
  x: number;
  y: number;
  velocityX: number;
  velocityY: number;
  frame: number;
  bornAt: number;
}

@Injectable({ providedIn: 'root' })
export class CoinAnimationService {
  readonly request = signal<CoinBurstRequest | null>(null);
  private nextRequestId = 0;
  private pointerX = 0;
  private pointerY = 0;

  setPointerPosition(x: number, y: number): void {
    this.pointerX = x;
    this.pointerY = y;
  }

  getPointerPosition(): { x: number; y: number } {
    return { x: this.pointerX, y: this.pointerY };
  }

  spawn(count: number): void {
    const coinCount = Math.max(0, Math.floor(count));
    if (coinCount === 0) return;
    this.request.set({ id: ++this.nextRequestId, count: coinCount });
  }
}

@Component({
  selector: 'app-coin-animation',
  template: `
    <div class="coin-layer" aria-hidden="true">
      @for (coin of coins(); track coin.id) {
        <img
          class="coin"
          [src]="frames[coin.frame]"
          [style.transform]="'translate3d(' + coin.x + 'px, ' + coin.y + 'px, 0) translate(-50%, -50%)'"
          alt=""
          width="52"
          height="64"
        />
      }
    </div>
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      z-index: 1190;
      overflow: hidden;
      pointer-events: none;
    }

    .coin-layer {
      position: absolute;
      inset: 0;
      overflow: hidden;
    }

    .coin {
      position: absolute;
      top: 0;
      left: 0;
      width: auto;
      height: 64px;
      image-rendering: pixelated;
      will-change: transform;
    }
  `,
})
export class CoinAnimationView {
  protected readonly frames = [
    '/assets/coin/frame-0.png',
    '/assets/coin/frame-1.png',
    '/assets/coin/frame-2.png',
    '/assets/coin/frame-3.png',
    '/assets/coin/frame-4.png',
    '/assets/coin/frame-5.png',
  ];
  protected readonly coins = signal<CoinParticle[]>([]);

  private readonly animation = inject(CoinAnimationService);
  private readonly spawnTimers: number[] = [];
  private nextCoinId = 0;
  private animationFrame = 0;
  private lastTick = 0;

  constructor() {
    this.animation.setPointerPosition(window.innerWidth / 2, window.innerHeight / 2);

    for (const frame of this.frames) {
      const image = new Image();
      image.src = frame;
    }

    effect((onCleanup) => {
      const request = this.animation.request();
      if (!request) return;

      this.clearAll();
      for (let index = 0; index < request.count; index++) {
        this.spawnTimers.push(window.setTimeout(() => this.spawnCoin(), index * 30));
      }
      onCleanup(() => this.clearAll());
    });
  }

  @HostListener('document:keydown')
  protected interruptOnKeydown(): void {
    this.interrupt();
  }

  @HostListener('document:mousedown')
  protected interruptOnMouseButton(): void {
    this.interrupt();
  }

  @HostListener('document:mousemove', ['$event'])
  protected trackPointer(event: MouseEvent): void {
    this.animation.setPointerPosition(event.clientX, event.clientY);
  }

  private spawnCoin(): void {
    const now = performance.now();
    const origin = this.animation.getPointerPosition();
    const coin: CoinParticle = {
      id: ++this.nextCoinId,
      x: origin.x,
      y: origin.y,
      velocityX: (Math.random() - 0.5) * 420,
      velocityY: -500 - Math.random() * 360,
      frame: 0,
      bornAt: now,
    };
    this.coins.update((coins) => [...coins, coin]);
    if (this.animationFrame === 0) {
      this.lastTick = now;
      this.animationFrame = window.requestAnimationFrame(this.tick);
    }
  }

  private readonly tick = (now: number): void => {
    const delta = Math.min((now - this.lastTick) / 1000, 0.05);
    this.lastTick = now;
    const width = window.innerWidth;
    const height = window.innerHeight;

    this.coins.update((coins) =>
      coins.flatMap((coin) => {
        const velocityY = coin.velocityY + 900 * delta;
        const x = coin.x + coin.velocityX * delta;
        const y = coin.y + velocityY * delta;
        if (x < -64 || x > width + 64 || y < -80 || y > height + 80) return [];
        return [{
          ...coin,
          x,
          y,
          velocityY,
          frame: Math.floor((now - coin.bornAt) / 60) % this.frames.length,
        }];
      }),
    );

    if (this.coins().length > 0) {
      this.animationFrame = window.requestAnimationFrame(this.tick);
    } else {
      this.animationFrame = 0;
    }
  };

  private interrupt(): void {
    if (this.spawnTimers.length === 0 && this.coins().length === 0) return;
    this.clearAll();
  }

  private clearAll(): void {
    this.spawnTimers.forEach((timer) => window.clearTimeout(timer));
    this.spawnTimers.length = 0;
    if (this.animationFrame !== 0) {
      window.cancelAnimationFrame(this.animationFrame);
      this.animationFrame = 0;
    }
    this.coins.set([]);
  }
}