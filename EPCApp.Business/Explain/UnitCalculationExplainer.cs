using System.Globalization;
using EPCApp.Model.ViewModel;

namespace EPCApp.Business.Explain;

public static class UnitCalculationExplainer
{
    public static string Purpose(string unitTypeName) => unitTypeName switch
    {
        "ScreenChamber" => "Removes coarse floating and suspended material before the pumps and tanks.",
        "OilGreaseTrap" => "Skims free oil and grease off the wastewater before biological treatment.",
        "Equalization" => "Holds and blends the raw wastewater so flow and load reach the treatment units evenly.",
        "ChemicalDosing" => "Doses chemicals into the wastewater as needed by the downstream units.",
        "PrimaryClarifier" => "Settles suspended solids out of the wastewater and collects them as primary sludge.",
        "Aeration" => "Supplies oxygen to the biomass so it breaks down the organic load (BOD) in the wastewater.",
        "Mbbr" => "Moving-bed biofilm reactor: biofilm grown on floating media removes the organic load (COD).",
        "SecondaryClarifier" => "Separates the biomass from the treated water and returns part of it to the aeration tank.",
        "SludgeThickener" => "Thickens the sludge so less water has to be handled in dewatering.",
        "FilterPress" => "Squeezes the water out of thickened sludge to form a solid cake.",
        "PressureSandFilter" => "Filters out fine suspended solids that carry over from the clarifier.",
        "ActivatedCarbonFilter" => "Adsorbs remaining colour, odour and dissolved organics.",
        "UltraFiltration" => "Membrane filtration that removes fine particles before reverse osmosis.",
        "ReverseOsmosis" => "Membrane process that removes dissolved salts and produces reusable water.",
        "MultiEffectEvaporator" => "Evaporates water from the RO reject to concentrate the dissolved salts.",
        "PhNeutralization" => "Adjusts the pH of the wastewater to the target value using acid or alkali.",
        "CoagulationFlocculation" => "Chemicals bind fine particles into larger flocs that can settle or float out.",
        "DafApiSeparator" => "Removes free and emulsified oil and grease by gravity and dissolved air flotation.",
        "AnaerobicReactor" => "Breaks down the organic load without oxygen and produces biogas.",
        "Disinfection" => "Kills pathogens in the treated water by chlorine contact.",
        "Softener" => "Ion-exchange resin removes hardness so downstream units do not scale.",
        "AtfdCrystallizer" => "Agitated thin-film dryer with crystallizer that turns the concentrate into dry solids.",
        _ => "Treatment unit in the selected train."
    };

    public static UnitExplanation Explain(string unitTypeName, IReadOnlyDictionary<string, decimal> parameters)
    {
        var c = new Ctx(parameters);

        switch (unitTypeName)
        {
            case "ScreenChamber": ScreenChamber(c); break;
            case "OilGreaseTrap": OilGreaseTrap(c); break;
            case "ChemicalDosing": ChemicalDosing(c); break;
            case "PressureSandFilter": PressureSandFilter(c); break;
            case "ActivatedCarbonFilter": ActivatedCarbonFilter(c); break;
            case "UltraFiltration": UltraFiltration(c); break;
            case "ReverseOsmosis": ReverseOsmosis(c); break;
            case "MultiEffectEvaporator": MultiEffectEvaporator(c); break;
            case "Equalization": Equalization(c); break;
            case "Aeration": Aeration(c); break;
            case "Mbbr": Mbbr(c); break;
            case "AnaerobicReactor": Anaerobic(c); break;
            case "SecondaryClarifier": SecondaryClarifier(c); break;
            case "PrimaryClarifier": PrimaryClarifier(c); break;
            case "CoagulationFlocculation": Coagulation(c); break;
            case "PhNeutralization": PhNeutralization(c); break;
            case "DafApiSeparator": Daf(c); break;
            case "SludgeThickener": Thickener(c); break;
            case "FilterPress": FilterPress(c); break;
            case "Disinfection": Disinfection(c); break;
            case "Softener": Softener(c); break;
            case "AtfdCrystallizer": Atfd(c); break;
        }

        return new UnitExplanation { Inputs = c.Inputs, Steps = c.Steps };
    }

    private static void ScreenChamber(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Peak factor", "PeakFactor", "");
        c.In("Approach velocity", "ApproachVelocityMPerSec", "m/s");
        c.In("Water depth", "WaterDepthM", "m");
        c.In("Clear bar spacing", "ClearSpacingMm", "mm");
        c.In("Bar thickness", "BarThicknessMm", "mm");

        c.Step("Peak flow", "Peak flow = flow ÷ seconds per day × peak factor",
            $"{c.N("FlowM3PerDay")} ÷ seconds per day × {c.N("PeakFactor")}", "PeakFlowM3PerSec", "m³/s");
        c.Step("Flow area", "Area = peak flow ÷ approach velocity",
            $"{c.N("PeakFlowM3PerSec")} ÷ {c.N("ApproachVelocityMPerSec")}", "FlowAreaM2", "m²");
        c.Add("Channel width", "Width = flow area ÷ water depth, rounded up to the next practical size",
            $"{c.N("FlowAreaM2")} ÷ {c.N("WaterDepthM")}", c.N("WidthM"), "m");
        c.Add("Channel size", "Length × width × total depth (water depth + freeboard)",
            $"{c.N("LengthM")} × {c.N("WidthM")} × {c.N("DepthM")}",
            $"{c.N("LengthM")} × {c.N("WidthM")} × {c.N("DepthM")}", "m (L × W × D)");
        c.Step("Number of bars", "Bars = (channel width − clear spacing) ÷ (clear spacing + bar thickness), rounded down",
            $"(channel width {c.N("WidthM")} m − {c.N("ClearSpacingMm")} mm) ÷ ({c.N("ClearSpacingMm")} mm + {c.N("BarThicknessMm")} mm)",
            "BarCount", "no.");

        var rate = Div(c.V("ScreeningsM3PerDay"), c.V("FlowM3PerDay"));
        c.Step("Screenings", "Screenings = flow × screenings per m³ of flow",
            $"{c.N("FlowM3PerDay")} × {Fmt(rate)}", "ScreeningsM3PerDay", "m³/day");
    }

    private static void OilGreaseTrap(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet oil & grease", "OilGreaseInMgL", "mg/L");
        c.In("Peak factor", "PeakFactor", "");
        c.In("Retention time", "RetentionMinutes", "min");
        c.In("Water depth", "WaterDepthM", "m");
        c.In("Removal fraction", "RemovalFraction", "");

        c.Step("Peak flow", "Peak flow = flow ÷ hours per day × peak factor",
            $"{c.N("FlowM3PerDay")} ÷ hours per day × {c.N("PeakFactor")}", "PeakFlowM3PerHour", "m³/h");
        c.Step("Trap volume", "Volume = peak flow × retention time ÷ minutes per hour",
            $"{c.N("PeakFlowM3PerHour")} × {c.N("RetentionMinutes")} ÷ minutes per hour", "VolumeM3", "m³");
        Rect(c);
        c.Step("Oil load", "Oil load = flow × inlet oil & grease ÷ mg per kg conversion",
            $"{c.N("FlowM3PerDay")} × {c.N("OilGreaseInMgL")} ÷ conversion", "OilLoadKgPerDay", "kg/day");
        c.Step("Oil removed", "Oil removed = oil load × removal fraction",
            $"{c.N("OilLoadKgPerDay")} × {c.N("RemovalFraction")}", "OilRemovedKgPerDay", "kg/day");
        c.Step("Outlet oil & grease", "Outlet = inlet × (1 − removal fraction)",
            $"{c.N("OilGreaseInMgL")} × (1 − {c.N("RemovalFraction")})", "OilGreaseOutMgL", "mg/L");
    }

