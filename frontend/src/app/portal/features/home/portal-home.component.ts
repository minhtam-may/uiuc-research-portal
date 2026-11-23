import { Component } from '@angular/core';
import { HeroComponent } from './components/hero/hero.component';
import { FocusAreasComponent } from './components/focus-areas/focus-areas.component';
import { PublicationsListComponent } from './components/publications-list/publications-list.component';
import { FacultyGridComponent } from './components/faculty-grid/faculty-grid.component';
import { ChatWindowComponent } from '../chat/components/chat-window/chat-window.component';

@Component({
  selector: 'app-portal-home',
  standalone: true,
  imports: [HeroComponent, FocusAreasComponent, PublicationsListComponent, FacultyGridComponent, ChatWindowComponent],
  templateUrl: './portal-home.component.html'
})
export class PortalHomeComponent {}
