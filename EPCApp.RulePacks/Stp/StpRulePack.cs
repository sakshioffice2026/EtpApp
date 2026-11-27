
using EPCApp.Database.Enums;
using EPCApp.Engine.Abstractions;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Costing;
using EPCApp.Engine.Models;
using EPCApp.Engine.Sizing.Clarifiers;
using EPCApp.Engine.Sizing.Tanks;

namespace EPCApp.RulePacks.Stp;

public sealed class StpRulePack : IPlantRulePack
{
    private const double DefaultSewageBodMgL = 250;
    private const double OilGreaseTrapTriggerMgL = 50;
    private const double MbbrFlowTriggerM3PerDay = 100;
    private const double TdsReuseTriggerMgL = 2000;

    public PlantType PlantType => PlantType.Stp;

    public TrainDefinition SelectTrain(StreamBasis basis)
    {
        var operations = new List<UnitOperationType>
        {
            UnitOperationType.ScreenChamber
        };

        if (basis.OilGreaseMgL >= OilGreaseTrapTriggerMgL)
            operations.Add(UnitOperationType.OilGreaseTrap);

        operations.Add(UnitOperationType.Equalization);

        operations.Add(basis.FlowM3PerDay <= MbbrFlowTriggerM3PerDay
            ? UnitOperationType.Mbbr
            : UnitOperationType.Aeration);

        operations.Add(UnitOperationType.SecondaryClarifier);
        operations.Add(UnitOperationType.ChemicalDosing);
        operations.Add(UnitOperationType.PressureSandFilter);
        operations.Add(UnitOperationType.ActivatedCarbonFilter);

        if (basis.TdsMgL > TdsReuseTriggerMgL)
        {
            operations.Add(UnitOperationType.UltraFiltration);
            operations.Add(UnitOperationType.ReverseOsmosis);
        }

        operations.Add(UnitOperationType.SludgeThickener);
        operations.Add(UnitOperationType.FilterPress);

        return new TrainDefinition
        {
            Name = "STP",
            Operations = operations
        };
    }

    public IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train)
    {
        var equipment = new List<SizedEquipment>();
        var bodIn = basis.BodMgL > 0 ? basis.BodMgL : DefaultSewageBodMgL;

        foreach (var operation in train.Operations)
        {
            switch (operation)
            {
                case UnitOperationType.Equalization:
                    equipment.Add(EqualizationTankSizer.Size("EQ-101", basis.FlowM3PerDay));
                    break;

                case UnitOperationType.Aeration:
                    equipment.Add(AerationTankSizer.Size("AT-201", basis.FlowM3PerDay, bodIn));
                    break;

                case UnitOperationType.SecondaryClarifier:
                    equipment.Add(ClarifierSizer.Size("SC-301", basis.FlowM3PerDay));
                    break;
            }
        }

        return equipment;
    }

    public IReadOnlyList<BoqLineItem> BuildBoq(IEnumerable<SizedEquipment> equipment, RateSheet rates)
    {
        var lines = new List<BoqLineItem>();

        foreach (var item in equipment)
        {
            var p = item.Parameters;

            if (p.TryGetValue("DiameterM", out var diameter) && p.TryGetValue("DepthM", out var circularDepth))
            {
                var units = p.TryGetValue("Units", out var count) ? count : 1;

                foreach (var line in CivilBoq.Circular(item.Tag, diameter, circularDepth, rates))
                    lines.Add(line with { Quantity = Math.Round(line.Quantity * units, 2) });
            }
            else if (p.TryGetValue("LengthM", out var length)
                     && p.TryGetValue("WidthM", out var width)
                     && p.TryGetValue("DepthM", out var depth))
            {
                lines.AddRange(CivilBoq.Rectangular(item.Tag, length, width, depth, rates));
            }
        }

        return lines;
    }
}