    private static void ChemicalDosing(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Dose", "DoseMgL", "mg/L");
        c.In("Solution strength", "StrengthFraction", "");
        c.In("Solution density", "DensityKgPerL", "kg/L");
        c.In("Storage", "StorageDays", "days");
        c.In("Pump margin", "PumpMargin", "");
        c.In("Tank fill fraction", "TankFillFraction", "");

        c.Step("Reagent quantity", "Reagent = flow × dose ÷ mg per kg conversion",
            $"{c.N("FlowM3PerDay")} × {c.N("DoseMgL")} ÷ conversion", "ReagentKgPerDay", "kg/day");
        c.Step("Solution mass", "Solution = reagent ÷ solution strength",
            $"{c.N("ReagentKgPerDay")} ÷ {c.N("StrengthFraction")}", "SolutionKgPerDay", "kg/day");
        c.Step("Solution volume", "Volume = solution mass ÷ solution density",
            $"{c.N("SolutionKgPerDay")} ÷ {c.N("DensityKgPerL")}", "SolutionLPerDay", "L/day");
        c.Step("Solution per hour", "Per hour = solution volume ÷ hours per day",
            $"{c.N("SolutionLPerDay")} ÷ hours per day", "SolutionLPerHour", "L/h");
        c.Step("Dosing pump capacity", "Pump = solution per hour × pump margin",
            $"{c.N("SolutionLPerHour")} × {c.N("PumpMargin")}", "DosingPumpLPerHour", "L/h");
        c.Step("Storage tank volume", "Tank = solution volume × storage days ÷ litres per m³ ÷ tank fill fraction",
            $"{c.N("SolutionLPerDay")} × {c.N("StorageDays")} ÷ litres per m³ ÷ {c.N("TankFillFraction")}",
            "TankVolumeM3", "m³");
    }

    private static void PressureSandFilter(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");
        c.In("Filtration rate", "FiltrationRateMPerHour", "m/h");
        c.In("Media depth", "MediaDepthM", "m");
        c.In("Backwash rate", "BackwashRateMPerHour", "m/h");
        c.In("Backwash time", "BackwashMinutes", "min");

        c.Step("Flow per hour", "Flow per hour = flow ÷ operating hours",
            $"{c.N("FlowM3PerDay")} ÷ {c.N("OperatingHoursPerDay")}", "FlowM3PerHour", "m³/h");
        c.Step("Required filter area", "Area = flow per hour ÷ filtration rate",
            $"{c.N("FlowM3PerHour")} ÷ {c.N("FiltrationRateMPerHour")}", "RequiredAreaM2", "m²");
        c.Step("Duty vessels", "Duty vessels = required area ÷ maximum area of one vessel, rounded up",
            $"{c.N("RequiredAreaM2")} m² split into {c.N("DutyUnits")} duty vessel(s)", "DutyUnits", "no.");
        c.Step("Vessel diameter", "Diameter = √(4 × (required area ÷ duty vessels) ÷ π), rounded up to the next practical size",
            $"√(4 × ({c.N("RequiredAreaM2")} ÷ {c.N("DutyUnits")}) ÷ π)", "VesselDiameterM", "m");
        c.Step("Area of one vessel", "Area = π × diameter² ÷ 4",
            $"π × {c.N("VesselDiameterM")}² ÷ 4", "AreaPerUnitM2", "m²");
        c.Step("Total vessels", "Total vessels = duty vessels + standby",
            $"{c.N("DutyUnits")} duty + {c.N("StandbyUnits")} standby", "Units", "no.");
        c.Step("Media volume", "Media = area of one vessel × media depth × total vessels",
            $"{c.N("AreaPerUnitM2")} × {c.N("MediaDepthM")} × {c.N("Units")}", "MediaVolumeM3", "m³");
        c.Step("Backwash flow", "Backwash flow = area of one vessel × backwash rate",
            $"{c.N("AreaPerUnitM2")} × {c.N("BackwashRateMPerHour")}", "BackwashFlowM3PerHour", "m³/h");
        c.Step("Backwash volume", "Volume = backwash flow × backwash time ÷ minutes per hour",
            $"{c.N("BackwashFlowM3PerHour")} × {c.N("BackwashMinutes")} ÷ minutes per hour", "BackwashVolumeM3", "m³");
    }

    private static void ActivatedCarbonFilter(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");
        c.In("Filtration rate", "FiltrationRateMPerHour", "m/h");
        c.In("Minimum contact time (EBCT)", "MinEbctMinutes", "min");
        c.In("Carbon density", "CarbonDensityKgPerM3", "kg/m³");

        c.Step("Flow per hour", "Flow per hour = flow ÷ operating hours",
            $"{c.N("FlowM3PerDay")} ÷ {c.N("OperatingHoursPerDay")}", "FlowM3PerHour", "m³/h");
        c.Step("Required filter area", "Area = flow per hour ÷ filtration rate",
            $"{c.N("FlowM3PerHour")} ÷ {c.N("FiltrationRateMPerHour")}", "RequiredAreaM2", "m²");
        c.Step("Duty vessels", "Duty vessels = required area ÷ maximum area of one vessel, rounded up",
            $"{c.N("RequiredAreaM2")} m² split into {c.N("DutyUnits")} duty vessel(s)", "DutyUnits", "no.");
        c.Step("Vessel diameter", "Diameter = √(4 × (required area ÷ duty vessels) ÷ π), rounded up to the next practical size",
            $"√(4 × ({c.N("RequiredAreaM2")} ÷ {c.N("DutyUnits")}) ÷ π)", "VesselDiameterM", "m");
        c.Step("Area of one vessel", "Area = π × diameter² ÷ 4",
            $"π × {c.N("VesselDiameterM")}² ÷ 4", "AreaPerUnitM2", "m²");
        c.Step("Bed depth", "Bed depth is raised if needed so the contact time meets the minimum",
            $"Minimum contact time {c.N("MinEbctMinutes")} min at {c.N("FlowM3PerHour")} m³/h", "MediaDepthM", "m");
        c.Step("Contact time (EBCT)", "EBCT = (area of one vessel × bed depth × duty vessels) ÷ flow per hour × minutes per hour",
            $"({c.N("AreaPerUnitM2")} × {c.N("MediaDepthM")} × {c.N("DutyUnits")}) ÷ {c.N("FlowM3PerHour")} × minutes per hour",
            "EbctMinutes", "min");
        c.Step("Total vessels", "Total vessels = duty vessels + standby",
            $"{c.N("DutyUnits")} duty + {c.N("StandbyUnits")} standby", "Units", "no.");
        c.Step("Carbon volume", "Carbon = area of one vessel × bed depth × total vessels",
            $"{c.N("AreaPerUnitM2")} × {c.N("MediaDepthM")} × {c.N("Units")}", "MediaVolumeM3", "m³");
        c.Step("Carbon mass", "Mass = carbon volume × carbon density",
            $"{c.N("MediaVolumeM3")} × {c.N("CarbonDensityKgPerM3")}", "CarbonKg", "kg");
    }

