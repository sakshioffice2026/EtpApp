using EPCApp.Database.Enums;
using EPCApp.Engine.Abstractions;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Costing;
using EPCApp.Engine.Electrical;
using EPCApp.Engine.Mechanical;
using EPCApp.Engine.Models;
using EPCApp.Engine.Sizing.Biology;
using EPCApp.Engine.Sizing.Chemical;
using EPCApp.Engine.Sizing.Clarifiers;
using EPCApp.Engine.Sizing.Preliminary;
using EPCApp.Engine.Sizing.Separators;
using EPCApp.Engine.Sizing.Sludge;
using EPCApp.Engine.Sizing.Tanks;
using EPCApp.Engine.Sizing.Tertiary;
using EPCApp.Engine.Sizing.Zld;

namespace EPCApp.RulePacks.Stp;

public sealed class StpRulePack : IPlantRulePack
{
    private const double DefaultSewageBodMgL = 250;
    private const double DefaultSewageCodMgL = 500;
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

    public IReadOnlyList<SizedEquipment> Size(StreamBasis basis, TrainDefinition train, DesignBasis design, TrainPlan plan)
    {
        var equipment = new List<SizedEquipment>();

        plan.InletFlowM3PerDay = Math.Round(basis.FlowM3PerDay, 2);
        plan.TreatedWaterM3PerDay = Math.Round(basis.FlowM3PerDay, 2);

        var bodIn = basis.BodMgL > 0 ? basis.BodMgL : DefaultSewageBodMgL;
        var codIn = basis.CodMgL > 0 ? basis.CodMgL : DefaultSewageCodMgL;
        var hasThickener = false;
        var hasFilterPress = false;

        foreach (var operation in train.Operations)
        {
            switch (operation)
            {
                case UnitOperationType.ScreenChamber:
                    equipment.Add(ScreenChamberSizer.Size("SCR-101", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.OilGreaseTrap:
                    equipment.Add(OilGreaseTrapSizer.Size("OGT-102", basis.FlowM3PerDay, basis.OilGreaseMgL, design));
                    break;

                case UnitOperationType.ChemicalDosing:
                    equipment.Add(ChemicalDosingSizer.Size("CD-141", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.Equalization:
                    equipment.Add(EqualizationTankSizer.Size("EQ-101", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.Aeration:
                    equipment.Add(AerationTankSizer.Size("AT-201", basis.FlowM3PerDay, bodIn, design));
                    break;

                case UnitOperationType.Mbbr:
                    equipment.Add(MbbrSizer.Size("MB-201", basis.FlowM3PerDay, codIn, design));
                    break;

                case UnitOperationType.SecondaryClarifier:
                    equipment.Add(ClarifierSizer.Size("SC-301", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.PressureSandFilter:
                    equipment.Add(PressureSandFilterSizer.Size("PSF-311", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.ActivatedCarbonFilter:
                    equipment.Add(ActivatedCarbonFilterSizer.Size("ACF-321", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.Disinfection:
                    equipment.Add(DisinfectionSizer.Size("DIS-401", basis.FlowM3PerDay, design));
                    break;

                case UnitOperationType.SludgeThickener:
                    hasThickener = true;
                    break;

                case UnitOperationType.FilterPress:
                    hasFilterPress = true;
                    break;
            }
        }

        if (hasThickener || hasFilterPress)
        {
            SludgeBalance.Compute(equipment, basis, design, plan);

            if (plan.SludgeDryKgPerDay > 0)
            {
                if (hasThickener)
                    equipment.Add(SludgeThickenerSizer.Size("ST-501", plan, design));

                if (hasFilterPress)
                    equipment.Add(FilterPressSizer.Size("FP-502", plan, design));
            }
        }

        if (train.Operations.Contains(UnitOperationType.ReverseOsmosis))
        {
            var membraneFeedM3PerDay = plan.TreatedWaterM3PerDay > 0 ? plan.TreatedWaterM3PerDay : basis.FlowM3PerDay;
            var handoff = ZldHandoff.Compute(basis, design, plan);

            if (train.Operations.Contains(UnitOperationType.UltraFiltration))
                equipment.Add(UltraFiltrationSizer.Size("UF-411", membraneFeedM3PerDay, design));

            equipment.Add(ReverseOsmosisSizer.Size("RO-421", handoff.RoFeedM3PerDay, basis.TdsMgL, design));
        }

        return equipment;
    }

    public IReadOnlyList<BoqLineItem> BuildBoq(IEnumerable<SizedEquipment> equipment, DesignBasis design)
    {
        var list = equipment.ToList();
        var rates = design.Civil;
        var lines = new List<BoqLineItem>();

        foreach (var item in list.Where(x => x.Material == MaterialOfConstruction.RccM30))
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

        var mechanical = MechanicalBoq.Build(list, design);
        var electrical = ElectricalBoq.Build(list, mechanical, design);

        lines.AddRange(mechanical);
        lines.AddRange(electrical);

        return lines;
    }
}