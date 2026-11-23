export interface PaperAuthorSummary {
  researcherId: string | null;
  displayName: string;
}

export interface Paper {
  id: string;
  openAlexId: string;
  title: string;
  publicationDate: string;
  doi: string | null;
  topics: string[];
  authors: PaperAuthorSummary[];
}

export interface FocusArea {
  keyword: string;
  researcherCount: number;
}