    private static void UltraFiltration(Ctx c)
    {
        c.In("Feed", "FlowM3PerDay", "m³/day");
        c.In("Recovery", "RecoveryFraction", "");
        c.In("Design flux", "DesignFluxLmh", "L/m²/h");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");
        c.In("Module area", "ModuleAreaM2", "m²");
        c.In("Backwash flux", "BackwashFluxLmh", "L/m²/h");

        c.Step("Permeate", "Permeate = feed × recovery",
            $"{c.N("FlowM3PerDay")} × {c.N("RecoveryFraction")}", "PermeateM3PerDay", "m³/day");
        c.Step("Reject", "Reject = feed − permeate",
            $"{c.N("FlowM3PerDay")} − {c.N("PermeateM3PerDay")}", "RejectM3PerDay", "m³/day");
        c.Step("Permeate per hour", "Per hour = permeate ÷ operating hours",
            $"{c.N("PermeateM3PerDay")} ÷ {c.N("OperatingHoursPerDay")}", "PermeateM3PerHour", "m³/h");
        c.Step("Required membrane area", "Area = permeate per hour × litres per m³ ÷ design flux",
            $"{c.N("PermeateM3PerHour")} × litres per m³ ÷ {c.N("DesignFluxLmh")}", "RequiredAreaM2", "m²");
        c.Step("Number of modules", "Modules = required area ÷ module area, rounded up",
            $"{c.N("RequiredAreaM2")} ÷ {c.N("ModuleAreaM2")}", "Modules", "no.");
        c.Step("Installed membrane area", "Installed area = modules × module area",
            $"{c.N("Modules")} × {c.N("ModuleAreaM2")}", "InstalledAreaM2", "m²");
        c.Step("Operating flux", "Flux = permeate per hour × litres per m³ ÷ installed area",
            $"{c.N("PermeateM3PerHour")} × litres per m³ ÷ {c.N("InstalledAreaM2")}", "OperatingFluxLmh", "L/m²/h");
        c.Step("Backwash flow", "Backwash flow = installed area × backwash flux ÷ litres per m³",
            $"{c.N("InstalledAreaM2")} × {c.N("BackwashFluxLmh")} ÷ litres per m³", "BackwashFlowM3PerHour", "m³/h");
        c.Step("Backwash volume", "Volume = backwash flow × backwash time ÷ seconds per hour",
            $"{c.N("BackwashFlowM3PerHour")} × backwash time ÷ seconds per hour", "BackwashVolumeM3", "m³",
            "Backwash time and interval come from the design rules.");
        c.Step("Backwashes per day", "Backwashes = operating hours × minutes per hour ÷ backwash interval",
            $"{c.N("OperatingHoursPerDay")} × minutes per hour ÷ backwash interval", "BackwashesPerDay", "per day");
    }

    private static void ReverseOsmosis(Ctx c)
    {
        c.In("Feed", "FlowM3PerDay", "m³/day");
        c.In("Feed TDS", "FeedTdsMgL", "mg/L");
        c.In("Recovery", "RecoveryFraction", "");
        c.In("Salt rejection", "RejectionFraction", "");
        c.In("Design flux", "DesignFluxLmh", "L/m²/h");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");
        c.In("Element area", "ElementAreaM2", "m²");
        c.In("Elements per vessel", "ElementsPerVessel", "no.");
        c.In("Net driving pressure", "NetDrivingPressureBar", "bar");
        c.In("Pump efficiency", "PumpEfficiency", "");

        c.Step("Permeate", "Permeate = feed × recovery",
            $"{c.N("FlowM3PerDay")} × {c.N("RecoveryFraction")}", "PermeateM3PerDay", "m³/day");
        c.Step("Reject", "Reject = feed − permeate",
            $"{c.N("FlowM3PerDay")} − {c.N("PermeateM3PerDay")}", "RejectM3PerDay", "m³/day");
        c.Step("Permeate TDS", "Permeate TDS = feed TDS × (1 − salt rejection)",
            $"{c.N("FeedTdsMgL")} × (1 − {c.N("RejectionFraction")})", "PermeateTdsMgL", "mg/L");
        c.Step("Reject TDS", "Reject TDS = (feed TDS × feed − permeate TDS × permeate) ÷ reject",
            $"({c.N("FeedTdsMgL")} × {c.N("FlowM3PerDay")} − {c.N("PermeateTdsMgL")} × {c.N("PermeateM3PerDay")}) ÷ {c.N("RejectM3PerDay")}",
            "RejectTdsMgL", "mg/L");
        c.Step("Feed per hour", "Per hour = feed ÷ operating hours",
            $"{c.N("FlowM3PerDay")} ÷ {c.N("OperatingHoursPerDay")}", "FeedM3PerHour", "m³/h");
        c.Step("Permeate per hour", "Per hour = permeate ÷ operating hours",
            $"{c.N("PermeateM3PerDay")} ÷ {c.N("OperatingHoursPerDay")}", "PermeateM3PerHour", "m³/h");
        c.Step("Required membrane area", "Area = permeate per hour × litres per m³ ÷ design flux",
            $"{c.N("PermeateM3PerHour")} × litres per m³ ÷ {c.N("DesignFluxLmh")}", "RequiredAreaM2", "m²");
        c.Step("Number of vessels", "Vessels = (required area ÷ element area) ÷ elements per vessel, rounded up",
            $"({c.N("RequiredAreaM2")} ÷ {c.N("ElementAreaM2")}) ÷ {c.N("ElementsPerVessel")}", "Vessels", "no.");
        c.Step("Number of elements", "Elements = vessels × elements per vessel",
            $"{c.N("Vessels")} × {c.N("ElementsPerVessel")}", "Elements", "no.");
        c.Step("Installed membrane area", "Installed area = elements × element area",
            $"{c.N("Elements")} × {c.N("ElementAreaM2")}", "InstalledAreaM2", "m²");
        c.Step("Operating flux", "Flux = permeate per hour × litres per m³ ÷ installed area",
            $"{c.N("PermeateM3PerHour")} × litres per m³ ÷ {c.N("InstalledAreaM2")}", "OperatingFluxLmh", "L/m²/h");
        c.Step("Osmotic pressure", "Osmotic pressure = average of feed and reject TDS × osmotic coefficient",
            $"average of {c.N("FeedTdsMgL")} and {c.N("RejectTdsMgL")} mg/L × osmotic coefficient", "OsmoticPressureBar", "bar");
        c.Step("Feed pressure", "Feed pressure = osmotic pressure + net driving pressure",
            $"{c.N("OsmoticPressureBar")} + {c.N("NetDrivingPressureBar")}", "FeedPressureBar", "bar");
        c.Step("High-pressure pump power", "Power = feed per hour × feed pressure ÷ (conversion factor × pump efficiency)",
            $"{c.N("FeedM3PerHour")} × {c.N("FeedPressureBar")} ÷ (conversion factor × {c.N("PumpEfficiency")})",
            "HighPressurePumpKw", "kW");
    }

