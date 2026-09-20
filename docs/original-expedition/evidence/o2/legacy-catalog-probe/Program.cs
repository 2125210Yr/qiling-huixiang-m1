using System;
using System.Reflection;
using Resonance.Battle;
using Resonance.Tests;

internal static class Program
{
    static int Main()
    {
        Console.WriteLine("Same-process legacy Catalog contamination probe; no test or production source changes.");
        Reset();
        var before = Poison("01 clean catalog");

        Console.WriteLine("02 invoke BattleSimTests.CatalogJsonSerializeRoundtripKeepsOpcode");
        try { new BattleSimTests().CatalogJsonSerializeRoundtripKeepsOpcode(); }
        catch (Exception error) { Console.WriteLine("Polluter unexpectedly failed: " + error); return 2; }
        PrintCatalog();
        var polluted = Poison("03 after opcode roundtrip");

        Reset();
        var restored = Poison("04 after BuildBuiltin restoration");
        var reproduced = before && !polluted && restored;
        Console.WriteLine("CONFIRMED pass -> fail -> pass: " + reproduced);
        return reproduced ? 0 : 1;
    }

    static void Reset()
    {
        typeof(Catalog).GetMethod("BuildBuiltin", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Console.WriteLine("Catalog.BuildBuiltin invoked.");
        PrintCatalog();
    }

    static bool Poison(string step)
    {
        Console.WriteLine(step + ": M1CoreSliceTests.PoisonTriggersOnActionAndHitTakenNotPerSecond");
        try
        {
            new M1CoreSliceTests().PoisonTriggersOnActionAndHitTakenNotPerSecond();
            Console.WriteLine("PASS");
            PrintCatalog();
            return true;
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL " + error);
            PrintCatalog();
            return false;
        }
    }

    static void PrintCatalog()
    {
        var stun = Catalog.TryEffect("stun");
        Console.WriteLine("Catalog: C001_tap.Opcode=" + Catalog.MustSkill("C001_tap").Opcode
            + "; stun.Opcode=" + stun.Opcode + "; stun.Kind=" + stun.Kind
            + "; stun.Magnitude=" + stun.Magnitude + "; stun.DurationSec=" + stun.DurationSec);
    }
}
