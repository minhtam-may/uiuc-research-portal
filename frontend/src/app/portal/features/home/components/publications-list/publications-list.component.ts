import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalDataService } from '@shared/services';
import { Paper } from '@shared/types';

@Component({
  selector: 'app-publications-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './publications-list.component.html',
  styleUrl: './publications-list.component.scss'
})
export class PublicationsListComponent implements OnInit {
  private readonly portalData = inject(PortalDataService);

  readonly papers = signal<Paper[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.portalData.getRecentPapers().subscribe({
      next: (papers) => {
        this.papers.set(papers);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  authorNames(paper: Paper): string {
    return paper.authors.map((a) => a.displayName).join(', ');
  }
}