    private static void MultiEffectEvaporator(Ctx c)
    {
        c.In("Feed", "FlowM3PerDay", "m³/day");
        c.In("Feed TDS", "FeedTdsMgL", "mg/L");
        c.In("Concentrate fraction", "ConcentrateFraction", "");
        c.In("Number of effects", "Effects", "no.");
        c.In("Economy per effect", "EconomyPerEffect", "");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");
        c.In("Heat transfer coefficient", "UKwPerM2K", "kW/m²·K");
        c.In("Temperature drop per effect", "DeltaTPerEffectK", "K");
        c.In("Latent heat", "LatentHeatKjPerKg", "kJ/kg");

        c.Step("Concentrate", "Concentrate = feed × concentrate fraction",
            $"{c.N("FlowM3PerDay")} × {c.N("ConcentrateFraction")}", "ConcentrateM3PerDay", "m³/day");
        c.Step("Concentrate TDS", "Concentrate TDS = feed TDS ÷ concentrate fraction",
            $"{c.N("FeedTdsMgL")} ÷ {c.N("ConcentrateFraction")}", "ConcentrateTdsMgL", "mg/L");
        c.Step("Water evaporated", "Evaporation = feed − concentrate",
            $"{c.N("FlowM3PerDay")} − {c.N("ConcentrateM3PerDay")}", "EvaporationM3PerDay", "m³/day");
        c.Step("Evaporation per hour", "Per hour = evaporation × kg per m³ ÷ operating hours",
            $"{c.N("EvaporationM3PerDay")} × kg per m³ ÷ {c.N("OperatingHoursPerDay")}", "EvaporationKgPerHour", "kg/h");
        c.Step("Steam economy", "Economy = number of effects × economy per effect",
            $"{c.N("Effects")} × {c.N("EconomyPerEffect")}", "SteamEconomy", "kg water/kg steam");
        c.Step("Steam consumption", "Steam = evaporation per hour ÷ steam economy",
            $"{c.N("EvaporationKgPerHour")} ÷ {c.N("SteamEconomy")}", "SteamKgPerHour", "kg/h");
        c.Step("Area per effect", "Area = (evaporation per hour ÷ effects ÷ seconds per hour) × latent heat ÷ (heat transfer coefficient × temperature drop)",
            $"({c.N("EvaporationKgPerHour")} ÷ {c.N("Effects")} ÷ seconds per hour) × {c.N("LatentHeatKjPerKg")} ÷ ({c.N("UKwPerM2K")} × {c.N("DeltaTPerEffectK")})",
            "AreaPerEffectM2", "m²");
        c.Step("Total heat transfer area", "Total area = area per effect × number of effects",
            $"{c.N("AreaPerEffectM2")} × {c.N("Effects")}", "HeatTransferAreaM2", "m²");
        c.Step("Condenser duty", "Duty = (last-effect vapour ÷ seconds per hour) × latent heat",
            $"({c.N("EvaporationKgPerHour")} ÷ {c.N("Effects")} ÷ seconds per hour) × {c.N("LatentHeatKjPerKg")}",
            "CondenserDutyKw", "kW");
        c.Step("Cooling water", "Cooling water = condenser duty ÷ (water heat capacity × cooling water temperature rise) × conversion",
            $"{c.N("CondenserDutyKw")} ÷ (water heat capacity × temperature rise) × conversion", "CoolingWaterM3PerHour", "m³/h");
    }

    private static void Equalization(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Retention time", "HrtHours", "hours");
        c.In("Water depth", "WaterDepthM", "m");

        c.Step("Tank volume", "Volume = flow × retention time ÷ 24",
            $"{c.N("FlowM3PerDay")} × {c.N("HrtHours")} ÷ 24", "VolumeM3", "m³");
        Rect(c);

        var rate = Div(c.V("MixingAirM3PerHour"), c.V("VolumeM3"));
        c.Step("Mixing air", "Mixing air = volume × air rate per m³ of tank",
            $"{c.N("VolumeM3")} × {Fmt(rate)}", "MixingAirM3PerHour", "m³/h");
    }

    private static void Aeration(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet BOD", "BodInMgL", "mg/L");
        c.In("Outlet BOD target", "BodOutMgL", "mg/L");
        c.In("MLSS", "MlssMgL", "mg/L");
        c.In("Food to microorganism ratio (F/M)", "FoodToMicroorganism", "kg BOD/kg MLSS/day");

        c.Step("BOD load", "BOD load = flow × inlet BOD ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("BodInMgL")} ÷ 1000", "BodLoadKgPerDay", "kg/day");
        c.Step("Tank volume", "Volume = BOD load ÷ (F/M × MLSS ÷ 1000)",
            $"{c.N("BodLoadKgPerDay")} ÷ ({c.N("FoodToMicroorganism")} × {c.N("MlssMgL")} ÷ 1000)", "VolumeM3", "m³");
        c.Step("Retention time", "HRT = volume ÷ flow × 24",
            $"{c.N("VolumeM3")} ÷ {c.N("FlowM3PerDay")} × 24", "HrtHours", "hours");
        c.Step("Volumetric loading", "Loading = BOD load ÷ volume",
            $"{c.N("BodLoadKgPerDay")} ÷ {c.N("VolumeM3")}", "VolumetricLoadingKgBodPerM3PerDay", "kg BOD/m³/day");
        Rect(c);

        var bodRemoved = Math.Max(c.V("BodInMgL") - c.V("BodOutMgL"), 0m);
        var o2Factor = Div(c.V("OxygenRequiredKgPerDay") * 1000m, c.V("FlowM3PerDay") * bodRemoved);
        c.Step("Oxygen required", "Oxygen = flow × (inlet BOD − outlet BOD) ÷ 1000 × oxygen per kg BOD",
            $"{c.N("FlowM3PerDay")} × ({c.N("BodInMgL")} − {c.N("BodOutMgL")}) ÷ 1000 × {Fmt(o2Factor)}",
            "OxygenRequiredKgPerDay", "kg O₂/day");

        var ote = Div(c.V("OxygenRequiredKgPerDay"), 0.28m * c.V("AirRequiredM3PerHour") * 24m);
        c.Step("Air required", "Air = oxygen ÷ (0.28 kg O₂ per m³ air × oxygen transfer efficiency) ÷ 24",
            $"{c.N("OxygenRequiredKgPerDay")} ÷ (0.28 × {Fmt(ote)}) ÷ 24", "AirRequiredM3PerHour", "m³/h");
        Blower(c);
    }

