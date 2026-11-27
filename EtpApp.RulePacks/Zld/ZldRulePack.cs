using EtpApp.Database.Enums;
using EtpApp.Engine.Abstractions;
using EtpApp.Engine.Civil;
using EtpApp.Engine.Costing;
using EtpApp.Engine.Models;
using EtpApp.Engine.Sizing.Clarifiers;
using EtpApp.Engine.Sizing.Tanks;

namespace EtpApp.RulePacks.Zld;

public sealed class ZldRulePack : IPlantRulePack
{
    private const double BodCodRatioMin = 0.3;
    private const double OilGreaseTrapTriggerMgL = 50;
    private const double DefaultBodMgL = 250;

    public PlantType PlantType => PlantType.Zld;

    public TrainDefinition SelectTrain(StreamBasis basis)
    {
        var operations = new List<UnitOperationType>
        {
            UnitOperationType.ScreenChamber
        };

        if (basis.OilGreaseMgL >= OilGreaseTrapTriggerMgL)
            operations.Add(UnitOperationType.OilGreaseTrap);

        operations.Add(UnitOperationType.Equalization);
        operations.Add(UnitOperationType.ChemicalDosing);

        var biodegradable = basis.CodMgL <= 0 || basis.BodCodRatio >= BodCodRatioMin;
        operations.Add(biodegradable ? UnitOperationType.Aeration : UnitOperationType.Mbbr);
        operations.Add(UnitOperationType.SecondaryClarifier);

        operations.Add(UnitOperationType.PressureSandFilter);
        operations.Add(UnitOperationType.ActivatedCarbonFilter);
        operations.Add(UnitOperationType.UltraFiltration);
        operations.Add(UnitOperationType.ReverseOsmosis);
        operations.Add(UnitOperationType.MultiEffectEvaporator);

        operations.Add(UnitOperationType.SludgeThickener);
        operations.Add(UnitOperationType.FilterPress);

        return new TrainDefinition
        {
            Name = "ZLD",
            Operations = operations
        };
    }

    public IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train)
    {
        var equipment = new List<SizedEquipment>();
        var bodIn = basis.BodMgL > 0 ? basis.BodMgL : DefaultBodMgL;

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