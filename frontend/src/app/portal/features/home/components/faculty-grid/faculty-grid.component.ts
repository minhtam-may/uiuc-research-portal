import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { PortalDataService } from '@shared/services';
import { Researcher } from '@shared/types';

@Component({
  selector: 'app-faculty-grid',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './faculty-grid.component.html',
  styleUrl: './faculty-grid.component.scss'
})
export class FacultyGridComponent implements OnInit {
  private readonly portalData = inject(PortalDataService);
  private readonly searchChanged = new Subject<string>();

  readonly researchers = signal<Researcher[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  ngOnInit(): void {
    this.load();

    this.searchChanged.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.load(term);
    });
  }

  onSearchInput(term: string): void {
    this.searchTerm.set(term);
    this.searchChanged.next(term);
  }

  private load(search?: string): void {
    this.isLoading.set(true);
    this.portalData.getResearchers(search).subscribe({
      next: (researchers) => {
        this.researchers.set(researchers);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }
}