    private static void Mbbr(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet COD", "CodInMgL", "mg/L");
        c.In("Outlet COD target", "CodOutMgL", "mg/L");
        c.In("Surface area loading rate", "SurfaceLoadGCodPerM2PerDay", "g COD/m²/day");
        c.In("Media specific surface area", "MediaSurfaceAreaM2PerM3", "m²/m³");
        c.In("Media fill fraction", "MediaFillFraction", "");

        c.Step("COD removed", "COD removed = flow × (inlet COD − outlet COD) ÷ 1000",
            $"{c.N("FlowM3PerDay")} × ({c.N("CodInMgL")} − {c.N("CodOutMgL")}) ÷ 1000", "CodRemovedKgPerDay", "kg/day");
        c.Step("Media surface area", "Area = COD removed × 1000 ÷ surface area loading rate",
            $"{c.N("CodRemovedKgPerDay")} × 1000 ÷ {c.N("SurfaceLoadGCodPerM2PerDay")}", "MediaAreaM2", "m²");
        c.Step("Media volume", "Media volume = media area ÷ media specific surface area",
            $"{c.N("MediaAreaM2")} ÷ {c.N("MediaSurfaceAreaM2PerM3")}", "MediaVolumeM3", "m³");
        c.Step("Reactor volume", "Reactor volume = media volume ÷ fill fraction",
            $"{c.N("MediaVolumeM3")} ÷ {c.N("MediaFillFraction")}", "VolumeM3", "m³");
        c.Step("Retention time", "HRT = reactor volume ÷ flow × 24",
            $"{c.N("VolumeM3")} ÷ {c.N("FlowM3PerDay")} × 24", "HrtHours", "hours");
        Rect(c);

        var o2Factor = Div(c.V("OxygenRequiredKgPerDay"), c.V("CodRemovedKgPerDay"));
        c.Step("Oxygen required", "Oxygen = COD removed × oxygen per kg COD",
            $"{c.N("CodRemovedKgPerDay")} × {Fmt(o2Factor)}", "OxygenRequiredKgPerDay", "kg O₂/day");

        var ote = Div(c.V("OxygenRequiredKgPerDay"), 0.28m * c.V("AirRequiredM3PerHour") * 24m);
        c.Step("Air required", "Air = oxygen ÷ (0.28 kg O₂ per m³ air × oxygen transfer efficiency) ÷ 24",
            $"{c.N("OxygenRequiredKgPerDay")} ÷ (0.28 × {Fmt(ote)}) ÷ 24", "AirRequiredM3PerHour", "m³/h");
        Blower(c);
    }

    private static void Anaerobic(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet COD", "CodInMgL", "mg/L");
        c.In("Organic loading rate", "OrganicLoadingKgCodPerM3PerDay", "kg COD/m³/day");
        c.In("COD removal fraction", "CodRemovalFraction", "");
        c.In("Water depth", "WaterDepthM", "m");

        c.Step("COD load", "COD load = flow × inlet COD ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("CodInMgL")} ÷ 1000", "CodLoadKgPerDay", "kg/day");

        var volByLoad = Div(c.V("CodLoadKgPerDay"), c.V("OrganicLoadingKgCodPerM3PerDay"));
        c.Add("Volume by organic loading", "Volume = COD load ÷ organic loading rate",
            $"{c.N("CodLoadKgPerDay")} ÷ {c.N("OrganicLoadingKgCodPerM3PerDay")}", Fmt(volByLoad), "m³");

        c.Step("Reactor volume", "Volume = larger of (volume by organic loading, volume by upflow velocity limit)",
            $"Volume by loading is {Fmt(volByLoad)} m³; the upflow velocity limit is checked against it", "VolumeM3", "m³");
        c.Step("Plan area", "Area = volume ÷ water depth",
            $"{c.N("VolumeM3")} ÷ {c.N("WaterDepthM")}", "AreaM2", "m²");
        c.Step("Diameter", "Diameter = √(4 × area ÷ π)",
            $"√(4 × {c.N("AreaM2")} ÷ π)", "DiameterM", "m");
        c.Add("Total depth", "Total depth = water depth + freeboard",
            $"{c.N("WaterDepthM")} + {Fmt(c.V("DepthM") - c.V("WaterDepthM"))}", c.N("DepthM"), "m");
        c.Step("Retention time", "HRT = volume ÷ flow × 24",
            $"{c.N("VolumeM3")} ÷ {c.N("FlowM3PerDay")} × 24", "HrtHours", "hours");
        c.Step("Upflow velocity", "Upflow velocity = flow per hour ÷ plan area",
            $"({c.N("FlowM3PerDay")} ÷ 24) ÷ {c.N("AreaM2")}", "UpflowVelocityMPerHour", "m/h");
        c.Step("COD removed", "COD removed = COD load × removal fraction",
            $"{c.N("CodLoadKgPerDay")} × {c.N("CodRemovalFraction")}", "CodRemovedKgPerDay", "kg/day");
        c.Step("Outlet COD", "Outlet COD = inlet COD × (1 − removal fraction)",
            $"{c.N("CodInMgL")} × (1 − {c.N("CodRemovalFraction")})", "CodOutMgL", "mg/L");

        var yield = Div(c.V("BiogasM3PerDay"), c.V("CodRemovedKgPerDay"));
        c.Step("Biogas produced", "Biogas = COD removed × biogas yield per kg COD removed",
            $"{c.N("CodRemovedKgPerDay")} × {Fmt(yield)}", "BiogasM3PerDay", "m³/day");
    }

    private static void SecondaryClarifier(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Surface overflow rate", "SurfaceOverflowRateM3PerM2PerDay", "m³/m²/day");
        c.In("Recycle ratio", "RecycleRatio", "");
        c.In("Water depth", "WaterDepthM", "m");

        c.Step("Total surface area", "Area = flow ÷ surface overflow rate",
            $"{c.N("FlowM3PerDay")} ÷ {c.N("SurfaceOverflowRateM3PerM2PerDay")}", "TotalAreaM2", "m²");

        var units = Math.Max(c.V("Units"), 1m);
        var unitArea = Div(c.V("TotalAreaM2"), units);
        c.Step("Number of clarifiers", "Units = total area ÷ maximum area of one clarifier, rounded up",
            $"{c.N("TotalAreaM2")} m² split into {Fmt(units)} unit(s)", "Units", "no.");
        c.Add("Diameter of each clarifier", "Diameter = √(4 × area per unit ÷ π)",
            $"√(4 × {Fmt(unitArea)} ÷ π)", c.N("DiameterM"), "m");
        c.Add("Total depth", "Total depth = water depth + freeboard",
            $"{c.N("WaterDepthM")} + {Fmt(c.V("DepthM") - c.V("WaterDepthM"))}", c.N("DepthM"), "m");

        var mlss = Div(c.V("SolidsLoadingKgPerM2PerHour") * c.V("TotalAreaM2") * 24m * 1000m,
            c.V("FlowM3PerDay") * (1m + c.V("RecycleRatio")));
        c.Step("Solids loading", "Loading = flow × (1 + recycle ratio) ÷ 24 × MLSS ÷ 1000 ÷ total area",
            $"{c.N("FlowM3PerDay")} × (1 + {c.N("RecycleRatio")}) ÷ 24 × {Fmt(mlss)} ÷ 1000 ÷ {c.N("TotalAreaM2")}",
            "SolidsLoadingKgPerM2PerHour", "kg/m²/h", "MLSS is taken from the aeration design.");
    }

