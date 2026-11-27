
using EtpApp.Database.Enums;
using EtpApp.Engine.Costing;
using EtpApp.Engine.Models;

namespace EtpApp.Engine.Abstractions;

public interface IPlantRulePack
{
    PlantType PlantType { get; }

    TrainDefinition SelectTrain(StreamBasis basis);

    IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train);

    IReadOnlyList<BoqLineItem> BuildBoq(IEnumerable<SizedEquipment> equipment, RateSheet rates);
}