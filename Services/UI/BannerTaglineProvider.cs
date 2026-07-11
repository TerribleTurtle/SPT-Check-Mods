using System;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Provides random taglines for the application banner.
/// </summary>
public static class BannerTaglineProvider
{
    private static readonly string[] _bannerTaglines =
    [
        "Cheeki breeki, your mods are peaky!",
        "No FiR tag required.",
        "Opachki! Your mods are showing.",
        "Warning: May cause gear fear.",
        "Fence would sell this for 3x the price.",
        "Not responsible for any leg meta incidents.",
        "Ref approved.",
        "Scav karma not affected by usage.",
        "No insurance fraud detected.",
        "Jaeger would make this a daily quest.",
        "Tested on scavs!",
        "More reliable than a PM pistol.",
        "Killa can't spawn here. You're safe.",
        "Side effects may include mod addiction.",
        "Lighthouse rogues hate this one simple trick!",
        "Your stash is safe. Your mods? Let's see...",
        "Better odds than finding a GPU in raid.",
        "Tagilla tested, Tagilla approved.",
        "No extract campers were consulted.",
        "Mechanic charges extra for this service.",
        "Labs keycard not required.",
        "Results may vary based on desync.",
        "Powered by strong coffee.",
        "Divide my cheeks!",
        "Won't fix your packet loss.",
        "Prapor's dogs won't find your lost mods.",
        "Extracting in 3... 2... 1...",
        "Awaits session start forever.",
        "Watch out for the Goons.",
        "Therapist wants to know your location.",
        "Head, Eyes.",
        "More terrifying than a cultist in the bushes.",
        "Dicky needles!",
    ];

    public static string GetRandomTagline()
    {
        return _bannerTaglines[Random.Shared.Next(_bannerTaglines.Length)];
    }
}