    private static void PrimaryClarifier(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet TSS", "TssInMgL", "mg/L");
        c.In("Surface overflow rate", "SurfaceOverflowRateM3PerM2PerDay", "m³/m²/day");
        c.In("TSS removal fraction", "TssRemovalFraction", "");
        c.In("Sludge solids fraction", "SludgeSolidsFraction", "");

        c.Step("Total surface area", "Area = flow ÷ surface overflow rate",
            $"{c.N("FlowM3PerDay")} ÷ {c.N("SurfaceOverflowRateM3PerM2PerDay")}", "TotalAreaM2", "m²");

        var units = Math.Max(c.V("Units"), 1m);
        var unitArea = Div(c.V("TotalAreaM2"), units);
        c.Step("Number of clarifiers", "Units = total area ÷ maximum area of one clarifier, rounded up",
            $"{c.N("TotalAreaM2")} m² split into {Fmt(units)} unit(s)", "Units", "no.");
        c.Add("Diameter of each clarifier", "Diameter = √(4 × area per unit ÷ π)",
            $"√(4 × {Fmt(unitArea)} ÷ π)", c.N("DiameterM"), "m");
        c.Add("Total depth", "Total depth = water depth + freeboard",
            $"{c.N("WaterDepthM")} + {Fmt(c.V("DepthM") - c.V("WaterDepthM"))}", c.N("DepthM"), "m");
        c.Step("Retention time", "HRT = total area × water depth ÷ flow × 24",
            $"{c.N("TotalAreaM2")} × {c.N("WaterDepthM")} ÷ {c.N("FlowM3PerDay")} × 24", "HrtHours", "hours");
        c.Step("Solids removed", "Solids = flow × inlet TSS ÷ 1000 × removal fraction",
            $"{c.N("FlowM3PerDay")} × {c.N("TssInMgL")} ÷ 1000 × {c.N("TssRemovalFraction")}", "SolidsRemovedKgPerDay", "kg/day");
        c.Step("Sludge volume", "Sludge = solids removed ÷ (sludge solids fraction × 1000)",
            $"{c.N("SolidsRemovedKgPerDay")} ÷ ({c.N("SludgeSolidsFraction")} × 1000)", "SludgeM3PerDay", "m³/day");
    }

    private static void Coagulation(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Coagulation time", "CoagulationMinutes", "min");
        c.In("Flocculation time", "FlocculationMinutes", "min");
        c.In("Coagulant dose", "CoagulantMgL", "mg/L");
        c.In("Polymer dose", "PolymerMgL", "mg/L");

        c.Step("Coagulation tank volume", "Volume = flow ÷ 24 × coagulation time ÷ 60",
            $"{c.N("FlowM3PerDay")} ÷ 24 × {c.N("CoagulationMinutes")} ÷ 60", "CoagulationVolumeM3", "m³");
        c.Step("Flocculation tank volume", "Volume = flow ÷ 24 × flocculation time ÷ 60",
            $"{c.N("FlowM3PerDay")} ÷ 24 × {c.N("FlocculationMinutes")} ÷ 60", "FlocculationVolumeM3", "m³");
        c.Add("Coagulation tank size", "Length × width from volume and water depth",
            $"{c.N("CoagulationVolumeM3")} ÷ {c.N("WaterDepthM")} plan area",
            $"{c.N("CoagulationLengthM")} × {c.N("CoagulationWidthM")}", "m (L × W)");
        c.Add("Flocculation tank size", "Length × width from volume and water depth",
            $"{c.N("FlocculationVolumeM3")} ÷ {c.N("WaterDepthM")} plan area",
            $"{c.N("FlocculationLengthM")} × {c.N("FlocculationWidthM")}", "m (L × W)");
        c.Step("Coagulant quantity", "Coagulant = flow × dose ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("CoagulantMgL")} ÷ 1000", "CoagulantKgPerDay", "kg/day");
        c.Step("Polymer quantity", "Polymer = flow × dose ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("PolymerMgL")} ÷ 1000", "PolymerKgPerDay", "kg/day");

        var rapidRate = Div(c.V("RapidMixerPowerKw"), c.V("CoagulationVolumeM3"));
        c.Step("Rapid mixer power", "Power = coagulation volume × power per m³",
            $"{c.N("CoagulationVolumeM3")} × {Fmt(rapidRate)}", "RapidMixerPowerKw", "kW");

        var slowRate = Div(c.V("SlowMixerPowerKw"), c.V("FlocculationVolumeM3"));
        c.Step("Slow mixer power", "Power = flocculation volume × power per m³",
            $"{c.N("FlocculationVolumeM3")} × {Fmt(slowRate)}", "SlowMixerPowerKw", "kW");
    }

    private static void PhNeutralization(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet pH", "InletPh", "");
        c.In("Target pH", "TargetPh", "");
        c.In("Retention time", "RetentionMinutes", "min");

        c.Step("Tank volume", "Volume = flow ÷ 24 × retention time ÷ 60",
            $"{c.N("FlowM3PerDay")} ÷ 24 × {c.N("RetentionMinutes")} ÷ 60", "VolumeM3", "m³");
        Rect(c);

        var deviation = Math.Abs(c.V("InletPh") - c.V("TargetPh"));
        c.Add("pH deviation", "Deviation = |inlet pH − target pH|",
            $"|{c.N("InletPh")} − {c.N("TargetPh")}|", Fmt(deviation), "pH units");

        var reagentName = c.V("ReagentIsAlkali") > 0 ? "alkali (NaOH)" : "acid (H₂SO₄)";
        var rate = Div(c.V("ReagentKgPerDay"), c.V("FlowM3PerDay") * deviation);
        c.Step("Reagent quantity", "Reagent = flow × pH deviation × reagent per m³ per pH unit",
            $"{c.N("FlowM3PerDay")} × {Fmt(deviation)} × {Fmt(rate)}", "ReagentKgPerDay", "kg/day",
            $"Reagent used: {reagentName}.");
        c.Step("Dosing pump capacity", "Pump = reagent ÷ 24 ÷ reagent density (1.2)",
            $"{c.N("ReagentKgPerDay")} ÷ 24 ÷ 1.2", "DosingPumpLPerHour", "L/h");

        var mixRate = Div(c.V("MixerPowerKw"), c.V("VolumeM3"));
        c.Step("Mixer power", "Power = volume × power per m³",
            $"{c.N("VolumeM3")} × {Fmt(mixRate)}", "MixerPowerKw", "kW");
    }

    private static void Daf(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Inlet oil & grease", "OilGreaseInMgL", "mg/L");
        c.In("Surface loading", "SurfaceLoadingM3PerM2PerHour", "m³/m²/h");
        c.In("Recycle ratio", "RecycleRatio", "");
        c.In("Water depth", "WaterDepthM", "m");

        c.Step("Flow per hour", "Flow per hour = flow ÷ 24",
            $"{c.N("FlowM3PerDay")} ÷ 24", "FlowM3PerHour", "m³/h");
        c.Step("Surface area", "Area = flow per hour ÷ surface loading",
            $"{c.N("FlowM3PerHour")} ÷ {c.N("SurfaceLoadingM3PerM2PerHour")}", "AreaM2", "m²");
        c.Step("Tank volume", "Volume = area × water depth",
            $"{c.N("AreaM2")} × {c.N("WaterDepthM")}", "VolumeM3", "m³");
        c.Step("Detention time", "Detention = volume ÷ flow per hour × 60",
            $"{c.N("VolumeM3")} ÷ {c.N("FlowM3PerHour")} × 60", "DetentionMinutes", "min");
        Rect(c);
        c.Step("Recycle flow", "Recycle = flow per hour × recycle ratio",
            $"{c.N("FlowM3PerHour")} × {c.N("RecycleRatio")}", "RecycleFlowM3PerHour", "m³/h",
            $"Saturator pressure: {c.N("SaturatorPressureBarG")} bar(g).");

        var removal = Div(c.V("OilRemovedKgPerDay") * 1000m, c.V("FlowM3PerDay") * c.V("OilGreaseInMgL"));
        c.Step("Oil removed", "Oil = flow × inlet oil & grease ÷ 1000 × removal fraction",
            $"{c.N("FlowM3PerDay")} × {c.N("OilGreaseInMgL")} ÷ 1000 × {Fmt(removal)}", "OilRemovedKgPerDay", "kg/day");
    }

