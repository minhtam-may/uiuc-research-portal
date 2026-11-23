import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalDataService } from '@shared/services';
import { FocusArea } from '@shared/types';

@Component({
  selector: 'app-focus-areas',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatProgressSpinnerModule],
  templateUrl: './focus-areas.component.html',
  styleUrl: './focus-areas.component.scss'
})
export class FocusAreasComponent implements OnInit {
  private readonly portalData = inject(PortalDataService);

  readonly focusAreas = signal<FocusArea[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.portalData.getFocusAreas().subscribe({
      next: (areas) => {
        this.focusAreas.set(areas);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }
}
