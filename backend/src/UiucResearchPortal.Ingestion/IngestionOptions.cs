namespace UiucResearchPortal.Ingestion;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>
    /// Cap on how many OpenAlex works to fetch. The assessment asks for 10,000-20,000; kept
    /// configurable so a quick smoke run (e.g. 20) doesn't burn the whole embedding API quota
    /// before a full run.
    /// </summary>
    public int MaxWorksToFetch { get; set; } = 12_000;
}
