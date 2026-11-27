namespace EPCApp.Engine.Models;

public sealed class TrainPlan
{
    private readonly List<string> _notes = new();

    public IReadOnlyList<string> Notes => _notes;

    public double InletFlowM3PerDay { get; set; }

    public double TreatedWaterM3PerDay { get; set; }

    public double SludgeM3PerDay { get; set; }

    public double SludgeDryKgPerDay { get; set; }

    public double CakeKgPerDay { get; set; }

    public double RejectM3PerDay { get; set; }

    public double ZldFeedM3PerDay { get; set; }

    public double WaterLossM3PerDay =>
        Math.Max(0, InletFlowM3PerDay - TreatedWaterM3PerDay - SludgeM3PerDay - RejectM3PerDay);

    public void AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            _notes.Add(note.Trim());
        }
    }
}