    private static void Thickener(Ctx c)
    {
        c.In("Sludge feed (volume)", "FeedM3PerDay", "m³/day");
        c.In("Sludge feed (dry solids)", "FeedDryKgPerDay", "kg/day");
        c.In("Solids loading", "SolidsLoadingKgPerM2PerDay", "kg/m²/day");
        c.In("Solids capture fraction", "SolidsCaptureFraction", "");
        c.In("Underflow solids fraction", "UnderflowSolidsFraction", "");

        c.Step("Surface area", "Area = dry solids feed ÷ solids loading",
            $"{c.N("FeedDryKgPerDay")} ÷ {c.N("SolidsLoadingKgPerM2PerDay")}", "TotalAreaM2", "m²");

        var units = Math.Max(c.V("Units"), 1m);
        var unitArea = Div(c.V("TotalAreaM2"), units);
        c.Step("Number of thickeners", "Units = total area ÷ maximum area of one thickener, rounded up",
            $"{c.N("TotalAreaM2")} m² split into {Fmt(units)} unit(s)", "Units", "no.");
        c.Add("Diameter of each thickener", "Diameter = √(4 × area per unit ÷ π)",
            $"√(4 × {Fmt(unitArea)} ÷ π)", c.N("DiameterM"), "m");
        c.Add("Total depth", "Total depth = water depth + freeboard",
            $"{c.N("WaterDepthM")} + {Fmt(c.V("DepthM") - c.V("WaterDepthM"))}", c.N("DepthM"), "m");

        var captured = c.V("FeedDryKgPerDay") * c.V("SolidsCaptureFraction");
        c.Add("Solids captured", "Captured = dry solids feed × capture fraction",
            $"{c.N("FeedDryKgPerDay")} × {c.N("SolidsCaptureFraction")}", Fmt(captured), "kg/day");
        c.Step("Underflow volume", "Underflow = captured solids ÷ (underflow solids fraction × 1000)",
            $"{Fmt(captured)} ÷ ({c.N("UnderflowSolidsFraction")} × 1000)", "UnderflowM3PerDay", "m³/day");
        c.Step("Supernatant", "Supernatant = sludge feed − underflow",
            $"{c.N("FeedM3PerDay")} − {c.N("UnderflowM3PerDay")}", "SupernatantM3PerDay", "m³/day",
            "Supernatant returns to equalization.");
    }

    private static void FilterPress(Ctx c)
    {
        c.In("Sludge feed (volume)", "FeedM3PerDay", "m³/day");
        c.In("Sludge feed (dry solids)", "FeedDryKgPerDay", "kg/day");
        c.In("Cake solids fraction", "CakeSolidsFraction", "");
        c.In("Cycles per day", "CyclesPerDay", "");
        c.In("Cycle time", "CycleHours", "hours");

        c.Step("Cake mass", "Cake = dry solids ÷ cake solids fraction",
            $"{c.N("FeedDryKgPerDay")} ÷ {c.N("CakeSolidsFraction")}", "CakeKgPerDay", "kg/day");

        var density = Div(c.V("CakeKgPerDay"), c.V("CakeM3PerDay"));
        c.Step("Cake volume", "Cake volume = cake mass ÷ cake density",
            $"{c.N("CakeKgPerDay")} ÷ {Fmt(density)}", "CakeM3PerDay", "m³/day");
        c.Step("Cake per cycle", "Cake per cycle = cake volume ÷ cycles per day",
            $"{c.N("CakeM3PerDay")} ÷ {c.N("CyclesPerDay")}", "CakeM3PerCycle", "m³");

        var chamberVolume = c.V("PlateSizeM") * c.V("PlateSizeM") * c.V("ChamberThicknessM") * 0.85m;
        c.Add("Volume of one chamber", "Chamber = plate size × plate size × chamber thickness × 0.85 fill",
            $"{c.N("PlateSizeM")} × {c.N("PlateSizeM")} × {c.N("ChamberThicknessM")} × 0.85", Fmt(chamberVolume), "m³");
        c.Step("Number of chambers", "Chambers = cake per cycle ÷ chamber volume, rounded up",
            $"{c.N("CakeM3PerCycle")} ÷ {Fmt(chamberVolume)}", "Chambers", "no.");
        c.Step("Number of plates", "Plates = chambers + 1",
            $"{c.N("Chambers")} + 1", "Plates", "no.");
        c.Step("Filtrate", "Filtrate = sludge feed − cake mass ÷ 1000",
            $"{c.N("FeedM3PerDay")} − {c.N("CakeKgPerDay")} ÷ 1000", "FiltrateM3PerDay", "m³/day",
            "Filtrate returns to equalization.");
    }

    private static void Disinfection(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Peak factor", "PeakFactor", "");
        c.In("Contact time", "ContactMinutes", "min");
        c.In("Chlorine dose", "ChlorineDoseMgL", "mg/L");

        c.Step("Peak flow", "Peak flow = flow ÷ 24 × peak factor",
            $"{c.N("FlowM3PerDay")} ÷ 24 × {c.N("PeakFactor")}", "PeakFlowM3PerHour", "m³/h");
        c.Step("Contact tank volume", "Volume = peak flow × contact time ÷ 60",
            $"{c.N("PeakFlowM3PerHour")} × {c.N("ContactMinutes")} ÷ 60", "VolumeM3", "m³");
        Rect(c);
        c.Step("Chlorine required", "Chlorine = flow × dose ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("ChlorineDoseMgL")} ÷ 1000", "ChlorineKgPerDay", "kg/day");

        var fraction = Div(c.V("ChlorineKgPerDay"), c.V("HypochloriteKgPerDay"));
        c.Step("Hypochlorite solution", "Hypochlorite = chlorine ÷ available chlorine fraction",
            $"{c.N("ChlorineKgPerDay")} ÷ {Fmt(fraction)}", "HypochloriteKgPerDay", "kg/day");
        c.Step("Dosing pump capacity", "Pump = hypochlorite ÷ 24 ÷ solution density (1.1)",
            $"{c.N("HypochloriteKgPerDay")} ÷ 24 ÷ 1.1", "DosingPumpLPerHour", "L/h");
    }

