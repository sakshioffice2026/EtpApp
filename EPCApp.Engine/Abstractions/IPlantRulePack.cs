
using EPCApp.Database.Enums;
using EPCApp.Engine.Costing;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Abstractions;

public interface IPlantRulePack
{
    PlantType PlantType { get; }

    TrainDefinition SelectTrain(StreamBasis basis);

    IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train, DesignBasis design, TrainPlan plan);

    IReadOnlyList<BoqLineItem> BuildBoq(IEnumerable<SizedEquipment> equipment, DesignBasis design);
}