    private static void Softener(Ctx c)
    {
        c.In("Flow", "FlowM3PerDay", "m³/day");
        c.In("Hardness", "HardnessMgLAsCaCO3", "mg/L as CaCO₃");
        c.In("Resin capacity", "ResinCapacityKgCaCO3PerM3", "kg CaCO₃/m³ resin");
        c.In("Service flow rate", "ServiceBedVolumesPerHour", "bed volumes/h");
        c.In("Bed depth", "BedDepthM", "m");

        c.Step("Flow per hour", "Flow per hour = flow ÷ 24",
            $"{c.N("FlowM3PerDay")} ÷ 24", "FlowM3PerHour", "m³/h");
        c.Step("Resin volume", "Resin = flow per hour ÷ service bed volumes per hour",
            $"{c.N("FlowM3PerHour")} ÷ {c.N("ServiceBedVolumesPerHour")}", "ResinVolumeM3", "m³");
        c.Step("Vessel diameter", "Diameter = √(4 × (resin ÷ bed depth ÷ duty units) ÷ π)",
            $"Resin {c.N("ResinVolumeM3")} m³ ÷ bed depth {c.N("BedDepthM")} m, shared over {c.N("DutyUnits")} duty vessel(s)",
            "DiameterM", "m");
        c.Step("Total vessels", "Total vessels = duty vessels + standby",
            $"{c.N("DutyUnits")} duty + {Fmt(c.V("Units") - c.V("DutyUnits"))} standby", "Units", "no.");
        c.Step("Hardness load", "Load = flow × hardness ÷ 1000",
            $"{c.N("FlowM3PerDay")} × {c.N("HardnessMgLAsCaCO3")} ÷ 1000", "HardnessLoadKgPerDay", "kg/day");
        c.Step("Run time between regenerations", "Run time = resin × capacity ÷ (hardness load ÷ 24)",
            $"{c.N("ResinVolumeM3")} × {c.N("ResinCapacityKgCaCO3PerM3")} ÷ ({c.N("HardnessLoadKgPerDay")} ÷ 24)",
            "RunHoursBetweenRegeneration", "hours");
        c.Step("Regenerations per day", "Regenerations = 24 ÷ run time",
            $"24 ÷ {c.N("RunHoursBetweenRegeneration")}", "RegenerationsPerDay", "per day");

        var saltDose = Div(c.V("SaltKgPerDay") * c.V("ResinCapacityKgCaCO3PerM3"), c.V("HardnessLoadKgPerDay"));
        c.Step("Salt consumption", "Salt = hardness load ÷ capacity × salt per m³ resin",
            $"{c.N("HardnessLoadKgPerDay")} ÷ {c.N("ResinCapacityKgCaCO3PerM3")} × {Fmt(saltDose)}", "SaltKgPerDay", "kg/day");
    }

    private static void Atfd(Ctx c)
    {
        c.In("Feed", "FeedM3PerDay", "m³/day");
        c.In("TDS", "TdsMgL", "mg/L");
        c.In("Evaporation rate", "EvaporationKgPerM2PerHour", "kg/m²/h");
        c.In("Operating hours", "OperatingHoursPerDay", "h/day");

        c.Step("Dissolved solids", "Solids = feed × TDS ÷ 1000",
            $"{c.N("FeedM3PerDay")} × {c.N("TdsMgL")} ÷ 1000", "SolidsKgPerDay", "kg/day");

        var moisture = 1m - Div(c.V("SolidsKgPerDay"), c.V("DryProductKgPerDay"));
        c.Step("Dry product", "Dry product = solids ÷ (1 − residual moisture)",
            $"{c.N("SolidsKgPerDay")} ÷ (1 − {Fmt(moisture)})", "DryProductKgPerDay", "kg/day");
        c.Step("Water evaporated", "Evaporation = (feed × 1000 − dry product) ÷ operating hours",
            $"({c.N("FeedM3PerDay")} × 1000 − {c.N("DryProductKgPerDay")}) ÷ {c.N("OperatingHoursPerDay")}",
            "WaterEvaporatedKgPerHour", "kg/h");
        c.Step("Heat transfer area", "Area = evaporation ÷ evaporation rate",
            $"{c.N("WaterEvaporatedKgPerHour")} ÷ {c.N("EvaporationKgPerM2PerHour")}", "HeatTransferAreaM2", "m²");
        c.Step("Number of units", "Units = total area ÷ maximum area of one unit, rounded up",
            $"{c.N("HeatTransferAreaM2")} m² split into {c.N("Units")} unit(s) of {c.N("UnitAreaM2")} m²", "Units", "no.");

        var steamRatio = Div(c.V("SteamKgPerHour"), c.V("WaterEvaporatedKgPerHour"));
        c.Step("Steam consumption", "Steam = evaporation × steam per kg of water",
            $"{c.N("WaterEvaporatedKgPerHour")} × {Fmt(steamRatio)}", "SteamKgPerHour", "kg/h");

        var feedFraction = Div(c.V("SolidsKgPerDay"), c.V("CrystallizerFeedKgPerDay"));
        c.Step("Crystallizer feed", "Feed = solids ÷ crystallizer feed solids fraction",
            $"{c.N("SolidsKgPerDay")} ÷ {Fmt(feedFraction)}", "CrystallizerFeedKgPerDay", "kg/day");
        c.Step("Condensate", "Condensate = water evaporated per day ÷ 1000",
            $"{c.N("WaterEvaporatedKgPerHour")} × {c.N("OperatingHoursPerDay")} ÷ 1000", "CondensateM3PerDay", "m³/day");
    }

    private static void Rect(Ctx c)
    {
        c.Add("Tank dimensions", "Plan area = volume ÷ water depth; length × width from the tank proportion",
            $"{c.N("VolumeM3")} ÷ {c.N("WaterDepthM")} = {Fmt(Div(c.V("VolumeM3"), c.V("WaterDepthM")))} m² plan area",
            $"{c.N("LengthM")} × {c.N("WidthM")}", "m (L × W)");
        c.Add("Total tank depth", "Total depth = water depth + freeboard",
            $"{c.N("WaterDepthM")} + {Fmt(c.V("DepthM") - c.V("WaterDepthM"))}", c.N("DepthM"), "m");
    }

    private static void Blower(Ctx c)
    {
        c.Add("Blower power", "Adiabatic compression power for the air flow at the tank submergence",
            $"{c.N("AirRequiredM3PerHour")} m³/h of air at about {Fmt(c.V("WaterDepthM") + 0.5m)} m submergence",
            c.N("BlowerPowerKw"), "kW", "Includes the blower efficiency from the design rules.");
    }

    private static string Fmt(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static decimal Div(decimal a, decimal b) => b == 0m ? 0m : a / b;

    private sealed class Ctx
    {
        private readonly IReadOnlyDictionary<string, decimal> _p;

        public Ctx(IReadOnlyDictionary<string, decimal> p)
        {
            _p = p;
        }

        public List<UnitValueItem> Inputs { get; } = new();

        public List<UnitCalcStep> Steps { get; } = new();

        public decimal V(string key) => _p.TryGetValue(key, out var v) ? v : 0m;

        public bool Has(string key) => _p.ContainsKey(key);

        public string N(string key) => Fmt(V(key));

        public void In(string label, string key, string unit)
        {
            if (!Has(key)) return;
            Inputs.Add(new UnitValueItem { Label = label, Value = N(key), Unit = unit });
        }

        public void Step(string title, string formula, string substitution, string resultKey, string unit, string? note = null)
            => Add(title, formula, substitution, Has(resultKey) ? N(resultKey) : "-", unit, note);

        public void Add(string title, string formula, string substitution, string result, string unit, string? note = null)
            => Steps.Add(new UnitCalcStep
            {
                Number = Steps.Count + 1,
                Title = title,
                Formula = formula,
                Substitution = substitution,
                Result = result,
                Unit = unit,
                Note = note
            });
    }
}